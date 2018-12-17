using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EcWamp.Subscriptions
{
    public class MasterList
    {
        public List<ObservableVariable> VariablesList { get; private set; } = new List<ObservableVariable>();
        private readonly ConcurrentDictionary<ObservableVariable, int> variables = new ConcurrentDictionary<ObservableVariable, int>(2, 1000);

        // TODO: Add the ref counter somewhere!

        public ObservableVariable Add(string variable)
        {
            var tempVar = new ObservableVariable() { TechnicalAddress = variable };
            if (!variables.TryAdd(tempVar, 1))
            {
                Console.WriteLine($"Variable {variable} is already in the MasterList!");
                // TODO: Increase the ref counter
                return tempVar;
            }
            else
            {
                if (!variables.TryUpdate(tempVar, 0, 0))
                {
                    Console.WriteLine("Unable to update the MasterList!");
                    return null;
                }
            }

            return tempVar;
        }

        public void Remove(ObservableVariable variable)
        {
            if (!variables.ContainsKey(variable))
            {
                Console.WriteLine("Trying to remove an unregistered variable!");
                return;
            }
        }
    }
}