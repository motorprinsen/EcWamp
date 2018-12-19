using EcWamp.DataStore;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace EcWamp
{
    public class MockScadaFunction : IOperationsService
    {
        private readonly List<string> subscriptions = new List<string>();
        private readonly Random random = new Random();

        public MockScadaFunction(ISubject<DataStoreMessage> @subject)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if(subscriptions.Count > 0)
                    {
                        lock (subscriptions)
                        {
                            foreach (var variable in subscriptions)
                            {
                                var message = new DataStoreMessage()
                                {
                                    Timestamp = DateTime.Now,
                                    Type = DataStoreMessageType.Update,
                                    Variable = variable,
                                    Value = random.NextDouble() * 100
                                };
                                Console.WriteLine($"Mocking {message.Value} for {message.Variable}");
                                subject.OnNext(message);

                                Thread.Sleep(1000);
                            }
                        }
                    }
                }
            });
        }

        public ProjectInfo GetProjectInfo()
        {
            return new ProjectInfo()
            {
                AlarmsDbConnectionString = "",
                AlarmsDbName = "",
                AnalogValuesDbConnectionString = "",
                AnalogValuesDbName = "",
                CurrentProject = "",
                DigitalValuesDbConnectionString = "",
                DigitalValuesDbName = "",
                MainComputer = ""
            };
        }

        public Task<DataStoreMessage> Read(string variable)
        {
            lock (subscriptions)
            {
                if (!subscriptions.Contains(variable.ToLower()))
                {
                    subscriptions.Add(variable.ToLower());
                    Console.WriteLine($"Added subscription for {variable}");
                }
            }

            var message = new DataStoreMessage()
            {
                Timestamp = DateTime.Now,
                Type = DataStoreMessageType.Response,
                Variable = variable,
                Value = random.NextDouble() * 100
            };

            return Task<DataStoreMessage>.Factory.StartNew(() => message);
        }

        public bool Write(DataStoreMessage message)
        {
            return true;
        }
    }
}