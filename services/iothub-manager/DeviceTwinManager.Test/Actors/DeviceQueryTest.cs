using Akka.Actor;
using Akka.TestKit.Xunit2;
using DeviceTwinManager.Actors;
using sensewire.entities;
using sensewire.entities.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeviceTwinManager.Test.Actors
{
    public class DeviceQueryTest : TestKit
    {
        [Fact]
        public void ReturnDeviceDetails()
        {
            var queryRequestor = CreateTestProbe();
            var temp1 = CreateTestProbe();
            var temp2 = CreateTestProbe();

            var deviceQuery = Sys.ActorOf(DeviceQuery.Props(
                actorRefToDeviceIdMap: new Dictionary<IActorRef, string>
                {
                    [temp1.Ref] = "123",
                    [temp2.Ref] = "456"
                },
                sender: queryRequestor.Ref,
                correlationId: 1,
                queryTimeout: TimeSpan.FromSeconds(3)
            ));

            temp1.ExpectMsg<SystemEvent>((m, sender) =>
            {
                Assert.Equal(1, m.CorrelationId);
                Assert.Equal(deviceQuery, sender);
            });

            temp2.ExpectMsg<SystemEvent>((m, sender) =>
            {
                Assert.Equal(1, m.CorrelationId);
                Assert.Equal(deviceQuery, sender);
            });

            deviceQuery.Tell(new SystemEvent(
                SystemEventTypesEnum.RespondDeviceDetails,
                1,
                new DeviceDetailsPayload
                {
                    Devices = new List<DeviceDetails>
                    {
                        new DeviceDetails
                        {
                            DeviceId = "123",
                            IsOnline = false
                        }
                    }
                }),
                temp1.Ref);
            deviceQuery.Tell(new SystemEvent(
                SystemEventTypesEnum.RespondDeviceDetails,
                1,
                new DeviceDetailsPayload
                {
                    Devices = new List<DeviceDetails>
                    {
                                    new DeviceDetails
                                    {
                                        DeviceId = "456",
                                        IsOnline = true
                                    }
                    }
                }),
                temp2.Ref);

            var response = queryRequestor.ExpectMsg<SystemEvent>();
            var data = response.Payload as DeviceDetailsPayload;
            Assert.Equal(1, response.CorrelationId);
            Assert.Equal(2, data.Devices.Count);

            var temp1Reading = Assert.IsAssignableFrom<bool>(data.Devices.Where(x => x.DeviceId.Equals("123")).FirstOrDefault().IsOnline);
            Assert.False(temp1Reading);
            var temp2Reading = Assert.IsAssignableFrom<bool>(data.Devices.Where(x => x.DeviceId.Equals("456")).FirstOrDefault().IsOnline);
            Assert.True(temp2Reading);

        }

        [Fact]
        public void TimeOutQuery()
        {
            var queryRequestor = CreateTestProbe();
            var temp1 = CreateTestProbe();
            var temp2 = CreateTestProbe();

            var deviceQuery = Sys.ActorOf(DeviceQuery.Props(
            actorRefToDeviceIdMap: new Dictionary<IActorRef, string>
            {
                [temp1.Ref] = "123",
                [temp2.Ref] = "456"
            },
            correlationId: 1,
            sender: queryRequestor.Ref,
            queryTimeout: TimeSpan.FromSeconds(3)
            ));

            temp1.ExpectMsg<SystemEvent>((m, sender) =>
            {
                Assert.Equal(1, m.CorrelationId);
                Assert.Equal(deviceQuery, sender);
            });

            temp2.ExpectMsg<SystemEvent>((m, sender) =>
            {
                Assert.Equal(1, m.CorrelationId);
                Assert.Equal(deviceQuery, sender);
            });

            deviceQuery.Tell(new SystemEvent(
                SystemEventTypesEnum.RespondDeviceDetails,
                1,
                new DeviceDetailsPayload
                {
                    Devices = new List<DeviceDetails>
                    {
                        new DeviceDetails
                        {
                            DeviceId = "123",
                            IsOnline = true
                        }
                    }
                }),
                temp1.Ref
            );
            var response = queryRequestor.ExpectMsg<SystemEvent>(TimeSpan.FromSeconds(5));
            var data = response.Payload as DeviceDetailsPayload;
            Assert.Equal(1, response.CorrelationId);
            Assert.Single(data.Devices);

            var temp1Reading = Assert.IsAssignableFrom<bool>(data.Devices.Where(x => x.DeviceId.Equals("123")).FirstOrDefault().IsOnline);
            Assert.True(temp1Reading);
            var temp2Reading = Assert.IsAssignableFrom<int>(data.Devices.Where(x => x.DeviceId.Equals("456")).Count());
            Assert.Equal(0, temp2Reading);

        }

        [Fact]
        public void FindSensorsStoppedDuringQuery()
        {
            var queryRequestor = CreateTestProbe();
            var temp1 = CreateTestProbe();
            var temp2 = CreateTestProbe();

            var deviceQuery = Sys.ActorOf(DeviceQuery.Props(
            actorRefToDeviceIdMap: new Dictionary<IActorRef, string>
            {
                [temp1.Ref] = "123",
                [temp2.Ref] = "456"
            },
            correlationId: 1,
            sender: queryRequestor.Ref,
            queryTimeout: TimeSpan.FromSeconds(3)
            ));

            temp1.ExpectMsg<SystemEvent>((m, sender) =>
            {
                Assert.Equal(1, m.CorrelationId);
                Assert.Equal(deviceQuery, sender);
            });

            temp2.ExpectMsg<SystemEvent>((m, sender) =>
            {
                Assert.Equal(1, m.CorrelationId);
                Assert.Equal(deviceQuery, sender);
            });

            deviceQuery.Tell(new SystemEvent(
                SystemEventTypesEnum.RespondDeviceDetails,
                1,
                new DeviceDetailsPayload
                {
                    Devices = new List<DeviceDetails>
                    {
                        new DeviceDetails
                        {
                            DeviceId = "123",
                            IsOnline = true
                        }
                    }
                }),
                temp1.Ref
            );
            temp2.Tell(PoisonPill.Instance);

            var response = queryRequestor.ExpectMsg<SystemEvent>();
            var data = response.Payload as DeviceDetailsPayload;

            Assert.Equal(1, response.CorrelationId);
            Assert.Single(data.Devices);

            var temp1Reading = Assert.IsAssignableFrom<bool>(data.Devices.Where(x => x.DeviceId.Equals("123")).FirstOrDefault().IsOnline);
            Assert.True(temp1Reading);
            var temp2Reading = Assert.IsAssignableFrom<int>(data.Devices.Where(x => x.DeviceId.Equals("456")).Count());
            Assert.Equal(0, temp2Reading);
        }

    }
}
