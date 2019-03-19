// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Helpers;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Models;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Runtime;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Wrappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services
{
    public sealed class MongoDbKeyValueContainer : IKeyValueContainer, IDisposable
    {
        private readonly IMongoClient client;
        private readonly IMongoDatabase database;
        private readonly IExceptionChecker exceptionChecker;
        private readonly ILogger log;
        private bool disposedValue;

        //private readonly string docDbDatabase;
        // private readonly string docDbCollection;
        // private readonly int docDbRUs;
        //private readonly RequestOptions docDbOptions;
        // private string collectionLink;

        public MongoDbKeyValueContainer(
            IFactory<IMongoClient> clientFactory,
            IExceptionChecker exceptionChecker,
            IServicesConfig config,
            ILogger logger)
        {
           
            this.client = clientFactory.Create();
            this.database = this.client.GetDatabase("database");
            this.exceptionChecker = exceptionChecker;
            this.log = logger;
            this.disposedValue = false;

            //this.docDbDatabase = config.DocumentDbDatabase;
            //this.docDbCollection = config.DocumentDbCollection;
            //this.docDbRUs = config.DocumentDbRUs;
            // this.docDbOptions = this.GetDocDbOptions();
        }

        public async Task<StatusResultServiceModel> PingAsync()
        {
            var result = new StatusResultServiceModel(false, "Storage check failed");

            try
            {
                IAsyncCursor<BsonDocument> response = null;
                if (this.client != null)
                {
                    // make generic call to see if storage client can be reached
                    response = await this.client.ListDatabasesAsync();
                }

                if (response.ToList().Count >0)
                {
                    result.IsHealthy = true;
                    result.Message = "Alive and Well!";
                }
            }
            catch (Exception e)
            {
                this.log.Info(result.Message, () => new { e });
            }

            return result;
        }

        public async Task<ValueServiceModel> GetAsync(string collectionId, string key)
        {

            try
            {
                var collection = database.GetCollection<ValueServiceModel>(collectionId);
                var response = collection.Find(a=>a.objectid == key).FirstOrDefault();
                return response;
  
            }
            catch (Exception ex)
            {
                if (!this.exceptionChecker.IsNotFoundException(ex)) throw;

                const string message = "The resource requested doesn't exist.";
                this.log.Info(message, () => new
                {
                    collectionId,
                    key
                });

                throw new ResourceNotFoundException(message);
            }
        }

        public async Task<IEnumerable<ValueServiceModel>> GetAllAsync(string CollectionName)
        {
            var collection = database.GetCollection<ValueServiceModel>(CollectionName);
            
            var result = await collection.Find(new BsonDocument()).ToListAsync();
            return result;

        }

        public async Task<ValueServiceModel> CreateAsync(string collectionId, string key, ValueServiceModel model)
        {

            try
            {
                var collection = database.GetCollection<ValueServiceModel>(collectionId);
                await collection.InsertOneAsync(model);
                return model;

            }
            catch (Exception ex)
            {
                if (!this.exceptionChecker.IsConflictException(ex)) throw;
                const string message = "There is already a value with the key specified.";
                this.log.Info(message, () => new { collectionId, key });
                throw new ConflictingResourceException(message);
            }
        }

        public async Task<ValueServiceModel> UpsertAsync(string collectionId, string key, ValueServiceModel input)
        {

            try
            {
                var collection = database.GetCollection<ValueServiceModel>(collectionId);
                var response =await collection.ReplaceOneAsync(a => a.objectid == key, input);
                return input;
            }
            catch (Exception ex)
            {
                if (!this.exceptionChecker.IsPreconditionFailedException(ex)) throw;

                const string message = "ETag mismatch: the resource has been updated by another client.";
                this.log.Info(message, () => new { collectionId, key });
                throw new ConflictingResourceException(message);
            }
        }

        public async Task DeleteAsync(string collectionId, string key)
        {

            try
            {
                var collection = database.GetCollection<ValueServiceModel>(collectionId);
                var response = collection.DeleteOne(a => a.objectid == key);
            }
            catch (Exception ex)
            {
                if (!this.exceptionChecker.IsNotFoundException(ex)) throw;
                this.log.Debug("Key does not exist, nothing to do", () => new { key });
            }
        }

        //private RequestOptions GetDocDbOptions()
        //{
        //    return new RequestOptions
        //    {
        //        OfferThroughput = this.docDbRUs,
        //        ConsistencyLevel = ConsistencyLevel.Strong
        //    };
        //}

        //private async Task SetupStorageAsync()
        //{
        //        //await this.CreateDatabaseIfNotExistsAsync();
        //        //await this.CreateCollectionIfNotExistsAsync();
        //}

        //private  async Task<ValueServiceModel> CreateDatabaseIfNotExistsAsync()
        //    {
        //    try
        //    {
        //        var dbs = await this.client.ListDatabasesAsync();
        //        if (dbs.ToList().Count() == 0)
        //        {
        //            this.client.GetDatabase(database);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    return null;
        //}

        //public async Task<ValueServiceModel> CreateCollectionIfNotExistsAsync()
        //{
        //    try
        //    {
        //        var db = this.client.GetDatabase("database");
        //        var collection = await db.ListCollectionsAsync();

        //        if (collection.ToList().Count() == 0)
        //        {
        //            await db.CreateCollectionAsync("students", new CreateCollectionOptions
        //            {

        //                MaxDocuments = 25,
        //                Capped = true,
        //                MaxSize = 100
        //            });
        //        }
        //    }
        //    catch (Exception  ex)
        //    {
        //        throw ex;
        //    }
        //     return null;
        //}

        //private async Task CreateDatabaseAsync()
        //{
        //    try
        //    {
        //        this.log.Info("Creating DocumentDb database",
        //            () => new { this.docDbDatabase });
        //        var db = new Database { Id = this.docDbDatabase };
        //        await this.client.CreateDatabaseAsync(db);
        //    }
        //    catch (DocumentClientException e)
        //    {
        //        if (e.StatusCode == HttpStatusCode.Conflict)
        //        {
        //            this.log.Warn("Another process already created the database",
        //                () => new { this.docDbDatabase });
        //        }

        //        this.log.Error("Error while creating DocumentDb database",
        //            () => new { this.docDbDatabase, e });
        //    }
        //    catch (Exception e)
        //    {
        //        this.log.Error("Error while creating DocumentDb database",
        //            () => new { this.docDbDatabase, e });
        //        throw;
        //    }
        //}

        //private async Task CreateCollectionAsync()
        //{
        //    try
        //    {
        //        this.log.Info("Creating DocumentDb collection",
        //            () => new { this.docDbCollection });
        //        var coll = new DocumentCollection { Id = this.docDbCollection };

        //        var index = Index.Range(DataType.String, -1);
        //        var indexing = new IndexingPolicy(index) { IndexingMode = IndexingMode.Consistent };
        //        coll.IndexingPolicy = indexing;

        //        // Partitioning can be enabled in case the storage adapter is used to store 100k+ records
        //        //coll.PartitionKey = new PartitionKeyDefinition { Paths = new Collection<string> { "/CollectionId" } };

        //        var dbUri = "/dbs/" + this.docDbDatabase;
        //        await this.client.CreateDocumentCollectionAsync(dbUri, coll, this.docDbOptions);
        //    }
        //    catch (DocumentClientException e)
        //    {
        //        if (e.StatusCode == HttpStatusCode.Conflict)
        //        {
        //            this.log.Warn("Another process already created the collection",
        //                () => new { this.docDbCollection });
        //        }

        //        this.log.Error("Error while creating DocumentDb collection",
        //            () => new { this.docDbCollection, e });
        //    }
        //    catch (Exception e)
        //    {
        //        this.log.Error("Error while creating DocumentDb collection",
        //            () => new { this.docDbDatabase, e });
        //        throw;
        //    }
        //}

        //private static RequestOptions IfMatch(string etag)
        //{
        //    if (etag == "*")
        //    {
        //        // Match all
        //        return null;
        //    }
        //    return new RequestOptions
        //    {
        //        AccessCondition = new AccessCondition
        //        {
        //            Condition = etag,
        //            Type = AccessConditionType.IfMatch
        //        }
        //    };
        //}

        #region IDisposable Support

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    (this.client as IDisposable)?.Dispose();
                }
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        //public Task<ValueServiceModel> GetAsync(string collectionId, string key)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<IEnumerable<ValueServiceModel>> GetAllAsync(string collectionId)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<ValueServiceModel> CreateAsync(string collectionId, string key, ValueServiceModel input)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<ValueServiceModel> UpsertAsync(string collectionId, string key, ValueServiceModel input)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task DeleteAsync(string collectionId, string key)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion
    }
}
