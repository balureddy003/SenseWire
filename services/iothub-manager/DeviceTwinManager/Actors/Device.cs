using Akka.Actor;
using Newtonsoft.Json.Linq;
using sensewire.entities;
using sensewire.entities.Payloads;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceTwinManager.Actors
{
    public class Device : UntypedActor
    {

        public Device(string deviceId)
        {
            _deviceId = deviceId;
        }

        private string _deviceId { get; }

        private bool _isOnline { get; set; }
        private JObject _reportedProperties { get; }
        private JObject _desiredProperties { get; }

        protected override void OnReceive(object message)
        {
            var systemEvent = message as SystemEvent;
            if (systemEvent != null)
            {
                switch (systemEvent.EventType)
                {
                    case SystemEventTypesEnum.RequestDeviceRegistration:
                        var payload = systemEvent.Payload as RegistrationRequestPayload;
                        if (payload.DeviceId.Equals(_deviceId, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Sender.Tell(new SystemEvent
                            (
                                SystemEventTypesEnum.RespondDeviceRegistration,
                                systemEvent.CorrelationId,
                                new RegistrationResponsePayload { DeviceReference = Context.Self }
                            ));

                        }
                        else
                        {
                            Unhandled(systemEvent);
                        }
                        break;

                    case SystemEventTypesEnum.RequestDeviceDetails when systemEvent.EntityId.Equals(_deviceId):
                        Sender.Tell(new SystemEvent(
                            SystemEventTypesEnum.RespondDeviceDetails,
                            systemEvent.CorrelationId,
                            new DeviceDetailsPayload
                            {
                                Devices = new List<DeviceDetails>
                                {
                                    new DeviceDetails
                                    {
                                        DeviceId = _deviceId,
                                        IsOnline = _isOnline,
                                        DesiredProperties = _desiredProperties,
                                        ReportedProperties = _reportedProperties
                                    }
                                }
                            }
                            ));
                        break;

                    case SystemEventTypesEnum.DeviceOnline when systemEvent.EntityId.Equals(_deviceId):
                        _isOnline = true;
                        break;

                    case SystemEventTypesEnum.DeviceOffline when systemEvent.EntityId.Equals(_deviceId):
                        _isOnline = false;
                        break;

                    default:
                        Unhandled(systemEvent);
                        break;
                }
            }
        }

        public static Props Props(string deviceId) =>
            Akka.Actor.Props.Create(() => new Device(deviceId));
    }
}
