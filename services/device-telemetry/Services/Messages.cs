// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Helpers;
using Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Storage.CosmosDB;
using Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Storage.TimeSeries;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services
{
    public interface IMessages
    {
        Task<MessageList> ListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices);
    }

    public class Messages : IMessages
    {
        private const string DATA_PROPERTY_NAME = "data";
        private const string DATA_PREFIX = DATA_PROPERTY_NAME + ".";
        private const string DATA_SCHEMA_TYPE = DATA_PREFIX + "schema";
        private const string DATA_PARTITION_ID = "PartitionId";
        private const string TSI_STORAGE_TYPE_KEY = "tsi";

        private readonly ILogger log;
        private readonly IStorageClient storageClient;
        private readonly ITimeSeriesClient timeSeriesClient;

        private readonly bool timeSeriesEnabled;
        private readonly IMongoClient documentClient;
        private readonly string databaseName;
        private readonly string collectionId;

        public Messages(
            IServicesConfig config,
            IStorageClient storageClient,
            ITimeSeriesClient timeSeriesClient,
            ILogger logger)
        {
            this.storageClient = storageClient;
            this.timeSeriesClient = timeSeriesClient;
            this.timeSeriesEnabled = config.StorageType.Equals(
                TSI_STORAGE_TYPE_KEY, StringComparison.OrdinalIgnoreCase);
            this.documentClient = storageClient.GetDocumentClient();
            this.databaseName = config.MessagesConfig.CosmosDbDatabase;
            this.collectionId = config.MessagesConfig.CosmosDbCollection;
            this.log = logger;
        }

        public async Task<MessageList> ListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            InputValidator.Validate(order);
            foreach (var device in devices)
            {
                InputValidator.Validate(device);
            }

            return this.timeSeriesEnabled ? 
                await this.GetListFromTimeSeriesAsync(from, to, order, skip, limit, devices) : 
                await this.GetListFromCosmosDb(from, to, order, skip, limit, devices);
        }

        private async Task<MessageList> GetListFromTimeSeriesAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            return await this.timeSeriesClient.QueryEventsAsync(from, to, order, skip, limit, devices);
        }

        private async Task<MessageList> GetListFromCosmosDb(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            int dataPrefixLen = DATA_PREFIX.Length;

            var sql = QueryBuilder.GetDocumentsSql(
                "d2cmessage",
                null, null,
                from, "device.msg.received",
                to, "device.msg.received",
                order, "device.msg.received",
                skip,
                limit,
                devices, "device.id");

            this.log.Debug("Created Message Query", () => new { sql });

            FeedOptions queryOptions = new FeedOptions();
            queryOptions.EnableCrossPartitionQuery = true;
            queryOptions.EnableScanInQuery = true;

            List<BsonDocument> docs =await this.storageClient.QueryDocumentsAsync(
                this.databaseName,
                this.collectionId,
                queryOptions,
                sql,
                skip,
                limit);

            // Messages to return
            List<Message> messages = new List<Message>();

            // Auto discovered telemetry types
            HashSet<string> properties = new HashSet<string>();

            foreach (var doc in docs)
            {
                // Document fields to expose
                //JObject data = new JObject();
                try {
                    //var msg = BsonSerializer.Deserialize<Message>(doc);
                    JObject data = new JObject();
                    var jsonDoc = JObject.Parse(doc["Data"].ToString());
                    var msg = new Message();
                    msg.DeviceId = doc["DeviceId"].AsString;
                    msg.Time =DateTime.Parse(doc["Time"].ToString());
                    msg.Data= jsonDoc;


                    // myClass.Data = doc["Data"].AsString;
                    // Extract all the telemetry data and types
                    //var jsonDoc = JObject.Parse(doc.ToString());

                    //string dataSchema = jsonDoc.GetValue(DATA_SCHEMA_TYPE)?.ToString();
                    //SchemaType schemaType;
                    //Enum.TryParse(dataSchema, true, out schemaType);

                    //switch (schemaType)
                    //{
                    //    // Process messages output by streaming jobs
                    //    case SchemaType.StreamingJobs:
                    //        data = (JObject)jsonDoc.GetValue(DATA_PROPERTY_NAME);
                    //        if (data != null)
                    //        {
                    //            // Filter PartitionId property sometimes generated by ASA query by default
                    //            properties.UnionWith(data.Properties()
                    //                .Where(p => !p.Name.Equals(DATA_PARTITION_ID, StringComparison.OrdinalIgnoreCase))
                    //                .Select(p => p.Name));
                    //        };
                    //        break;
                    //    // Process messages output by telemetry agent
                    //    default:
                    //        foreach (var item in jsonDoc)
                    //        {
                    //            // Ignore fields that don't start with "data."
                    //            if (item.Key.StartsWith(DATA_PREFIX))
                    //            {
                    //                // Remove the "data." prefix
                    //                string key = item.Key.ToString().Substring(dataPrefixLen);
                    //                data.Add(key, item.Value);

                    //                // Telemetry types auto-discovery magic through union of all keys
                    //                properties.Add(key);
                    //            }
                    //        }
                    //        break;
                    //}
                    messages.Add(msg);
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }

            return new MessageList(messages, new List<string>(properties));
        }
    }
}
