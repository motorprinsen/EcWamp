using EXOscadaAPI.Interfaces.Logging;
using EXOscadaAPI.Protocols;
using EXOScadaAPI.DataStore.Wamp;
using FluentCache;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace EXOScadaAPI.DataStore
{
    public class DataStore : IDataStore
    {
        private Cache<IOperationsService> _repositoryCache;
        private CallbackMemoryCache _cacheImpl;
        private IWampChannel _channel;
        private IOperationsService _proxy;
        public bool _connected = false;
        private string _connectionString;
        private ManualResetEvent _resetEvent;
        private WampChannelTimeOutReconnector _reconnector;
        private readonly ILog log = LogProvider.GetCurrentClassLogger();

        public event DataMessageDelegate DataChanged;
        public event OnCacheItemExpiration CacheItemExpired;

        public DataStore(string server, string endPoint, int port, String securityKey)
        {
            //IWampClientAuthenticator authenticator;
            //authenticator = new TicketAuthenticator(securityKey);
            _connectionString = $"ws://{server}:{port}/{endPoint}";
            DefaultWampChannelFactory factory =
                        new DefaultWampChannelFactory();

            log.Debug($"Creating the json channel with commectionString: {_connectionString}");
            _channel = factory.CreateJsonChannel(_connectionString, "data"/*, authenticator*/);
            _channel.RealmProxy.Monitor.ConnectionBroken += Monitor_ConnectionBroken;
            _channel.RealmProxy.Monitor.ConnectionError += Monitor_ConnectionError;
            _channel.RealmProxy.Monitor.ConnectionEstablished += Monitor_ConnectionEstablished;

            log.Debug("Getting proxies");
            _proxy = _channel.RealmProxy.Services.GetCalleeProxy<IOperationsService>();

            log.Debug("Creating the cache");
            _cacheImpl = new CallbackMemoryCache(System.Runtime.Caching.MemoryCache.Default);
            _repositoryCache = _cacheImpl.WithSource(_proxy);
            _cacheImpl.OnCacheItemExpiration += OnCacheItemExpiration;

            _resetEvent = new ManualResetEvent(false);
        }

        private void OnCacheItemExpiration(DataStoreMessage message)
        {
            // Pass it upward
            CacheItemExpired?.Invoke(message);
        }

        private IDisposable subscription;

        public void SetSubscribe()
        {
            LogProvider.GetCurrentClassLogger().Debug("Set subscribe");
            //Add a general subscription for all message from server
            subscription =
              _channel.RealmProxy.Services.GetSubject<DataStoreMessage>("subscriptions")
                   .Subscribe(x =>
                   {
                       //TODO : Check if a valid datastoremessage
                       //if (x.Type == DataStoreMessageType.Error)
                       //{
                       //}
                       //else
                       {
                           log.Debug($"Got subscription event: {x}");
                           SetDataToCache(x);
                       }
                   });
        }

        private void Monitor_ConnectionEstablished(object sender, WampSharp.V2.Realm.WampSessionCreatedEventArgs e)
        {
            LogProvider.GetCurrentClassLogger().Info("Connection established");
            _connected = true;
            _resetEvent.Set();
            SetSubscribe();
        }

        private void Monitor_ConnectionError(object sender, WampSharp.Core.Listener.WampConnectionErrorEventArgs e)
        {
            LogProvider.GetCurrentClassLogger().Error("ConnectionError:" + e.Exception.Message);
            if (subscription != null)
                subscription.Dispose();
        }

        private void Monitor_ConnectionBroken(object sender, WampSharp.V2.Realm.WampSessionCloseEventArgs e)
        {
            log.Info("Got ConnectionBroken!");
            _resetEvent.Reset();
            _connected = false;

            if (e.CloseType == SessionCloseType.Abort)
            {
                log.Debug("It was an Abort");
                if (subscription != null)
                    subscription.Dispose();
                _resetEvent.Set();
                _reconnector.StopTimeOut();
                LogProvider.GetCurrentClassLogger().Error($"SessionCloseType.Abort {e.Reason}");
            }
        }

        private void Reconnector_TimeOut(object sender, EventArgs e)
        {
            LogProvider.GetCurrentClassLogger().Debug("Timeout");
            _resetEvent.Set();
            _connected = false;
        }

        public ProjectInfo GetProjectInfo()
        {
            log.Debug("In ProjectInfo");
            try
            {
                log.Debug("Opening the connection");
                Open();

                log.Debug("Getting and returning ProjectInfo");
                return _proxy.GetProjectInfo();
            }
            catch (Exception e)
            {
                log.Error($"Ran in to an exception while getting ProjectInfo: {e.Message}");
                throw e;
            }
        }

        public void Open()
        {
            log.Debug("In Open()");
            if (_connected)
            {
                log.Debug("We're already connected! Returning...");
                return;
            }

            LogProvider.GetCurrentClassLogger().Info($"Open channel: {_connectionString}");

            try
            {
                async Task connect()
                {
                    log.Debug("Awaiting Open...");
                    await _channel.Open().ConfigureAwait(false);
                    log.Debug("We should be opened now");
                }

                log.Debug("Setting up and starting the WampChannelTimeOutReconnector");
                _reconnector = new WampChannelTimeOutReconnector(_channel, connect);
                _reconnector.TimeOut += Reconnector_TimeOut;
                _reconnector.Start();

                log.Debug("Waiting...");
                _resetEvent.WaitOne();
                log.Debug("Continuing");

                if (!_connected)
                {
                    String s = "Connection to EXOscadaFunction couldn't be established!";
                    LogProvider.GetCurrentClassLogger().Error(s);
                    throw new Exception(s);
                }
                log.Debug("Everything looks fine! Returning...");
            }
            catch (Exception e)
            {
                LogProvider.GetCurrentClassLogger().Error(e.Message);
                throw e;
            }
        }

        public bool Write(DataStoreMessage message)
        {
            LogProvider.GetCurrentClassLogger().Debug($"Write datastore message: {message.ToString()}");
            return _proxy.Write(message);
        }

        public Dictionary<string, DataStoreMessage> Read(List<string> variables)
        {
            Dictionary<string, DataStoreMessage> dataDictonary = new Dictionary<string, DataStoreMessage>();
            foreach (String variable in variables)
            {
                dataDictonary.Add(variable, Read(variable).Result);
            }
            return dataDictonary;
        }

        public async Task<DataStoreMessage> Read(string variable)
        {
            DataStoreMessage value = await _repositoryCache.Method(r => _proxy.Read(variable))
                      .ExpireAfter(TimeSpan.FromSeconds(20))
                      .GetValueAsync();
            return value;
        }

        public void SetDataToCache(DataStoreMessage data)
        {
            String variable = data.Variable;

            _repositoryCache.Method(r => _proxy.Read(variable)).SetValue(data);

            DataChanged?.Invoke(data);
        }
    }
}