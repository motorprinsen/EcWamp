using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcWamp.Subscriptions
{
    public class SubscriptionService
    {
        public Dictionary<string, SubscriptionStream> Streams { get; private set; } = new Dictionary<string, SubscriptionStream>();

        public void Add(string subscriptionId)
        {
            if(!Streams.ContainsKey(subscriptionId))
            {

            }
        }
    }
}
