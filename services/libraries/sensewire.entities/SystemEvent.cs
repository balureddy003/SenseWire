using sensewire.entities.Payloads;
using System;
using System.Collections.Generic;
using System.Text;

namespace sensewire.entities
{
    public class SystemEvent
    {
        public SystemEventTypesEnum EventType { get; }

        public long? CorrelationId { get; }

        public string EntityId { get; }

        public IPayload Payload { get; }

        public DateTime SystemTime { get; }

        public SystemEvent(SystemEventTypesEnum eventType, long? correlationId, IPayload payload = null, string entityId = "")
        {
            EventType = eventType;
            CorrelationId = correlationId;
            EntityId = entityId;
            Payload = payload;
            SystemTime = DateTime.UtcNow;
        }
    }
}
