// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Extensions;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.External;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Helpers;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AuthenticationType = Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models.AuthenticationType;

namespace Microsoft.Azure.IoTSolutions.IotHubManager.Services
{
    public delegate Task<DevicePropertyServiceModel> DevicePropertyDelegate(DevicePropertyServiceModel model);
    public interface IDevices
    {
        Task<DeviceServiceListModel> GetListAsync(string query, string continuationToken);
        Task<DeviceTwinName> GetDeviceTwinNamesAsync();
        Task<DeviceServiceModel> GetAsync(string id);
        Task<DeviceServiceModel> CreateAsync(DeviceServiceModel toServiceModel);
        Task<DeviceServiceModel> CreateOrUpdateAsync(DeviceServiceModel toServiceModel, DevicePropertyDelegate devicePropertyDelegate);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, bool IsConnected);
        Task<TwinServiceModel> GetModuleTwinAsync(string deviceId, string moduleId);
        Task<TwinServiceListModel> GetModuleTwinsByQueryAsync(string query, string continuationToken);
        Task<StatusResultServiceModel> PingRegistryAsync();
    }

    public class Devices : IDevices
    {
        private const int MAX_GET_LIST = 1000;
        private const string QUERY_PREFIX = "SELECT * FROM devices";
        private const string MODULE_QUERY_PREFIX = "SELECT * FROM devices.modules";
        private const string DEVICES_CONNECTED_QUERY = "connectionState = 'Connected'";

        private RegistryManager registry;
        internal const string DEVICE_COLLECTION_ID = "devices";
        internal const string DEVICE_TWIN_COLLECTION_ID = "devicetwin";
        private readonly IStorageAdapterClient client;
        private string ioTHubHostName;

        public Devices(
            IServicesConfig config, IStorageAdapterClient client)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            this.client = client;
            IoTHubConnectionHelper.CreateUsingHubConnectionString(config.IoTHubConnString, (conn) =>
            {
                this.registry = RegistryManager.CreateFromConnectionString(conn);
                this.ioTHubHostName = IotHubConnectionStringBuilder.Create(conn).HostName;
            });
        }

        public Devices(RegistryManager registry, string ioTHubHostName, IStorageAdapterClient client)
        {
            this.registry = registry;
            this.ioTHubHostName = ioTHubHostName;
            this.client = client;
        }

        // Ping the registry to see if the connection is healthy
        public async Task<StatusResultServiceModel> PingRegistryAsync()
        {
            var result = new StatusResultServiceModel(false, "");
            try
            {
                await this.registry.GetDeviceAsync("healthcheck");
                result.IsHealthy = true;
                result.Message = "Alive and Well!";
            }
            catch (Exception e)
            {
                result.Message = e.Message;
            }

            return result;
        }
        private static bool DictionaryEqual(Dictionary<string, JToken> oldDict,Dictionary<string, JToken> newDict)
        {
            // Simple check, are the counts the same?
            if (!oldDict.Count.Equals(newDict.Count)) return false;

            // Verify the keys
            if (!oldDict.Keys.SequenceEqual(newDict.Keys)) return false;

            // Verify the values for each key
            foreach (string key in oldDict.Keys)
                if (!oldDict[key].SequenceEqual(newDict[key]))
                    return false;

            return true;
        }
        /// <summary>
        /// Query devices
        /// </summary>
        /// <param name="query">
        /// Two types of query supported:
        /// 1. Serialized Clause list in JSON. Each clause includes three parts: key, operator and value
        /// 2. The "Where" clause of official IoTHub query string, except keyword "WHERE"
        /// </param>
        /// <param name="continuationToken">Continuation token. Not in use yet</param>
        /// <returns>List of devices</returns>
        public async Task<DeviceServiceListModel> GetListAsync(string query, string continuationToken)
        {
            var response = await this.client.GetAllAsync(DEVICE_COLLECTION_ID);

            var requireddevicedata = response.Items.Select(CreateDeviceServiceModel);

            //// normally we need deviceTwins for all devices to show device list
            //var devices = await this.registry.GetDevicesAsync(MAX_GET_LIST);

            //var twins = await this.GetTwinByQueryAsync(QUERY_PREFIX,
            //                                           query,
            //                                           continuationToken,
            //                                           MAX_GET_LIST);
            //var twinsMap = twins.Result.ToDictionary(twin => twin.DeviceId, twin => twin);

            //var devicesList = devices.Where(dvc => twinsMap.ContainsKey(dvc.Id)).ToList();



            var twins = await this.client.GetAllAsync(DEVICE_TWIN_COLLECTION_ID);

            var requiredtwindata = twins.Items.Select(CreateTwinServiceModel);
           List<TwinServiceModel> filteredTwins = new List<TwinServiceModel>();
             if ((!string.IsNullOrWhiteSpace(query))&& (query!="[]"))
            {
                // Try to translate clauses to query
             
                query = QueryConditionTranslator.ToQueryString(query);
                var dictionary = query.Split(';').Select(part => part.Split('=')).Where(part => part.Length == 2).ToDictionary(sp => sp[0].Trim(), sp => JToken.Parse(sp[1].ToString().Trim()));
                foreach (var newitem in requiredtwindata)
                {
                 
                    if (newitem.Tags != null)
                    {
                        var sum = dictionary.Keys.Intersect(newitem.Tags.Keys).ToDictionary(t => t, t => dictionary[t]);
                        if (sum.Count==dictionary.Count){
                            filteredTwins.Add(newitem);
                        }
                    }

                }
            }
            else
            {
                filteredTwins = requiredtwindata.ToList();
            }
           
            var twinsMap = filteredTwins.ToDictionary(twin => twin.DeviceId, twin => twin);

            var devicesList = requireddevicedata.Where(dvc => twinsMap.ContainsKey(dvc.Id)).ToList();

            // return new DeviceServiceListModel(devicesList);

            //var connectedEdgeDevices = this.GetConnectedEdgeDevices(devicesList, twinsMap).Result;

            //// since deviceAsync does not support continuationToken for now, we need to ignore those devices which does not shown in twins
            return new DeviceServiceListModel(devicesList
                    .Select(azureDevice => new DeviceServiceModel(azureDevice,
                                                                  twinsMap[azureDevice.Id],
                                                                  this.ioTHubHostName,
                                                                 false, "text")),
                                              null);
        }

        /// <summary>
        /// Query devices
        /// </summary>
        /// <returns>DeviceTwinName</returns>
        public async Task<DeviceTwinName> GetDeviceTwinNamesAsync()
        {
            var content = await this.GetListAsync(string.Empty, string.Empty);

            return content.GetDeviceTwinNames();
        }

        public async Task<DeviceServiceModel> GetAsync(string id)
        {
            var device = this.registry.GetDeviceAsync(id);
            var twin = this.registry.GetTwinAsync(id);

            await Task.WhenAll(device, twin);

            if (device.Result == null)
            {
                throw new ResourceNotFoundException("The device doesn't exist.");
            }

            var isEdgeConnectedDevice = await this.DoesDeviceHaveConnectedModules(device.Result.Id);

            return new DeviceServiceModel(device.Result, twin.Result, this.ioTHubHostName, isEdgeConnectedDevice);
        }

        public async Task<DeviceServiceModel> CreateAsync(DeviceServiceModel device)
        {
            try
            {

                if (device.IsEdgeDevice &&
                    device.Authentication != null &&
                    !device.Authentication.AuthenticationType.Equals(AuthenticationType.Sas))
                {
                    throw new InvalidInputException("Edge devices only support symmetric key authentication.");
                }

                // auto generate DeviceId, if missing
                if (string.IsNullOrEmpty(device.Id))
                {
                    device.Id = Guid.NewGuid().ToString();
                }

                var value = JsonConvert.SerializeObject(device, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var CreateDeviceResponse = await this.client.CreateAsync(DEVICE_COLLECTION_ID, value);

                var iotDevice = this.CreateDeviceServiceModel(CreateDeviceResponse);
                JObject jsonObject = new JObject
             {
                { "DeviceId",iotDevice.Id },
                { "ModuleId",iotDevice.Id },
                  { "Tags", new JObject
                {
                      { "Location", "chennai" },
                      { "Floor",2 },
                       { "Campus", "office" },
                }
             },
                { "ReportedProperties", new JObject
                {
                     { "SupportedMethods","Reboot,FirmwareUpdate,EmergencyValveRelease,IncreasePressure"},
                        { "telemetry", new JObject
                {
                     
                      { "MessageTemplate","{\"floor\":${floor},\"vibration\":${vibration},\"vibration_unit\":\"${vibration_unit}\",\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\"}"},
                       {"MessageSchema", new JObject
                {
                      { "Name", "JOBS" },
                      { "Format",DateTime.UtcNow },
                       {"Fields", new JObject
                {
                      { "floor", "integer" },
                      { "vibration","integer" },
                       { "vibration_unit","integer" },
                }},
                } },
                }
                },
                      { "type", "JOBS" },
                    { "firmware", new JObject
                {
                      { "currentFwVersion", "JOBS" },
                      { "lastFwUpdateStartTime",DateTime.UtcNow },
                       { "lastFwUpdateEndTime", DateTime.UtcNow },
                }
             }

                }
             },
                     { "DesiredProperties", new JObject
                {
                        { "telemetry", "JOBS" },
                      { "type", "JOBS" },
                    { "firmware", new JObject
                {
                      { "currentFwVersion", "JOBS" },
                      { "lastFwUpdateStartTime",DateTime.UtcNow },
                       { "lastFwUpdateEndTime", DateTime.UtcNow },
                }
             }

                }
             }
            };


                var valuetwin = JsonConvert.SerializeObject(jsonObject, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var deviceTwin = await this.client.CreateAsync(DEVICE_TWIN_COLLECTION_ID, valuetwin);
                var TwinDevice = this.CreateDeviceTwinServiceModel(deviceTwin, iotDevice);



                return TwinDevice;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// We only support update twin
        /// </summary>
        /// <param name="device"></param>
        /// <param name="devicePropertyDelegate"></param>
        /// <returns></returns>
        public async Task<DeviceServiceModel> CreateOrUpdateAsync(DeviceServiceModel device, DevicePropertyDelegate devicePropertyDelegate)
        {
            // validate device module
            var azureDevice = await this.registry.GetDeviceAsync(device.Id);
            if (azureDevice == null)
            {
                azureDevice = await this.registry.AddDeviceAsync(device.ToAzureModel());
            }

            Twin azureTwin;
            if (device.Twin == null)
            {
                azureTwin = await this.registry.GetTwinAsync(device.Id);
            }
            else
            {
                azureTwin = await this.registry.UpdateTwinAsync(device.Id, device.Twin.ToAzureModel(), device.Twin.ETag);

                // Update the deviceGroupFilter cache, no need to wait
                var model = new DevicePropertyServiceModel();

                var tagRoot = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(device.Twin.Tags)) as JToken;
                if (tagRoot != null)
                {
                    model.Tags = new HashSet<string>(tagRoot.GetAllLeavesPath());
                }

                var reportedRoot = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(device.Twin.ReportedProperties)) as JToken;
                if (reportedRoot != null)
                {
                    model.Reported = new HashSet<string>(reportedRoot.GetAllLeavesPath());
                }
                var unused = devicePropertyDelegate(model);
            }

            return new DeviceServiceModel(azureDevice, azureTwin, this.ioTHubHostName);
        }

        public async Task DeleteAsync(string id)
        {
            await this.client.DeleteAsync(DEVICE_COLLECTION_ID, id);
        }

        public async Task<TwinServiceModel> GetModuleTwinAsync(string deviceId, string moduleId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidInputException("A valid deviceId must be provided.");
            }

            if (string.IsNullOrWhiteSpace(moduleId))
            {
                throw new InvalidInputException("A valid moduleId must be provided.");
            }

            var twin = await this.registry.GetTwinAsync(deviceId, moduleId);
            return new TwinServiceModel(twin);
        }

        public async Task<TwinServiceListModel> GetModuleTwinsByQueryAsync(string query,
                                                                           string continuationToken)
        {
            //var twins = await this.GetTwinByQueryAsync(MODULE_QUERY_PREFIX,
            //                                           query,
            //                                           continuationToken,
            //                                           MAX_GET_LIST);
            var twins = await this.client.GetAllAsync(DEVICE_TWIN_COLLECTION_ID);

            var requiredtwindata = twins.Items.Select(CreateTwinServiceModel);

            return new TwinServiceListModel(requiredtwindata, null);
        }

        /// <summary>
        /// Get twin result by query
        /// </summary>
        /// <param name="queryPrefix">The query prefix which selects devices or device modules</param>
        /// <param name="query">The query without prefix</param>
        /// <param name="continuationToken">The continuationToken</param>
        /// <param name="numberOfResult">The max result</param>
        /// <returns></returns>
        private async Task<ResultWithContinuationToken<List<Twin>>> GetTwinByQueryAsync(string queryPrefix,
            string query, string continuationToken, int numberOfResult)
        {
            query = string.IsNullOrEmpty(query) ? queryPrefix : $"{queryPrefix} where {query}";

            var twins = new List<Twin>();

            var twinQuery = this.registry.CreateQuery(query);

            QueryOptions options = new QueryOptions();
            options.ContinuationToken = continuationToken;

            while (twinQuery.HasMoreResults && twins.Count < numberOfResult)
            {
                var response = await twinQuery.GetNextAsTwinAsync(options);
                options.ContinuationToken = response.ContinuationToken;
                twins.AddRange(response);
            }

            return new ResultWithContinuationToken<List<Twin>>(twins, options.ContinuationToken);
        }

        /// <summary>
        /// Retrieves the list of edge devices which are reporting as connected based on
        /// connectivity of their modules. If any of the modules are connected than the edge device
        /// should report as connected.
        /// </summary>
        /// <param name="devicesList">The list of devices to check</param>
        /// <param name="twinsMap">Map of associated twins for those devices</param>
        /// <returns>Dictionary of edge device ids and the device</returns>
        private async Task<Dictionary<string, Device>> GetConnectedEdgeDevices(List<Device> devicesList,
            Dictionary<string, Twin> twinsMap)
        {
            if (!devicesList.Any(dvc => dvc.Capabilities?.IotEdge ??
                                        twinsMap.Values.Any(twin => twin.Capabilities?.IotEdge ?? false)))
            {
                return new Dictionary<string, Device>();
            }

            var devicesWithConnectedModules = await this.GetDevicesWithConnectedModules();
            var edgeDevices = devicesList
                .Where(device => device.Capabilities?.IotEdge ??
                           twinsMap[device.Id].Capabilities?.IotEdge ?? false)
                .Where(edgeDvc => devicesWithConnectedModules.Contains(edgeDvc.Id))
                .ToDictionary(edgeDevice => edgeDevice.Id, edgeDevice => edgeDevice);
            return edgeDevices;
        }

        /// <summary>
        /// Retrieves the set of devices that have at least one module connected.
        /// </summary>
        /// <returns>Set of devices which are listed as connected</returns>
        private async Task<HashSet<string>> GetDevicesWithConnectedModules()
        {
            var connectedEdgeDevices = new HashSet<string>();

            var edgeModules = await this.GetModuleTwinsByQueryAsync(DEVICES_CONNECTED_QUERY, "");
            foreach (var model in edgeModules.Items)
            {
                connectedEdgeDevices.Add(model.DeviceId);
            }

            return connectedEdgeDevices;
        }
        public async Task UpdateStatusAsync(string id, bool IsConnected)
        {
            var device = await this.client.GetAsync(DEVICE_COLLECTION_ID, id);
            if (device != null)
            {
                var devicemodel = this.CreateDeviceServiceModel(device);
                devicemodel.Connected = IsConnected;
                devicemodel.LastActivity = DateTime.Now;
                var value = JsonConvert.SerializeObject(devicemodel, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var updateddevice = await this.client.UpdateAsync(DEVICE_COLLECTION_ID, id, value, null);
            }
          
        }
        /// <summary>
        /// Checks if a single device has connected modules
        /// </summary>
        /// <param name="deviceId">Device Id to query</param>
        /// <returns>True if one of the modules for this device is connected.</returns>
        private async Task<bool> DoesDeviceHaveConnectedModules(string deviceId)
        {
            var query = $"deviceId='{deviceId}' AND {DEVICES_CONNECTED_QUERY}";
            var edgeModules = await this.GetModuleTwinsByQueryAsync(query, "");
            return edgeModules.Items.Any();
        }

        private class ResultWithContinuationToken<T>
        {
            public T Result { get; private set; }

            public string ContinuationToken { get; private set; }

            public ResultWithContinuationToken(T queryResult, string continuationToken)
            {
                this.Result = queryResult;
                this.ContinuationToken = continuationToken;
            }
        }
        private DeviceServiceModel CreateDeviceServiceModel(ValueApiModel input)
        {
            DeviceServiceModel output = JsonConvert.DeserializeObject<DeviceServiceModel>(input.Data);
            output.Id = input.objectid;
            output.Etag = input.ETag;
            output.IoTHubHostName = "iCuboid";
            return output;
        }
        private DeviceServiceModel CreateDeviceTwinServiceModel(ValueApiModel input, DeviceServiceModel data)
        {
            TwinServiceModel output = JsonConvert.DeserializeObject<TwinServiceModel>(input.Data);
            data.Twin = output;
            return data;
        }
        private DeviceServiceModel CreateDeviceTwinListServiceModel(TwinServiceModel input, DeviceServiceModel data)
        {

            data.Twin = input;
            return data;
        }
        private TwinServiceModel CreateTwinServiceModel(ValueApiModel input)
        {
            TwinServiceModel output = JsonConvert.DeserializeObject<TwinServiceModel>(input.Data);
            return output;
        }
    }
}
