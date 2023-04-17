using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework_Panasonic_Automation
{
	public class Program
	{
		private const int statusLedPin = 22;
		private const int dhtEchoPin = 33;
		private const int dhtTriggerPin = 32;
		private const int dhtWakePin = 26;
		private const int irWakePin = 15;
		private const int irSignalPin = 16;
		private const string area = "Bedroom";

		public static void Main()
		{
			Debug.WriteLine("INFO ==> PROGRAM STARTED.");

			using var gpioController = new GpioController();
			using var statusLed = gpioController.OpenPin(statusLedPin, PinMode.Output);
			using var dhtWaker = gpioController.OpenPin(dhtWakePin, PinMode.Output);
			using var irWaker = gpioController.OpenPin(irWakePin, PinMode.Output);
			using var dhtSensor = new Iot.Device.DHTxx.Esp32.Dht22(dhtEchoPin, dhtTriggerPin, gpioController: gpioController);

			var busySignal = SignalBusyStatus(statusLed);
			busySignal.Start();

			// wake the DHT22 now. This should give it enough time to "warm up"
			dhtWaker.Write(PinValue.High);

			// while DHT22 is "warming up", connect to wifi, mqtt, and execute needed announcements
			Wifi.Connect();
			Mqtt.Connect();
			Mqtt.AnnounceTempSensorToHomeAssistant(area);
			Mqtt.AnnounceHumiditySensorToHomeAssistant(area);

			var attemptNumber = 0;
			while (attemptNumber < 5)
			{
				var temp = dhtSensor.Temperature;
				var humidity = dhtSensor.Humidity;

				if (!dhtSensor.IsLastReadSuccessful)
				{

					Debug.WriteLine($"ERROR ==> FAILED TO READ TEMP/HUMIDITY. ATTEMPT {attemptNumber}.");
					Thread.Sleep(1000);

					attemptNumber++;
					continue;
				}

				PanasonicAcAutomator.Process(temp, humidity, irSignalPin, irWaker);

				Mqtt.PublishData(area, temp.DegreesCelsius, humidity.Percent);
				break;
			}

			Debug.WriteLine($"INFO ==> GOING TO SLEEP. SEE YOU SOON!");

			// clean up
			Mqtt.Disconnect();

			// put the DHT22 and IR to sleep to save on power
			dhtWaker.Write(PinValue.Low);
			irWaker.Write(PinValue.Low);

			busySignal.Abort();

			// put the MCU to sleep and set it to wake up in 1 minute
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
