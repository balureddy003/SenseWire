using Akka.Actor;
using DeviceTwinManager.Actors;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sensewire.constants;
using sensewire.entities;
using sensewire.kafka.consumer;
using sensewire.kafka.producer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTwinUpdateAgent
{
    public interface IAgent
    {
        void Run();
    }

    public class Agent : IAgent
    {
        private readonly IDevices _devices;
        private readonly IServicesConfig _config;
        private MessageProducer _producer;
        private IActorRef _deviceManager;
        public Agent(IDevices devices, IServicesConfig config)
        {
            this._devices = devices;
            this._config = config;
            var system = ActorSystem.Create("sensewire");
            _deviceManager = system.ActorOf(Props.Create<DeviceManager>(), "device-manager");
        }

        public void Run()
        {
            string KafkaURL = _config.KafkaUrl; 
            Console.WriteLine($"Kafka URL : {KafkaURL}");

            var consumer = MessageConsumer.GetInstance;
            var subscriberConfig = new Dictionary<string, object>{
                { "group.id", Literals.KAFKA_CONSUMER_GROUP_STATUS_UPDATE },
                { "bootstrap.servers", KafkaURL}
            };

            var producer = MessageProducer.GetInstance;
            var producerConfig = new Dictionary<string, object>{
                { "bootstrap.servers", KafkaURL}
            };
            producer.SetupProducer(producerConfig);
            consumer.SetupConsumer(subscriberConfig);
            consumer.SubscribeTopics(new List<string> { Literals.KAFKA_TOPIC_DEVICESTATUS }, OnMessageReceived);

        }

        private void OnMessageReceived(string json)
        {
            Task.Run(async () =>
            {
                SystemEvent systemEvent = JsonConvert.DeserializeObject<SystemEvent>(json);
                //update device status
                if (systemEvent.EventType == SystemEventTypesEnum.DeviceOnline || systemEvent.EventType == SystemEventTypesEnum.DeviceOffline)
                {
                    _deviceManager.Tell(systemEvent);
                    await this._devices.UpdateStatusAsync(systemEvent.EntityId, systemEvent.EventType == SystemEventTypesEnum.DeviceOnline ? true : false);
                    Console.WriteLine("Device status updated");
                }

                // Frame message for ack
                //dynamic message = new ExpandoObject();
                //message.Type = "Acknowledgement";
                //message.topic = ID;
                //var jsonMessage = JsonConvert.SerializeObject(message);

                ////send message to kafaka producer
                //_producer.ProduceMessage(Literals.KAFKA_TOPIC_CONFIG, jsonMessage);
            });
        }
    }
}
