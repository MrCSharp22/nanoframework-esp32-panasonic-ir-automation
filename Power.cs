using System;
using System.Diagnostics;

namespace nanoFramework_Panasonic_Automation
{
    internal static class Power
    {
        internal static void Sleep(TimeSpan sleepDuration)
        {
            try
            {
                nanoFramework.Hardware.Esp32.Sleep.EnableWakeupByTimer(sleepDuration);
                nanoFramework.Hardware.Esp32.Sleep.StartDeepSleep();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SLEEP ERROR ==> {ex.Message}");
            }
        }
    }
}
