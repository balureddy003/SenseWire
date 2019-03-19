// Copyright (c) Microsoft. All rights reserved.

using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Models
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId  _id { get; set; }
        [JsonProperty("DeviceId")]
        public string DeviceId { get; set; }
        [JsonProperty("Time")]
        public DateTimeOffset Time { get; set; }
        [JsonProperty("Data")]
        public JObject Data { get; set; }

        public Message()
        {
            this.DeviceId = string.Empty;
            this.Time = DateTimeOffset.UtcNow;
            this.Data = null;
        }

        public Message(
            string deviceId,
            long time,
            JObject data)
        {
            this.DeviceId = deviceId;
            this.Time = DateTimeOffset.FromUnixTimeMilliseconds(time);
            this.Data = data;
        }
    }
}
