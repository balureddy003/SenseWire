using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.entities.Payloads
{
    public class DeviceDetailsPayload : IPayload
    {
        public List<DeviceDetails> Devices { get; set; }
    }
    public class DeviceDetails
    {
        public string DeviceId { get; set; }
        public bool IsOnline { get; set; }

        public JObject ReportedProperties { get; set; }
        public JObject DesiredProperties { get; set; }
    }
}
