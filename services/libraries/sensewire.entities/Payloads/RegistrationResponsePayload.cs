using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.entities.Payloads
{
    public class RegistrationResponsePayload:IPayload
    {
        public object DeviceReference { get; set; }
    }
}
