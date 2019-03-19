using sensewire.constants;
using sensewire.kafka.consumer;
using sensewire.kafka.producer;
using System;
using System.Collections.Generic;

namespace wssAdapter
{
    class Program
    {
        static void OnMessageReceived(string Message)
        {
            Console.WriteLine(Message);
        }

        static void Main(string[] args)
        {
            var producer = MessageProducer.GetInstance;
            var producerConfig = new Dictionary<string, object>{
                { "bootstrap.servers","127.0.0.1:9092"}
            };
            producer.SetupProducer(producerConfig);
            for (int i = 0; i < 100; i++)
            {
                producer.ProduceMessage(Literals.KAFKA_TOPIC_TELEMETRY, String.Format("message {0}", i));
            }


            var consumer = MessageConsumer.GetInstance;
            var subscriberConfig = new Dictionary<string, object>{
                { "group.id", Literals.KAFKA_CONSUMER_GROUP_WSS_ADAPTER },
                { "bootstrap.servers", "127.0.0.1:9092"}
            };
            consumer.SetupConsumer(subscriberConfig);
            consumer.SubscribeTopics(new List<string> { Literals.KAFKA_TOPIC_CONFIG }, OnMessageReceived);

            Console.ReadLine();
        }
    }
}