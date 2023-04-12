using System.Device.Wifi;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;

using nanoFramework.Networking;

namespace nanoFramework_Panasonic_Automation
{
    internal static class Wifi
    {
        internal static void Connect()
        {
            var wifiConfig = GetConfiguration();
            if (wifiConfig == null || string.IsNullOrEmpty(wifiConfig.Ssid))
            {
                #region HIDE
                Configure(ssid: "", password: "");
                #endregion

                wifiConfig = GetConfiguration();
                var firstConnectionResult = WifiNetworkHelper.ConnectDhcp(wifiConfig.Ssid,
                    wifiConfig.Password,
                    reconnectionKind: WifiReconnectionKind.Automatic,
                    requiresDateTime: true,
                    token: new CancellationTokenSource(60000).Token);

                Debug.WriteLine($"WIFI First-Time Connection Status: {firstConnectionResult}");
            }
            else
            {
                var reconnectionResult = WifiNetworkHelper.Reconnect(requiresDateTime: true);
                Debug.WriteLine($"WIFI Reconnection Status: {reconnectionResult}");
            }
        }

        internal static Wireless80211Configuration GetConfiguration()
        {
            var wiressAPInterface = GetInterface();
            return Wireless80211Configuration.GetAllWireless80211Configurations()[wiressAPInterface.SpecificConfigId];
        }

        internal static void Configure(string ssid, string password)
        {
            var wconf = GetConfiguration();
            wconf.Options = Wireless80211Configuration.ConfigurationOptions.AutoConnect
                | Wireless80211Configuration.ConfigurationOptions.Enable;
            wconf.Authentication = AuthenticationType.WPA2;
            wconf.Ssid = ssid;
            wconf.Password = password;
            wconf.SaveConfiguration();
        }

        internal static NetworkInterface GetInterface()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    return networkInterface;
                }
            }

            return null;
        }
    }
}
