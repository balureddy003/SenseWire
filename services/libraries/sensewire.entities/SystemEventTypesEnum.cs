using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.entities
{
    public enum SystemEventTypesEnum
    {
        RequestDeviceRegistration = 0,
        RespondDeviceRegistration = 1,
        DeviceOnline = 2,
        DeviceOffline = 3,
        RequestAllDeviceIds = 4,
        RespondAllDeviceIds = 5,
        RequestDeviceDetails = 6,
        RespondDeviceDetails = 7,
        QueryDevicesDetails = 8,
        QueryTimeout = 9
    }
}
