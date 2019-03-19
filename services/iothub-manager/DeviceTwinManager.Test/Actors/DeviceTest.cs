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
    public class DeviceTest : TestKit
    {
        [Fact]
        public void InitializeDeviceMetaData()
        {
            var probe = CreateTestProbe();
            var device = Sys.ActorOf(Device.Props("123"));

            device.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceDetails, 1, null, "123"), probe.Ref);

            var received = probe.ExpectMsg<SystemEvent>();
            var payload = received.Payload as DeviceDetailsPayload;
            Assert.Equal(1, received.CorrelationId);
            Assert.Equal("123", payload.Devices.FirstOrDefault().DeviceId);
        }

        [Fact]
        public void RegisterDevice()
        {
            var probe = CreateTestProbe();
            var device = Sys.ActorOf(Device.Props("123"));

            device.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            var received = probe.ExpectMsg<SystemEvent>();
            Assert.Equal(1, received.CorrelationId);
            var payload = received.Payload as RegistrationResponsePayload;
            Assert.Equal(device, payload.DeviceReference as IActorRef);
        }


        [Fact]
        public void NotRegisterDeviceWhenDeviceMismatch()
        {
            var probe = CreateTestProbe();
            var eventStreamProbe = CreateTestProbe();
            Sys.EventStream.Subscribe(eventStreamProbe, typeof(Akka.Event.UnhandledMessage));
            var device = Sys.ActorOf(Device.Props("123"));
            device.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "1234" }), probe.Ref);
            probe.ExpectNoMsg();
            var unhandled = eventStreamProbe.ExpectMsg<Akka.Event.UnhandledMessage>();

            Assert.IsType<SystemEvent>(unhandled.Message);

        }

        [Fact]
        public void UpdateOnlineStatus()
        {
            var probe = CreateTestProbe();
            var device = Sys.ActorOf(Device.Props("123"));

            device.Tell(new SystemEvent(SystemEventTypesEnum.DeviceOnline, 1, null, "123"), probe.Ref);

            device.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceDetails, 2, null, "123"), probe.Ref);
            var received = probe.ExpectMsg<SystemEvent>();
            var payload = received.Payload as DeviceDetailsPayload;
            Assert.Equal(2, received.CorrelationId);
            Assert.Equal("123", payload.Devices.FirstOrDefault().DeviceId);
            Assert.True(payload.Devices.FirstOrDefault().IsOnline);

        }
    }
}
