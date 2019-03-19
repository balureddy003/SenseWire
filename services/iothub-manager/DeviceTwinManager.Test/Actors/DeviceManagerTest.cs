using Akka.Actor;
using Akka.TestKit.Xunit2;
using DeviceTwinManager.Actors;
using sensewire.entities;
using sensewire.entities.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeviceTwinManager.Test.Actors
{
    public class DeviceManagerTest : TestKit
    {
        [Fact]
        public void ReturnNoDeviceIdsWhenNewlyCreted()
        {
            var probe = CreateTestProbe();
            var manager = Sys.ActorOf(DeviceManager.Props());

            manager.Tell(new SystemEvent(SystemEventTypesEnum.RequestAllDeviceIds, 1), probe.Ref);
            var received = probe.ExpectMsg<SystemEvent>();
            var payload = received.Payload as DeviceIdListPayload;
            Assert.Equal(1, received.CorrelationId);
            Assert.Empty(payload.DeviceIdList);
        }

        [Fact]
        public void RegisterNewsDeviceIfNotPresent()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());
            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>(x => x.Payload is RegistrationResponsePayload);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestAllDeviceIds, 2), probe.Ref);
            var received = probe.ExpectMsg<SystemEvent>();
            var payload = received.Payload as DeviceIdListPayload;
            Assert.Equal(2, received.CorrelationId);
            Assert.Single(payload.DeviceIdList);
            Assert.Contains("123", payload.DeviceIdList);
        }

        [Fact]
        public void ReturnDeviceIfExitsWhenRegistered()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());
            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "1234" }), probe.Ref);
            var response = probe.ExpectMsg<SystemEvent>();
            Assert.Equal(1, response.CorrelationId);
            var firstDevice = probe.LastSender;

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 2, new RegistrationRequestPayload { DeviceId = "1234" }), probe.Ref);
            response = probe.ExpectMsg<SystemEvent>();
            Assert.Equal(2, response.CorrelationId);
            var secondSensor = probe.LastSender;

            Assert.Equal(firstDevice, secondSensor);
        }

        [Fact]
        public void ReturnAllDevices()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "1234" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>();

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 2, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>();

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestAllDeviceIds, 3), probe.Ref);
            var response = probe.ExpectMsg<SystemEvent>();
            var payload = response.Payload as DeviceIdListPayload;

            Assert.Equal(2, payload.DeviceIdList.Count);
            Assert.Contains("123", payload.DeviceIdList);
            Assert.Contains("1234", payload.DeviceIdList);
        }

        [Fact]
        public void ReturnEmptyListIfNoDevices()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestAllDeviceIds, 1), probe.Ref);
            var response = probe.ExpectMsg<SystemEvent>();
            var payload = response.Payload as DeviceIdListPayload;

            Assert.Empty(payload.DeviceIdList);
        }

        [Fact]
        public void ReturnActiveDevices()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "1234" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>();
            var firstDevice = probe.LastSender;

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 2, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>();

            probe.Watch(firstDevice);
            firstDevice.Tell(PoisonPill.Instance);
            probe.ExpectTerminated(firstDevice);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestAllDeviceIds, 3), probe.Ref);
            var response = probe.ExpectMsg<SystemEvent>();
            var payload = response.Payload as DeviceIdListPayload;

            Assert.Single(payload.DeviceIdList);
            Assert.Contains("123", payload.DeviceIdList);
        }

        [Fact]
        public void UpdateOnlineStatus()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>(x => x.Payload is RegistrationResponsePayload);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.DeviceOnline, 1, null, "123"), probe.Ref);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceDetails, 2, null, "123"), probe.Ref);
            var received = probe.ExpectMsg<SystemEvent>();
            var payload = received.Payload as DeviceDetailsPayload;
            Assert.Equal(2, received.CorrelationId);
            Assert.Equal("123", payload.Devices.FirstOrDefault().DeviceId);
            Assert.True(payload.Devices.FirstOrDefault().IsOnline);

        }

        [Fact]
        public void UpdateOfflineStatus()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>(x => x.Payload is RegistrationResponsePayload);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.DeviceOffline, 1, null, "123"), probe.Ref);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceDetails, 2, null, "123"), probe.Ref);
            var received = probe.ExpectMsg<SystemEvent>();
            var payload = received.Payload as DeviceDetailsPayload;
            Assert.Equal(2, received.CorrelationId);
            Assert.Equal("123", payload.Devices.FirstOrDefault().DeviceId);
            Assert.False(payload.Devices.FirstOrDefault().IsOnline);

        }

        [Fact]
        public void InitializeDeviceMetaData()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());
            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>(x => x.Payload is RegistrationResponsePayload);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceDetails, 1, null, "123"), probe.Ref);

            var received = probe.ExpectMsg<SystemEvent>();
            var payload = received.Payload as DeviceDetailsPayload;
            Assert.Equal(1, received.CorrelationId);
            Assert.Equal("123", payload.Devices.FirstOrDefault().DeviceId);
        }

        [Fact]
        public void ShouldInitiateQuery()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props());

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "123" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>(x => x.Payload is RegistrationResponsePayload);
            var device1 = probe.LastSender;

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 2, new RegistrationRequestPayload { DeviceId = "456" }), probe.Ref);
            probe.ExpectMsg<SystemEvent>(x => x.Payload is RegistrationResponsePayload);
            var device2 = probe.LastSender;

            device1.Tell(new SystemEvent(SystemEventTypesEnum.DeviceOnline, 3, null, "123"), probe.Ref);
            device2.Tell(new SystemEvent(SystemEventTypesEnum.DeviceOffline, 4, null, "456"), probe.Ref);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.QueryDevicesDetails, 5, new QueryFilterPayload
            {
                DeviceIdList = new List<string> { "123", "456" },
                QueryTimeout = TimeSpan.FromSeconds(5)
            }), probe.Ref);
            var response = probe.ExpectMsg<SystemEvent>(x => x.Payload is DeviceDetailsPayload);
            var data = response.Payload as DeviceDetailsPayload;

            Assert.Equal(2, data.Devices.Count);

            var reading1 = Assert.IsType<bool>(data.Devices.Where(x => x.DeviceId == "123").FirstOrDefault().IsOnline);
            Assert.True(reading1);

            var reading2 = Assert.IsType<bool>(data.Devices.Where(x => x.DeviceId == "456").FirstOrDefault().IsOnline);
            Assert.False(reading2);

        }

        [Fact]
        public async Task ReturnOnlyActiveDeviceIds()
        {
            var probe = CreateTestProbe();
            var deviceManager = Sys.ActorOf(DeviceManager.Props(), "DeviceManager");

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 1, new RegistrationRequestPayload { DeviceId = "123" }));
            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestDeviceRegistration, 2, new RegistrationRequestPayload { DeviceId = "456" }));

            var firstDevice = await Sys.ActorSelection("akka://test/user/DeviceManager/device-123")
                .ResolveOne(TimeSpan.FromSeconds(3));

            probe.Watch(firstDevice);
            firstDevice.Tell(PoisonPill.Instance);
            probe.ExpectTerminated(firstDevice);

            deviceManager.Tell(new SystemEvent(SystemEventTypesEnum.RequestAllDeviceIds, 3), probe.Ref);
            var response = probe.ExpectMsg<SystemEvent>();
            var payload = response.Payload as DeviceIdListPayload;

            Assert.Single(payload.DeviceIdList);
            Assert.Contains("456", payload.DeviceIdList);

        }
    }
}
