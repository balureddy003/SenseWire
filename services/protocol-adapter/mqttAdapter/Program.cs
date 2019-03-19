using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sensewire.constants;
using sensewire.entities;
using sensewire.kafka.consumer;
using sensewire.kafka.producer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace mqttAdapter
{
    class Program
    {
        private static IConfigurationRoot config;
        private static IMqttServer mqttServer;
        private static MessageProducer producer;
        public static ManualResetEvent _Shutdown = new ManualResetEvent(false);
        public static ManualResetEventSlim _Complete = new ManualResetEventSlim();

        static int Main(string[] args)
        {
            try
            {
                //var ended = new ManualResetEventSlim();
                //var starting = new ManualResetEventSlim();

                Console.Write("Starting application...");
                config = new ConfigurationBuilder()
                  .AddIniFile("appsettings.ini", optional: false, reloadOnChange: true)
                  .Build();
                ConnectToKafka();
                CreateServer();

                // Capture SIGTERM  
                AssemblyLoadContext.Default.Unloading += Default_Unloading;

                // Wait for a singnal
                _Shutdown.WaitOne();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            finally
            {
                Console.Write("Cleaning up resources");
            }

            Console.Write("Exiting...");
            _Complete.Set();

            return 0;


        }

        private static void Default_Unloading(AssemblyLoadContext obj)
        {
            Console.Write($"Shutting down in response to SIGTERM.");
            _Shutdown.Set();
            _Complete.Wait();
        }
        private static void ConnectToKafka()
        {
            string KafkaURL = config["Kafka:kafkaURL"];
            Console.WriteLine($"Kafka URL : {KafkaURL}");

            producer = MessageProducer.GetInstance;
            var producerConfig = new Dictionary<string, object>{
                { "bootstrap.servers", KafkaURL}
            };
            producer.SetupProducer(producerConfig);


            var consumer = MessageConsumer.GetInstance;
            var subscriberConfig = new Dictionary<string, object>{
                { "group.id", Literals.KAFKA_CONSUMER_GROUP_MQTT_ADAPTER },
                { "bootstrap.servers", KafkaURL}
            };
            consumer.SetupConsumer(subscriberConfig);
            Task.Run(() => consumer.SubscribeTopics(new List<string> { Literals.KAFKA_TOPIC_CONFIG }, SendMessageToClient));


        }
        async private static void CreateServer()
        {
            string ipAddress = config["mqttSettings:ipaddress"];
            int port = Convert.ToInt32(config["mqttSettings:port"]);
            bool useSSL = Convert.ToBoolean(config["mqttSettings:useSSL"]);

            // Setup client validator.
            var optionsBuilder = new MqttServerOptionsBuilder()
               .WithConnectionBacklog(100)
               // .WithDefaultEndpointBoundIPAddress(IPAddress.Parse(ipAddress))
               .WithDefaultEndpointPort(port);

            var options = optionsBuilder.Build() as MqttServerOptions;

            //options.ConnectionValidator = c =>
            //{
            //    ConnectionValidator.ValidateConnection(c);
            //};

            if (useSSL)
            {
                string certificateName = config["mqttSettings:certificateName"];
                X509Certificate cert = null;
                X509Store store = new X509Store(StoreLocation.LocalMachine);
                X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindBySubjectName, certificateName, true);
                if (cers.Count > 0)
                {
                    cert = cers[0];
                };
                options.TlsEndpointOptions.Certificate = cert.Export(X509ContentType.Cert);
            }

            mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.ClientConnected += OnConnected;
            mqttServer.ClientDisconnected += OnDisonnected;
            mqttServer.ClientSubscribedTopic += OnSubscribe;
            mqttServer.ClientUnsubscribedTopic += OnUnsubscribe;
            mqttServer.ApplicationMessageReceived += OnMessage;

            MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
            {
                var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                if (e.TraceMessage.Exception != null)
                {
                    trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                }

                Console.WriteLine(trace);
            };

            await mqttServer.StartAsync(options);

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
            //await mqttServer.StopAsync();
        }

        static void SendMessageToClient(string payload)
        {
            //TODO derive topic from payload
            dynamic ack = JObject.Parse(payload);
            string topic = ack.topic;
            mqttServer.PublishAsync(topic, payload);
        }

        private static void OnConnected(object param, MqttClientConnectedEventArgs args)
        {
            Console.WriteLine("### CLIENT CONNECTING ###");
            SystemEvent systemEvent = new SystemEvent(SystemEventTypesEnum.DeviceOnline, null, null, args.ClientId);
            var jsonMessage = JsonConvert.SerializeObject(systemEvent, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            producer.ProduceMessage(Literals.KAFKA_TOPIC_DEVICESTATUS, jsonMessage);
            Console.WriteLine("### CLIENT CONNECTED ###");
        }
        private static void OnDisonnected(object param, MqttClientDisconnectedEventArgs args)
        {
            SystemEvent systemEvent = new SystemEvent(SystemEventTypesEnum.DeviceOffline, null, null, args.ClientId);
            var jsonMessage = JsonConvert.SerializeObject(systemEvent, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            producer.ProduceMessage(Literals.KAFKA_TOPIC_DEVICESTATUS, jsonMessage);
            Console.WriteLine("### CLIENT DISCONNECTED ###");
        }
        private static void OnSubscribe(object param, MqttClientSubscribedTopicEventArgs args)
        {
            Console.WriteLine("### CLIENT SUBSCRIBED ###");
        }
        private static void OnUnsubscribe(object param, MqttClientUnsubscribedTopicEventArgs args)
        {
            Console.WriteLine("### CLIENT UNSUBSCRIBED ###");
        }
        private static void OnMessage(object param, MqttApplicationMessageReceivedEventArgs args)
        {
            Console.WriteLine("### MESSAGE RECEIVED FROM DEVICE ###");
            if (args.ApplicationMessage.Topic.Contains("telemetry"))
            {
                producer.ProduceMessage(Literals.KAFKA_TOPIC_TELEMETRY, args.ApplicationMessage.ConvertPayloadToString());
            }

        }
    }
}
