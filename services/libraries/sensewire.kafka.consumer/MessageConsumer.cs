using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.kafka.consumer
{
    public class MessageConsumer
    {
        private static Consumer<Null, string> consumer;
        private static readonly object Instancelock = new object();
        private MessageConsumer()
        {
            Console.WriteLine("Consumer initialized");
        }
        private static MessageConsumer instance = null;

        public static MessageConsumer GetInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (Instancelock)
                    {
                        if (instance == null)
                        {
                            instance = new MessageConsumer();
                        }
                    }
                }
                return instance;
            }
        }
        public void SubscribeTopics(List<string> topic, Action<string> onMessageReceived)
        {
            consumer.Subscribe(topic);

            consumer.OnMessage += (_, msg) =>
            {
                onMessageReceived(msg.Value);
            };

            while (true)
            {
                consumer.Poll(1);
            }
        }

        public void SetupConsumer(Dictionary<string, object> config)
        {
            consumer = new Consumer<Null, string>(config, null, new StringDeserializer(Encoding.UTF8));
        }
    }
}
