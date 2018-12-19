using EXOscadaAPI.Interfaces.Logging;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Realm;

namespace EXOScadaAPI.DataStore.Wamp
{
    public class WampChannelTimeOutReconnector : IDisposable
    {
        public event EventHandler TimeOut;

        private IObservable<Unit> mMerged;
        private IDisposable mDisposable = Disposable.Empty;
        private bool mStarted = false;
        private bool _connected = false;
        private readonly object mLock = new object();
        private IDisposable mConnectionBrokenDisposable;
        private System.Timers.Timer aTimer;
        private readonly ILog log = LogProvider.GetCurrentClassLogger();

        public WampChannelTimeOutReconnector(IWampChannel channel, Func<Task> connector)
        {
            log.Debug($"In WampChannelTimeOutReconnector ctor with channel: {channel}, connector: {connector}");
            IWampClientConnectionMonitor monitor = channel.RealmProxy.Monitor;
            monitor.ConnectionEstablished += Monitor_ConnectionEstablished;
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 1000;
            var connectionBrokenObservable =
                Observable.FromEventPattern<WampSessionCloseEventArgs>
                          (x => monitor.ConnectionBroken += x,
                           x => monitor.ConnectionBroken -= x)
                          .Select(x => Unit.Default)
                          .Replay(10);

            var onceAndConnectionBroken =
                connectionBrokenObservable.StartWith(Unit.Default);

            IObservable<IObservable<Unit>> reconnect =
                from connectionBroke in onceAndConnectionBroken
                let tryReconnect = Observable.FromAsync(connector)
                    .Catch<Unit, Exception>(x => Observable.Empty<Unit>())
                select tryReconnect;

            mConnectionBrokenDisposable = connectionBrokenObservable.Connect();

            mMerged = reconnect.Concat();
        }

        private int counter = 0;

        public void StopTimeOut()
        {
            log.Debug("In StopTimeOut");
            aTimer.Stop();
            counter = 0;
            _connected = false;
            Dispose();
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            log.Debug("In OnTimedEvent");
            counter++;
            if (counter > 20)
            {
                log.Debug("Counter > 20 so we're stopping and invoking");
                aTimer.Stop();
                counter = 0;
                TimeOut?.Invoke(this, e);
            }
        }

        private void Monitor_ConnectionEstablished(object sender, WampSessionCreatedEventArgs e)
        {
            log.Debug("In ConnectionEstablished");
            _connected = true;
            aTimer.Enabled = false;
        }

        /// <summary>
        /// Start trying connection establishment to router.
        /// </summary>
        public void Start()
        {
            log.Debug("In Start");
            lock (mLock)
            {
                if (mStarted)
                {
                    throw new Exception("Already started");
                }
                else
                {
                    if (mMerged != null)
                    {
                        log.Debug("Starting");
                        counter = 0;
                        aTimer.Start();
                        _connected = false;
                        mDisposable = mMerged.Subscribe(x => { });
                        mStarted = true;
                    }
                    else
                    {
                        log.Error("mMerged wa null!");
                        throw new ObjectDisposedException(typeof(WampChannelReconnector).Name);
                    }
                }
            }
        }

        public void Dispose()
        {
            log.Debug("In Dispose");
            lock (mLock)
            {
                mMerged = null;
                mDisposable.Dispose();
                mConnectionBrokenDisposable.Dispose();
            }
        }
    }
}