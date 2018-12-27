using EXOscadaAPI.Interfaces.Logging;
using EXOScadaAPI.DataStore;
using JsonData;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace EcWamp.Subscriptions
{
    public class SubscriptionService
    {
        public Dictionary<string, SubscriptionStream> Streams { get; private set; } = new Dictionary<string, SubscriptionStream>();
        private readonly MasterList masterList =  new MasterList(evictedVariable => Console.WriteLine($"SS: Heard that {evictedVariable} was evicted from the MasterList"));
        private readonly ILog log = LogProvider.GetCurrentClassLogger();

        public SubscriptionStream GetStream(string subscriptionId, List<BindableElement> bindableElements)
        {
            // If it already exists, return it
            if (Streams.ContainsKey(subscriptionId)) return Streams[subscriptionId];

            log.Debug($"Creating a new Stream for {subscriptionId}");

            var stream = new SubscriptionStream();
            foreach (var bindableElement in bindableElements)
            {
                masterList.GetOrAdd(bindableElement.BindingAddress);
            }
            stream.BindableVariables.AddRange(bindableElements);
            Streams.Add(subscriptionId, stream);

            return stream;
        }

        public void Remove(string subscriptionId)
        {
            if(Streams.ContainsKey(subscriptionId))
            {
                log.Debug($"Removing subscriptionId: {subscriptionId}");
                var stream = Streams[subscriptionId];
                masterList.Remove(stream);
                Streams.Remove(subscriptionId);
            }
        }

        class MasterList
        {
            private readonly Dictionary<string, (ObservableVariable Variable, int RefCounter)> _variables = new Dictionary<string, (ObservableVariable Variable, int RefCounter)>(1000);
            private readonly Action<string> _evictionHandler;
            private readonly DataStore _dataStore;
            private readonly ILog log = LogProvider.GetCurrentClassLogger();

            // TODO: We need to DI the DataStore, but we do it manually as for now
            public MasterList(Action<string> evictionHandler /*, IDataStore dataStore*/)
            {
                _evictionHandler = evictionHandler;

                _dataStore = new DataStore("127.0.0.1", "", 8080, "");
                _dataStore.Open();

                _dataStore.DataChanged += DataStore_DataChanged;
            }

            private void DataStore_DataChanged(EXOscadaAPI.Protocols.DataStoreMessage message)
            {
                if (message.Type == EXOscadaAPI.Protocols.DataStoreMessageType.Update)
                {
                    var value = message.Value;
                    var variable = message.Variable.ToLower();
                    if (_variables.ContainsKey(variable))
                    {
                        log.Debug($"Updating the value for {variable} to {value}");

                        var tuple = _variables[variable];
                        tuple.Variable.Value = value;
                        _variables[variable] = tuple;
                        // TODO: Here it would be nice to do -> [something].OnNext(tuple.Variable)
                    }
                }
            }

            public IObservable<ObservableVariable> GetOrAdd(string variable)
            {
                variable = variable.ToLower();

                lock (_variables)
                {
                    // Does the variable already exist?
                    if (_variables.ContainsKey(variable))
                    {
                        // Yes it did. Increment the refCounter and return the ObservableVariable
                        log.Debug($"{variable} is already in the MasterList");
                        var tuple = _variables[variable];
                        tuple.RefCounter++;
                        _variables[variable] = tuple;
                        return tuple.Variable.AsObservable();
                    }
                    else
                    {
                        // No it didn't, so lets create it
                        log.Debug($"Adding {variable} to the MasterList");
                        var tuple = (Variable: new ObservableVariable() { TechnicalAddress = variable }, RefCounter: 1);
                        _variables.Add(variable, tuple);

                        // TODO: Add it to the DataStore
                        _dataStore.Read(variable);

                        return tuple.Variable.AsObservable();
                    }
                }
            }

            public List<IObservable<ObservableVariable>> GetOrAdd(SubscriptionStream stream)
            {
                var observables = new List<IObservable<ObservableVariable>>();
                foreach (var variable in stream.BindableVariables)
                {
                    observables.Add(GetOrAdd(variable.BindingAddress));
                }
                return observables;
            }

            public void Remove(SubscriptionStream userViewList)
            {
                foreach (var variable in userViewList.BindableVariables)
                {
                    Remove(variable.BindingAddress);
                }
            }

            public void Remove(ObservableVariable variable)
            {
                Remove(variable.TechnicalAddress);
            }

            public void Remove(string variable)
            {
                variable = variable.ToLower();

                lock (_variables)
                {
                    if (!_variables.ContainsKey(variable))
                    {
                        log.Debug("Trying to remove an unregistered variable!");
                    }
                    else
                    {
                        var tuple = _variables[variable];
                        tuple.RefCounter--;
                        _variables[variable] = tuple;

                        if (tuple.RefCounter < 1)
                        {
                            log.Debug($"No more registered listeners for {variable}");
                            _variables.Remove(variable);
                            _evictionHandler(variable);
                        }
                    }
                }
            }

            public int References(string variable)
            {
                variable = variable.ToLower();

                lock (_variables)
                {
                    if (!_variables.ContainsKey(variable))
                    {
                        log.Debug("Trying to count references of an unregistered variable!");
                        return -1;
                    }
                    else
                    {
                        var (Variable, RefCounter) = _variables[variable];
                        log.Debug($"{variable} currently has {RefCounter} references");
                        return RefCounter;
                    }
                }
            }
        }
    }
}