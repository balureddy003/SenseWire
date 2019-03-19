using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.entities.Payloads
{
    public class DeviceIdListPayload:IPayload
    {
        public List<string> DeviceIdList { get; set; }
    }
}
