using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.entities.Payloads
{
    public class QueryFilterPayload :IPayload
    {
        public List<string> DeviceIdList { get; set; }

        public TimeSpan QueryTimeout { get; set; }
    }
}
