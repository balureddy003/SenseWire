// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Helpers;
using MongoDB.Bson;

[assembly: InternalsVisibleTo("Services.Test")]

namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services
{
    internal sealed class KeyValueDocument : Resource
    {
        public string CollectionId { get; }
        public string Key { get; }
        public string Data { get; }

        public KeyValueDocument(string collectionId, string key, string data)
        {
            
            this.Key = key;
            this.Data = data;
        }
    }
}
