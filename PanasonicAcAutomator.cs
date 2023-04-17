using System.Device.Gpio;

using UnitsNet;

namespace nanoFramework_Panasonic_Automation
{
	internal static class PanasonicAcAutomator
	{
		internal static void Process(Temperature currentTemperature,
			RelativeHumidity currentHumidity,
			int irSignalPin, GpioPin irPowerPin)
		{
			var tempCelsius = currentTemperature.DegreesCelsius;
			var humidity = currentHumidity.Percent;

			if (humidity > 50)
			{
				irPowerPin.Write(PinValue.High);

				PanasonicIRController.TurnOn(targetTemperature: 23,
					PanasonicACMode.Dry,
					irSignalPin);

				irPowerPin.Write(PinValue.Low);
			}
			else if (tempCelsius > 25)
			{
				irPowerPin.Write(PinValue.High);

				PanasonicIRController.TurnOn(PanasonicIRController.MinTemperature,
					PanasonicACMode.Cool,
					irSignalPin);

				irPowerPin.Write(PinValue.Low);
			}
			else if (tempCelsius < 20)
			{
				irPowerPin.Write(PinValue.High);

				PanasonicIRController.TurnOn(targetTemperature: 25,
					PanasonicACMode.Heat,
					irSignalPin);

				irPowerPin.Write(PinValue.Low);
			}
			else
			{
				irPowerPin.Write(PinValue.High);

				PanasonicIRController.TurnOff(irSignalPin);

				irPowerPin.Write(PinValue.Low);
			}
		}
	}
}
