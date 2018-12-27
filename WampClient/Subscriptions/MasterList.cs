//using EXOScadaAPI.DataStore;
//using JsonData;
//using System;
//using System.Collections.Generic;
//using System.Reactive.Linq;

//namespace EcWamp.Subscriptions
//{
//    public class MasterList
//    {
//        private readonly Dictionary<string, (ObservableVariable Variable, int RefCounter)> _variables = new Dictionary<string, (ObservableVariable Variable, int RefCounter)>(1000);
//        private readonly Action<string> _evictionHandler;
//        private readonly DataStore _dataStore;

//        // TODO: We need to DI the DataStore, but we do it manually as for now
//        public MasterList(Action<string> evictionHandler /*, IDataStore dataStore*/)
//        {
//            _evictionHandler = evictionHandler;

//            _dataStore = new DataStore("127.0.0.1", "", 8080, "");
//            _dataStore.Open();

//            _dataStore.DataChanged += DataStore_DataChanged;
//        }

//        private void DataStore_DataChanged(EXOscadaAPI.Protocols.DataStoreMessage message)
//        {
//            if(message.Type == EXOscadaAPI.Protocols.DataStoreMessageType.Update)
//            {
//                var value = message.Value;
//                var variable = message.Variable.ToLower();
//                if(_variables.ContainsKey(variable))
//                {
//                    Console.WriteLine($"Updating the value for {variable} to {value}");

//                    var tuple = _variables[variable];
//                    tuple.Variable.Value = value;
//                    _variables[variable] = tuple;
//                    // TODO: Here it would be nice to do -> [something].OnNext(tuple.Variable)
//                }
//            }
//        }

//        public IObservable<ObservableVariable> GetOrAdd(string variable)
//        {
//            variable = variable.ToLower();

//            lock (_variables)
//            {
//                // Does the variable already exist?
//                if (_variables.ContainsKey(variable))
//                {
//                    // Yes it did. Increment the refCounter and return the ObservableVariable
//                    Console.WriteLine($"{variable} is already in the MasterList");
//                    var tuple = _variables[variable];
//                    tuple.RefCounter++;
//                    _variables[variable] = tuple;
//                    return tuple.Variable.AsObservable();
//                }
//                else
//                {
//                    // No it didn't, so lets create it
//                    Console.WriteLine($"Adding {variable} to the MasterList");
//                    var tuple = (Variable: new ObservableVariable() { TechnicalAddress = variable }, RefCounter: 1);
//                    _variables.Add(variable, tuple);

//                    // TODO: Add it to the DataStore
//                    _dataStore.Read(variable);

//                    return tuple.Variable.AsObservable();
//                }
//            }
//        }

//        public List<IObservable<ObservableVariable>> GetOrAdd(SubscriptionStream stream)
//        {
//            var observables = new List<IObservable<ObservableVariable>>();
//            foreach (var variable in stream.BindableVariables)
//            {
//                observables.Add(GetOrAdd(variable.BindingAddress));
//            }
//            return observables;
//        }

//        public void Remove(SubscriptionStream userViewList)
//        {
//            foreach (var variable in userViewList.BindableVariables)
//            {
//                Remove(variable.BindingAddress);
//            }
//        }

//        public void Remove(ObservableVariable variable)
//        {
//            Remove(variable.TechnicalAddress);
//        }

//        public void Remove(string variable)
//        {
//            variable = variable.ToLower();

//            lock (_variables)
//            {
//                if (!_variables.ContainsKey(variable))
//                {
//                    Console.WriteLine("Trying to remove an unregistered variable!");
//                }
//                else
//                {
//                    var tuple = _variables[variable];
//                    tuple.RefCounter--;
//                    _variables[variable] = tuple;

//                    if (tuple.RefCounter < 1)
//                    {
//                        Console.WriteLine($"No more registered listeners for {variable}");
//                        _variables.Remove(variable);
//                        _evictionHandler(variable);
//                    }
//                }
//            }
//        }

//        public int References(string variable)
//        {
//            variable = variable.ToLower();

//            lock (_variables)
//            {
//                if (!_variables.ContainsKey(variable))
//                {
//                    Console.WriteLine("Trying to count references of an unregistered variable!");
//                    return -1;
//                }
//                else
//                {
//                    var (Variable, RefCounter) = _variables[variable];
//                    Console.WriteLine($"{variable} currently has {RefCounter} references");
//                    return RefCounter;
//                }
//            }
//        }
//    }
//}