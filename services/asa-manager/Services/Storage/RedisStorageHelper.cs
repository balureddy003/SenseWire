// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.AsaManager.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.AsaManager.Services.Models;
using Microsoft.Azure.IoTSolutions.AsaManager.Services.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using StackExchange.Redis;

namespace Microsoft.Azure.IoTSolutions.AsaManager.Services.Storage
{
    public interface IRedisStorageHelper
    {
        Task WriteBlobFromFileAsync(string blobName, string fileName);
        Task<StatusResultServiceModel> PingAsync();
    }

    public class RedisStorageHelper : IRedisStorageHelper
    {
        private readonly ICloudStorageWrapper cloudStorageWrapper;
        private CloudBlobContainer cloudBlobContainer;
        private readonly IBlobStorageConfig blobStorageConfig;
        private readonly ILogger logger;
        private bool isInitialized;
        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            string cacheConnection = "";
            return ConnectionMultiplexer.Connect(cacheConnection);
        });
        public RedisStorageHelper(
            IBlobStorageConfig blobStorageConfig,
            ICloudStorageWrapper cloudStorageWrapper,
            ILogger logger)
        {
            this.logger = logger;
            this.blobStorageConfig = blobStorageConfig;
            this.cloudStorageWrapper = cloudStorageWrapper;
            this.isInitialized = false;
        }

        //private async Task InitializeBlobStorage()
        //{
        //    if (!this.isInitialized)
        //    {
        //        //string storageConnectionString =
        //        //    $"DefaultEndpointsProtocol=https;AccountName={this.blobStorageConfig.AccountName};AccountKey={this.blobStorageConfig.AccountKey};EndpointSuffix={this.blobStorageConfig.EndpointSuffix}";
        //        //CloudStorageAccount account = this.cloudStorageWrapper.Parse(storageConnectionString);
        //        //CloudBlobClient blobClient = this.cloudStorageWrapper.CreateCloudBlobClient(account);

        //        //this.cloudBlobContainer = this.cloudStorageWrapper.GetContainerReference(blobClient, this.blobStorageConfig.ReferenceDataContainer);
        //        //await this.cloudStorageWrapper.CreateIfNotExistsAsync(
        //        //    this.cloudBlobContainer,
        //        //    BlobContainerPublicAccessType.Blob, 
        //        //    new BlobRequestOptions(), 
        //        //    new OperationContext());

        //        IDatabase cache = lazyConnection.Value.GetDatabase();

        //        this.isInitialized = true;
        //    }
        //}

        public async Task WriteBlobFromFileAsync(string blobName, string fileName)
        {
            try
            {
                IDatabase cache = lazyConnection.Value.GetDatabase();
                CloudBlockBlob blockBlob = this.cloudStorageWrapper.GetBlockBlobReference(this.cloudBlobContainer, blobName);
                await this.cloudStorageWrapper.UploadFromFileAsync(blockBlob, fileName);
            }
            catch (Exception e)
            {
                this.logger.Error("Unable to upload reference data to blob", () => new { e });
            }
        }

        public async Task<StatusResultServiceModel> PingAsync()
        {
            var result = new StatusResultServiceModel(false, "Blob check failed");

            try
            {
                IDatabase cache = lazyConnection.Value.GetDatabase();
                var response = await cache.ExecuteAsync("PING");
                if (response.ToString()== "PONG")
                {
                    result.Message = "Alive and well!";
                    result.IsHealthy = true;
                }
            }
            catch (Exception e)
            {
                this.logger.Error(result.Message, () => new { e });
            }

            return result;
        }
    }
}
