// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Azure.IoTSolutions.AsaManager.Services.Storage
{
    public interface IMongoDbSqlWrapper
    {
        /// <summary>Wrap CosmosDb API for testability. Create a DB if it doesn't exist</summary>
        /// <param name="uri">CosmosDb URI</param>
        /// <param name="authKey">Authentication key</param>
        /// <param name="consistencyLevel">Consistency level</param>
        /// <param name="database">Database Id</param>
        Task CreateDatabaseIfNotExistsAsync(
           string connectionString,
            string database);

        /// <summary>Wrap CosmosDb API for testability. Create a collection if it doesn't exist</summary>
        /// <param name="uri">CosmosDb URI</param>
        /// <param name="authKey">Authentication key</param>
        /// <param name="consistencyLevel">Consistency level</param>
        /// <param name="database">Database Id</param>
        /// <param name="collection">Collection Id</param>
        /// <param name="RUs">Collection capacity in RUs</param>
        Task CreateDocumentCollectionIfNotExistsAsync(
           string connectionString,
            string database,
            string collection,
            int RUs);

        Task ReadDatabaseAsync( string connectionString, string database);
    }

    public class MongoDbSqlWrapper : IMongoDbSqlWrapper
    {
        /// <summary>Wrap CosmosDb API for testability. Create a DB if it doesn't exist</summary>
        /// <param name="uri">CosmosDb URI</param>
        /// <param name="authKey">Authentication key</param>
        /// <param name="consistencyLevel">Consistency level</param>
        /// <param name="database">Database Id</param>
        public async Task CreateDatabaseIfNotExistsAsync(
           string connectionString,
            string database)
        {
            var client = new MongoClient(connectionString);
            await Task.FromResult(client.GetDatabase(database));
        }

        /// <summary>Wrap CosmosDb API for testability. Create a collection if it doesn't exist</summary>
        /// <param name="uri">CosmosDb URI</param>
        /// <param name="authKey">Authentication key</param>
        /// <param name="consistencyLevel">Consistency level</param>
        /// <param name="database">Database Id</param>
        /// <param name="collection">Collection Id</param>
        /// <param name="RUs">Collection capacity in RUs</param>
        public async Task CreateDocumentCollectionIfNotExistsAsync(
            string connectionString,
            string database,
            string collection,
            int RUs)
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(database);
            db.GetCollection<BsonDocument>(collection);

        }

        public async Task ReadDatabaseAsync(string connectionString, string database)
        {
            var client = new MongoClient(connectionString);
            await Task.FromResult(client.GetDatabase(database));
        }
    }
}
