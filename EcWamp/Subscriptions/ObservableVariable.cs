using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Collections;

namespace EcWamp.Subscriptions
{
    public class ObservableVariable : IEqualityComparer<ObservableVariable>, IObservable<ObservableVariable>
    {
        public string TechnicalAddress { get; set; }
        public object Value { get; set; }

        public bool Equals(ObservableVariable x, ObservableVariable y)
        {
            return x.TechnicalAddress.ToLower().Equals(y.TechnicalAddress.ToLower());
        }

        public int GetHashCode(ObservableVariable obj)
        {
            return obj.TechnicalAddress.ToLower().GetHashCode();
        }

        public IDisposable Subscribe(IObserver<ObservableVariable> observer)
        {
            throw new NotImplementedException();
        }
    }
}
