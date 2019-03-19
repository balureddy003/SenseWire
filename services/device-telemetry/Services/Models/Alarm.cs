// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.Documents;
using MongoDB.Bson;

namespace Microsoft.Azure.IoTSolutions.DeviceTelemetry.Services.Models
{
    public class Alarm
    {
        public string ETag { get; set; }
        public string Id { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset DateModified { get; set; }
        public string Description { get; set; }
        public string GroupId { get; set; }
        public string DeviceId { get; set; }
        public string Status { get; set; }
        public string RuleId { get; set; }
        public string RuleSeverity { get; set; }
        public string RuleDescription { get; set; }

        public Alarm(
            string etag,
            string id,
            long dateCreated,
            long dateModified,
            string description,
            string groupId,
            string deviceId,
            string status,
            string ruleId,
            string ruleSeverity,
            string ruleDescription)
        {
            this.ETag = etag;
            this.Id = id;
            this.DateCreated = DateTimeOffset.FromUnixTimeMilliseconds(dateCreated);
            this.DateModified = DateTimeOffset.FromUnixTimeMilliseconds(dateModified);
            this.Description = description;
            this.GroupId = groupId;
            this.DeviceId = deviceId;
            this.Status = status;
            this.RuleId = ruleId;
            this.RuleSeverity = ruleSeverity;
            this.RuleDescription = ruleDescription;
        }

        public Alarm(BsonDocument doc)
        {
            if (doc != null)
            {
                this.ETag = doc["ETag"].AsString;
                this.Id = doc["Id"].AsString;
                //this.DateCreated = DateTimeOffset.FromUnixTimeMilliseconds(doc.DateCreated.Ticks);
                //this.DateModified = DateTimeOffset.FromUnixTimeMilliseconds(doc.DateModified.Ticks);
                this.Description = doc["Description"].AsString;
                this.GroupId = doc["GroupId"].AsString;
                this.DeviceId = doc["DeviceId"].AsString;
                this.Status = doc["Status"].AsString;
                this.RuleId = doc["RuleId"].AsString;
                this.RuleSeverity = doc["RuleSeverity"].AsString;
                this.RuleDescription = doc["RuleDescription"].AsString;
            }
        }
    }
}
