// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Extensions;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.External;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Helpers;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Runtime;
using DeviceJobStatus = Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models.DeviceJobStatus;
using azureDeviceJobStatus = Microsoft.Azure.Devices.DeviceJobStatus;
using JobStatus = Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models.JobStatus;
using JobType = Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models.JobType;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.IotHubManager.Services
{
    public interface IJobs
    {
        Task<IEnumerable<JobServiceModel>> GetJobsAsync(
            JobType? jobType,
            JobStatus? jobStatus,
            int? pageSize,
            string queryFrom,
            string queryTo);

        Task<JobServiceModel> GetJobsAsync(
            string jobId,
            bool? includeDeviceDetails,
            DeviceJobStatus? deviceJobStatus);

        Task<JobServiceModel> ScheduleDeviceMethodAsync(
            string jobId,
            string queryCondition,
            MethodParameterServiceModel parameter,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds);

        Task<JobServiceModel> ScheduleTwinUpdateAsync(
            string jobId,
            string queryCondition,
            TwinServiceModel twin,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds);
    }

    public class Jobs : IJobs
    {
        private JobClient jobClient;
        private RegistryManager registryManager;
        private IDeviceProperties deviceProperties;
        private readonly IStorageAdapterClient client;
        internal const string DEVICE_JOBS_COLLECTION_ID = "devicesJobs";
        private const string DEVICE_DETAILS_QUERY_FORMAT = "select * from devices.jobs where devices.jobs.jobId = '{0}'";
        private const string DEVICE_DETAILS_QUERYWITH_STATUS_FORMAT = "select * from devices.jobs where devices.jobs.jobId = '{0}' and devices.jobs.status = '{1}'";

        public Jobs(IServicesConfig config, IDeviceProperties deviceProperties, IStorageAdapterClient client)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            this.deviceProperties = deviceProperties;
            this.client = client;
            IoTHubConnectionHelper.CreateUsingHubConnectionString(
                config.IoTHubConnString,
                conn => { this.jobClient = JobClient.CreateFromConnectionString(conn); });

            IoTHubConnectionHelper.CreateUsingHubConnectionString(
                config.IoTHubConnString,
                conn => { this.registryManager = RegistryManager.CreateFromConnectionString(conn); });
        }

        public async Task<IEnumerable<JobServiceModel>> GetJobsAsync(
            JobType? jobType,
            JobStatus? jobStatus,
            int? pageSize,
            string queryFrom,
            string queryTo)
        {
            var from = DateTimeOffsetExtension.Parse(queryFrom, DateTimeOffset.MinValue);
            var to = DateTimeOffsetExtension.Parse(queryTo, DateTimeOffset.MaxValue);
            var data = await this.client.GetAllAsync(DEVICE_JOBS_COLLECTION_ID);
            var Coverteddata= data.Items.Select(CreatejobServiceModel);
            //var query = this.jobClient.CreateQuery(
            //    JobServiceModel.ToJobTypeAzureModel(jobType),
            //    JobServiceModel.ToJobStatusAzureModel(jobStatus),
            //    pageSize);
           
            var results = new List<JobServiceModel>();
            results.AddRange(Coverteddata
                .Where(j => j.CreatedTimeUtc >= from && j.CreatedTimeUtc <= to 
                && (jobType==null|| j.Type.ToString() == JobServiceModel.ToJobTypeAzureModel(jobType).ToString() )
                && (jobStatus == null || j.Status.ToString() == JobServiceModel.ToJobStatusAzureModel(jobStatus).ToString()))
                .Select(r => r));
            //while (query.HasMoreResults)
            //{
            //    var jobs = await query.GetNextAsJobResponseAsync();
             
            //}

            return results;
        }

        public async Task<JobServiceModel> GetJobsAsync(
            string jobId,
            bool? includeDeviceDetails,
            DeviceJobStatus? deviceJobStatus)
        {
            var data = await this.client.GetAsync(DEVICE_JOBS_COLLECTION_ID,jobId);
            var result = this.CreatejobServiceModel(data);

            if (!includeDeviceDetails.HasValue || !includeDeviceDetails.Value)
            {
                return result;
            }

            // Device job query by status of 'Completed' or 'Cancelled' will fail with InternalServerError
            // https://github.com/Azure/azure-iot-sdk-csharp/issues/257
            var queryString = deviceJobStatus.HasValue ?
                string.Format(DEVICE_DETAILS_QUERYWITH_STATUS_FORMAT, jobId, deviceJobStatus.Value.ToString().ToLower()) :
                string.Format(DEVICE_DETAILS_QUERY_FORMAT, jobId);

            //var query = this.registryManager.CreateQuery(queryString);

            var deviceJobs = new List<DeviceJob>();
            foreach (var devicejob in result.Devices)
            {
                var _devicejob = new DeviceJob();
                var type = result.Type.ToString();
                _devicejob.CreatedDateTimeUtc = devicejob.CreatedDateTimeUtc;
                _devicejob.DeviceId = devicejob.DeviceId;
                _devicejob.EndTimeUtc = devicejob.EndTimeUtc;
                _devicejob.JobType = (DeviceJobType)result.Type;
                //_devicejob.Error = devicejob.Error;
                _devicejob.LastUpdatedDateTimeUtc = devicejob.LastUpdatedDateTimeUtc;
                _devicejob.Status = (azureDeviceJobStatus)devicejob.Status;
                _devicejob.StartTimeUtc = devicejob.StartTimeUtc;
                //_devicejob.Outcome = devicejob.Outcome;
                _devicejob.JobId = jobId;
             
                deviceJobs.Add(_devicejob);
            }
            return new JobServiceModel(result, deviceJobs);
        }

        public async Task<JobServiceModel> ScheduleTwinUpdateAsync(
            string jobId,
            string queryCondition,
            TwinServiceModel twin,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds)
        {
            //var result = await this.jobClient.ScheduleTwinUpdateAsync(
            //    jobId,
            //    queryCondition,
            //    twin.ToAzureModel(),
            //    startTimeUtc.DateTime,
            //    maxExecutionTimeInSeconds);

           
            var devicelistString=queryCondition.Replace("deviceId in", "").Trim();
            var devicelist = JsonConvert.DeserializeObject<List<dynamic>>(devicelistString);
            List<DeviceJobServiceModel> devicemodellist = new List<DeviceJobServiceModel>(); 
            foreach (var item in devicelist)
            {
                DeviceJobServiceModel data = new DeviceJobServiceModel();
                data.DeviceId = item;
                data.Status = DeviceJobStatus.Scheduled;
                data.CreatedDateTimeUtc = DateTime.UtcNow;
                devicemodellist.Add(data);
            }
            var devicecount = devicemodellist.Count();
            JobServiceModel json = new JobServiceModel();
            json.CreatedTimeUtc = DateTime.UtcNow;
            json.Devices = devicemodellist.ToList();
            json.Status = JobStatus.Scheduled;
            json.UpdateTwin = twin;
            json.Type = JobType.ScheduleUpdateTwin;
            JobStatistics ResultStatistics = new JobStatistics(); 
            ResultStatistics.DeviceCount = devicecount;
            ResultStatistics.SucceededCount = 0;
            ResultStatistics.FailedCount = 0;
            ResultStatistics.PendingCount = 0;
            ResultStatistics.RunningCount = 0;
            json.ResultStatistics = ResultStatistics;
            var value= JsonConvert.SerializeObject(json, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var result = await this.client.CreateAsync(DEVICE_JOBS_COLLECTION_ID, value);
            var Job = this.CreatejobServiceModel(result);
            // Update the deviceProperties cache, no need to wait
            var model = new DevicePropertyServiceModel();

            var tagRoot = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(twin.Tags)) as JToken;
            if (tagRoot != null)
            {
                model.Tags = new HashSet<string>(tagRoot.GetAllLeavesPath());
            }

            var reportedRoot = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(twin.ReportedProperties)) as JToken;
            if (reportedRoot != null)
            {
                model.Reported = new HashSet<string>(reportedRoot.GetAllLeavesPath());
            }
            var unused = deviceProperties.UpdateListAsync(model);

            return Job;
        }

        public async Task<JobServiceModel> ScheduleDeviceMethodAsync(
            string jobId,
            string queryCondition,
            MethodParameterServiceModel parameter,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds)
        {
            //var result = await this.jobClient.ScheduleDeviceMethodAsync(
            //    jobId, queryCondition,
            //    parameter.ToAzureModel(),
            //    startTimeUtc.DateTime,
            //    maxExecutionTimeInSeconds);
            var devicelistString = queryCondition.Replace("deviceId in", "").Trim();
            var devicelist = JsonConvert.DeserializeObject<List<dynamic>>(devicelistString);
            List<DeviceJobServiceModel> devicemodellist = new List<DeviceJobServiceModel>();
            foreach (var item in devicelist)
            {
                DeviceJobServiceModel data = new DeviceJobServiceModel();
                data.DeviceId = item;
                data.Status = DeviceJobStatus.Scheduled;
                data.CreatedDateTimeUtc = DateTime.UtcNow;
                devicemodellist.Add(data);
            }
            var devicecount = devicemodellist.Count();
            JobServiceModel json = new JobServiceModel();
            json.CreatedTimeUtc = DateTime.UtcNow;
            json.Devices = devicemodellist.ToList();
            json.Status = JobStatus.Scheduled;
            json.MethodParameter = parameter;
            json.Type = JobType.ScheduleUpdateTwin;
            JobStatistics ResultStatistics = new JobStatistics();
            ResultStatistics.DeviceCount = devicecount;
            ResultStatistics.SucceededCount = 0;
            ResultStatistics.FailedCount = 0;
            ResultStatistics.PendingCount = 0;
            ResultStatistics.RunningCount = 0;
            json.ResultStatistics = ResultStatistics;
            var value = JsonConvert.SerializeObject(json, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var result = await this.client.CreateAsync(DEVICE_JOBS_COLLECTION_ID, value);
            var Job = this.CreatejobServiceModel(result);
            return Job;
        }
        private JobServiceModel CreatejobServiceModel(ValueApiModel input)
        {
            JobServiceModel output = JsonConvert.DeserializeObject<JobServiceModel>(input.Data);
            output.JobId = input.objectid;
            return output;
        }
    }
}
