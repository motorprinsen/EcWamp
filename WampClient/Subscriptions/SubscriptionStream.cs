using JsonData;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace EcWamp.Subscriptions
{
    public class SubscriptionStream : IDisposable
    {
        public string SubscriptionId { get; set; }
        public List<BindableElement> BindableVariables { get; set; } = new List<BindableElement>();

        private readonly Subject<(BindableElement, object)> subject = new Subject<(BindableElement, object)>();
        public IObservable<(BindableElement Element, object Value)> Updates { get { return subject.AsObservable(); } }

        public SubscriptionStream()
        {
            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(2000);
            //        lock (subject)
            //        {
            //            if (subject.HasObservers)
            //            {
            //                foreach (var variable in BindableVariables)
            //                {
            //                    var tuple = (variable, DateTime.Now.Second);
            //                    subject.OnNext(tuple);
            //                }
            //            }
            //            else
            //            {
            //                Console.WriteLine("Subject has no observers!");
            //            }
            //        }
            //    }
            //});
        }


        public void Dispose()
        {
            Console.WriteLine($"Disposing SubscriptionStream with sid: {SubscriptionId}");
            
        }
    }
}