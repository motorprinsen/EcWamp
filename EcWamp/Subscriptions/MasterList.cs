using System;
using System.Collections.Generic;

namespace EcWamp.Subscriptions
{
    public class MasterList
    {
        public List<ObservableVariable> VariablesList { get; private set; } = new List<ObservableVariable>();
        private readonly Dictionary<string, (ObservableVariable Variable, int RefCounter)> variables = new Dictionary<string, (ObservableVariable Variable, int RefCounter)>(1000);
        private readonly Action<string> _evictionHandler;

        public MasterList(Action<string> evictionHandler)
        {
            _evictionHandler = evictionHandler;
        }

        public ObservableVariable Add(string variable)
        {
            variable = variable.ToLower();

            lock (variables)
            {
                // Does the variable already exist?
                if (variables.ContainsKey(variable))
                {
                    // Yes it did. Increment the refCounter and return the ObservableVariable
                    Console.WriteLine($"{variable} is already in the MasterList");
                    var tuple = variables[variable];
                    tuple.RefCounter++;
                    variables[variable] = tuple;
                    return tuple.Variable;
                }
                else
                {
                    // No it didn't, so lets create it
                    Console.WriteLine($"Adding {variable} to the MasterList");
                    var tuple = (Variable: new ObservableVariable() { TechnicalAddress = variable }, RefCounter: 1);
                    variables.Add(variable, tuple);
                    return tuple.Variable;
                }
            }
        }

        public void Remove(ObservableVariable variable)
        {
            Remove(variable.TechnicalAddress);
        }

        public void Remove(string variable)
        {
            variable = variable.ToLower();

            lock (variables)
            {
                if (!variables.ContainsKey(variable))
                {
                    Console.WriteLine("Trying to remove an unregistered variable!");
                }
                else
                {
                    var tuple = variables[variable];
                    tuple.RefCounter--;
                    variables[variable] = tuple;

                    if (tuple.RefCounter < 1)
                    {
                        Console.WriteLine($"No more registered listeners for {variable}");
                        variables.Remove(variable);
                        _evictionHandler(variable);
                    }
                }
            }
        }

        public int References(string variable)
        {
            variable = variable.ToLower();

            lock (variables)
            {
                if (!variables.ContainsKey(variable))
                {
                    Console.WriteLine("Trying to count references of an unregistered variable!");
                    return -1;
                }
                else
                {
                    var (Variable, RefCounter) = variables[variable];
                    Console.WriteLine($"{variable} currently has {RefCounter} references");
                    return RefCounter;
                }
            }
        }
    }
}