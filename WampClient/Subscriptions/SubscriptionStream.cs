using JsonData;
using System.Collections.Generic;

namespace EcWamp.Subscriptions
{
    public class SubscriptionStream
    {
        public string SubscriptionId { get; set; }
        public List<BindableElement> BindableVariables { get; set; }
    }
}