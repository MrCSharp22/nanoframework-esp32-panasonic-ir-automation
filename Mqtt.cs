using System;
using System.Collections;
using System.Text;

using nanoFramework.M2Mqtt;

namespace nanoFramework_Panasonic_Automation
{
    internal static class Mqtt
    {
        private static MqttClient MqttClient;

        internal static void Connect()
        {
            try
            {
                if (MqttClient != null && MqttClient.IsConnected)
                {
                    return;
                }

                Disconnect();

                MqttClient = new MqttClient("IP ADDRESS OF INSTANCE")
                {
                    ProtocolVersion = MqttProtocolVersion.Version_5
                };

                MqttClient.Connect(Guid.NewGuid().ToString(), username: "", password: "");
            }
            catch
            {
                // do nothing
            }
        }

        internal static void Disconnect()
        {
            if (MqttClient == null)
            {
                return;
            }

            try
            {
                MqttClient.Disconnect();
                MqttClient.Dispose();
                MqttClient = null;
            }
            catch
            {
                // do nothing
            }
        }

        internal static void AnnounceTempSensorToHomeAssistant(string area)
        {
            try
            {
                MqttClient.Publish($"homeassistant/sensor/{area}Temperature/config",
                        Encoding.UTF8.GetBytes($"{{ \"name\": \"{area} temperature sensor\", \"device_class\": \"temperature\", \"state_topic\": \"homeassistant/sensor/{area}/state\", \"unit_of_measurement\": \"°C\", \"value_template\": \"{{{{ value_json.temperature }}}}\"}}"),
                        "application/json",
                        userProperties: null,
                        qosLevel: nanoFramework.M2Mqtt.Messages.MqttQoSLevel.AtMostOnce,
                        retain: true);
            }
            catch
            {
                // do nothing
            }
        }

        internal static void AnnounceHumiditySensorToHomeAssistant(string area)
        {
            try
            {
                MqttClient.Publish($"homeassistant/sensor/{area}Humidity/config",
                        Encoding.UTF8.GetBytes($"{{ \"name\": \"{area} humidity sensor\", \"device_class\": \"humidity\", \"state_topic\": \"homeassistant/sensor/{area}/state\", \"unit_of_measurement\": \"%\", \"value_template\": \"{{{{ value_json.humidity }}}}\"}}"),
                        "application/json",
                        userProperties: null,
                        qosLevel: nanoFramework.M2Mqtt.Messages.MqttQoSLevel.AtMostOnce,
                        retain: true);
            }
            catch
            {
                // do nothing
            }
        }

        internal static void PublishData(string area, double temp, double humidity)
        {
            try
            {
                MqttClient.Publish($"homeassistant/sensor/{area}/state",
                        Encoding.UTF8.GetBytes($"{{ \"temperature\": {temp}, \"humidity\": {humidity} }}"),
                        "application/json");
            }
            catch
            {
                // do nothing
            }
        }
    }
}
