using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace sensewire.kafka.producer
{
    public class MessageProducer
    {
        private static Producer<Null, string> producer;
        private static readonly object Instancelock = new object();
        private MessageProducer()
        {
            Console.WriteLine("Producer initialized");
        }
        private static MessageProducer instance = null;

        public static MessageProducer GetInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (Instancelock)
                    {
                        if (instance == null)
                        {
                            instance = new MessageProducer();
                        }
                    }
                }
                return instance;
            }
        }
        public void ProduceMessage(string topic, string message)
        {
            Task.Run(() =>
           {
               var result = producer.ProduceAsync(topic, null, message).GetAwaiter().GetResult();
               Console.WriteLine($"Event sent on Partition: {result.Partition} with Offset: {result.Offset}");
           }
            );
        }

        public void SetupProducer(Dictionary<string, object> config)
        {
            producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8));
        }
    }
}
