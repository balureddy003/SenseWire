// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.Devices;

namespace Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models
{
    public class DeviceJobServiceModel
    {
        public string DeviceId { get; set; }
        public DeviceJobStatus Status { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime LastUpdatedDateTimeUtc { get; set; }
        public MethodResultServiceModel Outcome { get; set; }
        public DeviceJobErrorServiceModel Error { get; set; }

        public DeviceJobServiceModel(DeviceJob deviceJob)
        {
            this.DeviceId = deviceJob.DeviceId;

            switch (deviceJob.Status)
            {
                case Azure.Devices.DeviceJobStatus.Pending:
                    this.Status = DeviceJobStatus.Pending;
                    break;

                case Azure.Devices.DeviceJobStatus.Scheduled:
                    this.Status = DeviceJobStatus.Scheduled;
                    break;

                case Azure.Devices.DeviceJobStatus.Running:
                    this.Status = DeviceJobStatus.Running;
                    break;

                case Azure.Devices.DeviceJobStatus.Completed:
                    this.Status = DeviceJobStatus.Completed;
                    break;

                case Azure.Devices.DeviceJobStatus.Failed:
                    this.Status = DeviceJobStatus.Failed;
                    break;

                case Azure.Devices.DeviceJobStatus.Canceled:
                    this.Status = DeviceJobStatus.Canceled;
                    break;
            }

            this.StartTimeUtc = deviceJob.StartTimeUtc;
            this.EndTimeUtc = deviceJob.EndTimeUtc;
            this.CreatedDateTimeUtc = deviceJob.CreatedDateTimeUtc;
            this.LastUpdatedDateTimeUtc = deviceJob.LastUpdatedDateTimeUtc;

            if (deviceJob.Outcome?.DeviceMethodResponse != null)
            {
                this.Outcome = new MethodResultServiceModel(deviceJob.Outcome.DeviceMethodResponse);
            }

            if (deviceJob.Error != null)
            {
                this.Error = new DeviceJobErrorServiceModel(deviceJob.Error);
            }
        }

        public DeviceJobServiceModel()
        {
        }
    }

    /// <summary>
    /// refer to Microsoft.Azure.Devices.DeviceJobStatus
    /// </summary>
    public enum DeviceJobStatus
    {
        Pending = 0,
        Scheduled = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Canceled = 5
    }
}
