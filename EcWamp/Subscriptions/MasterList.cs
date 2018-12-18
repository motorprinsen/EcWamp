using JsonData;
using System;
using System.Collections.Generic;

namespace EcWamp.Subscriptions
{
    public class MasterList
    {
        private readonly Dictionary<string, (ObservableVariable Variable, int RefCounter)> _variables = new Dictionary<string, (ObservableVariable Variable, int RefCounter)>(1000);
        private readonly Action<string> _evictionHandler;

        // TODO: We need to reference the DataStore and hook up to OnDataChanged

        public MasterList(Action<string> evictionHandler)
        {
            _evictionHandler = evictionHandler;
        }

        public ObservableVariable Add(string variable)
        {
            variable = variable.ToLower();

            lock (_variables)
            {
                // Does the variable already exist?
                if (_variables.ContainsKey(variable))
                {
                    // Yes it did. Increment the refCounter and return the ObservableVariable
                    Console.WriteLine($"{variable} is already in the MasterList");
                    var tuple = _variables[variable];
                    tuple.RefCounter++;
                    _variables[variable] = tuple;
                    return tuple.Variable;
                }
                else
                {
                    // No it didn't, so lets create it
                    Console.WriteLine($"Adding {variable} to the MasterList");
                    var tuple = (Variable: new ObservableVariable() { TechnicalAddress = variable }, RefCounter: 1);
                    _variables.Add(variable, tuple);

                    // TODO: Add it to the DataStore
                    // dataStore.Read(variable);

                    return tuple.Variable;
                }
            }
        }

        public void Add(SubscriptionStream stream)
        {
            foreach (var variable in stream.BindableVariables)
            {
                Add(variable.BindingAddress);
            }
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
                    Console.WriteLine("Trying to remove an unregistered variable!");
                }
                else
                {
                    var tuple = _variables[variable];
                    tuple.RefCounter--;
                    _variables[variable] = tuple;

                    if (tuple.RefCounter < 1)
                    {
                        Console.WriteLine($"No more registered listeners for {variable}");
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
                    Console.WriteLine("Trying to count references of an unregistered variable!");
                    return -1;
                }
                else
                {
                    var (Variable, RefCounter) = _variables[variable];
                    Console.WriteLine($"{variable} currently has {RefCounter} references");
                    return RefCounter;
                }
            }
        }
    }
}