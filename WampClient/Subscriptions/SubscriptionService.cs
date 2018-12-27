using JsonData;
using System;
using System.Collections.Generic;

namespace EcWamp.Subscriptions
{
    public class SubscriptionService
    {
        public Dictionary<string, SubscriptionStream> Streams { get; private set; } = new Dictionary<string, SubscriptionStream>();
        private readonly MasterList masterList =  new MasterList(evictedVariable => Console.WriteLine($"SS: Heard that {evictedVariable} was evicted from the MasterList"));

        public SubscriptionStream GetStream(string subscriptionId, List<BindableElement> bindableElements)
        {
            // If it already exists, return it
            if (Streams.ContainsKey(subscriptionId)) return Streams[subscriptionId];

            var stream = new SubscriptionStream();
            foreach (var bindableElement in bindableElements)
            {
                masterList.GetOrAdd(bindableElement.BindingAddress);
            }
            stream.BindableVariables.AddRange(bindableElements);
            Streams.Add(subscriptionId, stream);

            return stream;
        }

        public void Remove(string sid)
        {
            if(Streams.ContainsKey(sid))
            {
                var stream = Streams[sid];
                masterList.Remove(stream);
                Streams.Remove(sid);
            }
        }
    }
}