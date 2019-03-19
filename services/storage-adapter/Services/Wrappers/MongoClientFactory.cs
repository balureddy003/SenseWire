// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.RegularExpressions;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Runtime;
using MongoDB.Driver;
namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Wrappers
{
    public class MongoClientFactory : IFactory<IMongoClient>
    {
        private readonly Uri docDbEndpoint;
        private readonly string docDbKey;
        private readonly string mongoDbConnectionString;

        public MongoClientFactory(IServicesConfig config, ILogger logger)
        {
            //var match = Regex.Match(config.DocumentDbConnString, "^AccountEndpoint=(?<endpoint>.*);AccountKey=(?<key>.*);$");
            //if (!match.Success)
            //{
            //    var message = "Invalid connection string for Cosmos DB";
            //    logger.Error(message, () => { });
            //    throw new InvalidConfigurationException(message);
            //}

            //this.docDbEndpoint = new Uri(match.Groups["endpoint"].Value);
            //this.docDbKey = match.Groups["key"].Value;

            mongoDbConnectionString = config.MongoDbConnectionString;
        }


        IMongoClient IFactory<IMongoClient>.Create()
        {
            IMongoClient client = null;
            try
            {

                client = new MongoClient(mongoDbConnectionString);
            }
            catch (Exception ex)
            {


            }
            return client;
        }
    }
}
