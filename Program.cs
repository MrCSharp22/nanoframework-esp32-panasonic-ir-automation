using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework_Panasonic_Automation
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("INFO ==> PROGRAM STARTED.");

            var statusLedPin = 22;
            var dhtEchoPin = 33;
            var dhtTriggerPin = 32;
            var dhtWakePin = 26;
            var irWakePin = 15;
            var irSignalPin = 16;
            var area = "Bedroom";

            using var gpioController = new GpioController();
            using var statusLed = gpioController.OpenPin(statusLedPin, PinMode.Output);
            using var dhtWaker = gpioController.OpenPin(dhtWakePin, PinMode.Output);
            using var irWaker = gpioController.OpenPin(irWakePin, PinMode.Output);

            using var dhtSensor = new Iot.Device.DHTxx.Esp32.Dht22(dhtEchoPin, 
                dhtTriggerPin, 
                gpioController: gpioController);

            var busyStatusThread = SignalBusyStatus(statusLed);
            busyStatusThread.Start();

            // wake the DHT22 now. This should give it enough time to "warm up"
            dhtWaker.Write(PinValue.High);

            Wifi.Connect();
            Mqtt.Connect();

            Mqtt.AnnounceTempSensorToHomeAssistant(area);
            Thread.Sleep(1000);
            Mqtt.AnnounceHumiditySensorToHomeAssistant(area);

            // Wait a bit to give the sensor and Wifi a chance to stabilize
            Thread.Sleep(500);

            var attemptNumber = 0;
            while (attemptNumber < 5)
            {
                var temp = dhtSensor.Temperature;
                var humidity = dhtSensor.Humidity;

                if (!dhtSensor.IsLastReadSuccessful)
                {

                    Debug.WriteLine($"ERROR ==> FAILED TO READ TEMP/HUMIDITY. ATTEMPT {attemptNumber}.");

                    attemptNumber++;
                    Thread.Sleep(1000);

                    continue;
                }

                // very basic automation implementation (WIP / Incomplete)
                if (temp.DegreesCelsius > 25)
                {
                    irWaker.Write(PinValue.High);
                    PanasonicIRController.TurnOn(16, PanasonicACMode.Cool, irSignalPin);
                    irWaker.Write(PinValue.Low);
                }
                else if (temp.DegreesCelsius < 20)
                {
                    irWaker.Write(PinValue.High);
                    PanasonicIRController.TurnOn(25, PanasonicACMode.Heat, irSignalPin);
                    irWaker.Write(PinValue.Low);
                }

                Mqtt.PublishData(area, temp.DegreesCelsius, humidity.Percent);
                break;
            }

            Debug.WriteLine("INFO ==> GOING TO SLEEP. SEE YOU SOON!");

            Thread.Sleep(1000);
            Mqtt.Disconnect();
            busyStatusThread.Abort();

            // put the DHT22 to sleep to save on power
            dhtWaker.Write(PinValue.Low);

            // put the MCU to sleep and set it to wake up in 15 minutes
            Power.Sleep(TimeSpan.FromMinutes(15));
        }

        private static Thread SignalBusyStatus(GpioPin led)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        led.Write(PinValue.Low);
                        Thread.Sleep(250);

                        led.Write(PinValue.High);
                        Thread.Sleep(500);
                    }
                }
                catch (ThreadAbortException)
                {
                    // make sure the led is switched off to save power
                    led.Write(PinValue.Low);
                }
            });

            return thread;
        }
    }
}
