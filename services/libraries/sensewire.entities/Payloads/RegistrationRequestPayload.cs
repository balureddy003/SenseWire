using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.entities.Payloads
{
    public class RegistrationRequestPayload: IPayload
    {
        public string DeviceId { get; set; }

    }
}
