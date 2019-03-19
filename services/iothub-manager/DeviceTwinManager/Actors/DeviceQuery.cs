using Akka.Actor;
using sensewire.entities;
using sensewire.entities.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceTwinManager.Actors
{
    public class DeviceQuery : UntypedActor
    {
        private Dictionary<IActorRef, string> actorRefToDeviceIdMap;
        private IActorRef requestor;
        private TimeSpan queryTimeout;
        private long? correlationId;

        private ICancelable queryTimeoutTimer;
        private List<DeviceDetails> repliesReceived = new List<DeviceDetails>();
        private HashSet<IActorRef> waitingReply;
        public DeviceQuery(Dictionary<IActorRef, string> actorRefToDeviceIdMap, IActorRef sender, TimeSpan queryTimeout, long? correlationId)
        {
            this.actorRefToDeviceIdMap = actorRefToDeviceIdMap;
            this.requestor = sender;
            this.queryTimeout = queryTimeout;
            this.correlationId = correlationId;

            waitingReply = new HashSet<IActorRef>(actorRefToDeviceIdMap.Keys);
            queryTimeoutTimer = Context.System.Scheduler.ScheduleTellOnceCancelable(queryTimeout, Self, new SystemEvent(SystemEventTypesEnum.QueryTimeout, null), Self);
        }
        protected override void PreStart()
        {
            foreach (var sensor in actorRefToDeviceIdMap)
            {
                Context.Watch(sensor.Key);
                sensor.Key.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceDetails, correlationId, null, sensor.Value));
            }
        }

        protected override void PostStop()
        {
            queryTimeoutTimer.Cancel();
        }

        protected override void OnReceive(object message)
        {
            if (message is SystemEvent)
            {
                var systemEvent = message as SystemEvent;
                switch (systemEvent.EventType)
                {
                    case SystemEventTypesEnum.RespondDeviceDetails when systemEvent.CorrelationId == correlationId:
                        var payload = systemEvent.Payload as DeviceDetailsPayload;
                        RecordDeviceDetails(Sender, payload.Devices.FirstOrDefault());
                        break;

                    case SystemEventTypesEnum.QueryTimeout:
                        foreach (var sensor in waitingReply)
                        {
                            var deviceId = actorRefToDeviceIdMap[sensor];
                            //repliesReceived.Add(new DeviceDetails { DeviceId = deviceId });
                        }
                        requestor.Tell(new SystemEvent(SystemEventTypesEnum.RespondDeviceDetails, correlationId, new DeviceDetailsPayload { Devices = repliesReceived }));
                        Context.Stop(Self);
                        break;

                    default:
                        Unhandled(message);
                        break;
                }
            }
            else
            {
                switch (message)
                {
                    case Terminated m:
                        RecordDeviceDetails(m.ActorRef, null);
                        break;
                    default:
                        Unhandled(message);
                        break;
                }
            }
        }

        private void RecordDeviceDetails(IActorRef sender, DeviceDetails details)
        {
            Context.Unwatch(sender);
            var deviceId = actorRefToDeviceIdMap[sender];
            waitingReply.Remove(sender);
            if (details != null)
            {
                //details = new DeviceDetails { DeviceId = deviceId };
                repliesReceived.Add(details);
            }

            if (waitingReply.Count == 0)
            {
                requestor.Tell(new SystemEvent(SystemEventTypesEnum.RespondDeviceDetails, correlationId, new DeviceDetailsPayload { Devices = repliesReceived }));
                Context.Stop(Self);
            }
        }

        public static Props Props(Dictionary<IActorRef, string> actorRefToDeviceIdMap, IActorRef sender, TimeSpan queryTimeout, long? correlationId) =>
            Akka.Actor.Props.Create(() => new DeviceQuery(actorRefToDeviceIdMap, sender, queryTimeout, correlationId));
    }
}
