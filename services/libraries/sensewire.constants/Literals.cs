using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.constants
{
    public class Literals
    {
        public const string KAFKA_TOPIC_TELEMETRY = "DEVICE_TELEMETRY";
        public const string KAFKA_TOPIC_DEVICESTATUS = "DEVICE_STATUS";
        public const string KAFKA_TOPIC_CONFIG = "DEVICE_CONFIG";

        public const string KAFKA_CONSUMER_GROUP_MQTT_ADAPTER = "MQTT_ADAPTER";
        public const string KAFKA_CONSUMER_GROUP_WSS_ADAPTER = "WSS_ADAPTER";
        public const string KAFKA_CONSUMER_GROUP_STATUS_UPDATE = "STATUS_UPDATE_MANAGER";


    }
}
