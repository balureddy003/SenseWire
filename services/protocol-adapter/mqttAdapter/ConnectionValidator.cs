using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqttAdapter
{
    public static class ConnectionValidator
    {
        public static void ValidateConnection(MqttConnectionValidatorContext c)
        {
            {
                if (c.ClientId.Length < 10)
                {
                    c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                    return;
                }

                if (c.Username != "mySecretUser")
                {
                    c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                    return;
                }

                if (c.Password != "mySecretPassword")
                {
                    c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                    return;
                }

                c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
            }
        }
    }
}
