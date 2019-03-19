using Akka.Actor;
using sensewire.entities;
using sensewire.entities.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceTwinManager.Actors
{
    public class DeviceManager : UntypedActor
    {
        private Dictionary<string, IActorRef> _deviceMap = new Dictionary<string, IActorRef>();

        protected override void OnReceive(object message)
        {
            var systemEvent = message as SystemEvent;
            if (systemEvent != null)
            {
                switch (systemEvent.EventType)
                {
                    case SystemEventTypesEnum.RequestAllDeviceIds:
                        Sender.Tell(new SystemEvent
                            (
                                SystemEventTypesEnum.RespondAllDeviceIds,
                                systemEvent.CorrelationId,
                                new DeviceIdListPayload
                                {
                                    DeviceIdList = _deviceMap.Keys.ToList()
                                }
                            ));
                        break;

                    case SystemEventTypesEnum.RequestDeviceRegistration:
                        var payload = systemEvent.Payload as RegistrationRequestPayload;
                        if (_deviceMap.TryGetValue(payload.DeviceId, out var existingDeviceActor))
                        {
                            existingDeviceActor.Forward(systemEvent);
                        }
                        else
                        {
                            var newDeviceActor = Context.ActorOf(Device.Props(payload.DeviceId), $"device-{payload.DeviceId}");
                            Context.Watch(newDeviceActor);
                            _deviceMap.Add(payload.DeviceId, newDeviceActor);
                            newDeviceActor.Forward(systemEvent);
                        }
                        break;

                    case SystemEventTypesEnum.DeviceOnline:
                    case SystemEventTypesEnum.DeviceOffline:
                    case SystemEventTypesEnum.RequestDeviceDetails:
                        if (_deviceMap.TryGetValue(systemEvent.EntityId, out existingDeviceActor))
                        {
                            existingDeviceActor.Forward(systemEvent);
                        }
                        else
                        {

                        }
                        break;

                    case SystemEventTypesEnum.QueryDevicesDetails:
                        var actorRefToDeviceIdMap = new Dictionary<IActorRef, string>();
                        var filterPayload = systemEvent.Payload as QueryFilterPayload;
                        foreach (var deviceId in filterPayload.DeviceIdList)
                        {
                            if (_deviceMap.TryGetValue(deviceId, out var deviceActorRef))
                            {
                                actorRefToDeviceIdMap.Add(deviceActorRef, deviceId);
                            }
                        }
                        Context.ActorOf(DeviceQuery.Props(actorRefToDeviceIdMap, Sender, filterPayload.QueryTimeout, systemEvent.CorrelationId));
                        break;

                    default:
                        Unhandled(systemEvent);
                        break;
                }
            }
            else if (message is Terminated)
            {
                var m = message as Terminated;
                var terminatedId = _deviceMap.First(x => x.Value.Equals(m.ActorRef)).Key;
                _deviceMap.Remove(terminatedId);
            }
            else
            {
                Unhandled(message);
            }
        }

        public static Props Props() => Akka.Actor.Props.Create<DeviceManager>();
    }
}
