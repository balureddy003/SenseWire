// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Storage.StorageAdapter
{
    public class ValueApiModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string objectid { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("Data")]
        public string Data { get; set; }

        [JsonProperty("ETag")]
        public string ETag { get; set; }

        [JsonProperty("$metadata")]
        public Dictionary<string, string> Metadata;

        public ValueApiModel()
        {
            this.Metadata = new Dictionary<string, string>();
        }
    }
}
