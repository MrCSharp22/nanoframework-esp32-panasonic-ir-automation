using System;

using nanoFramework.Hardware.Esp32.Rmt;

namespace nanoFramework_Panasonic_Automation
{
    internal static class PanasonicIRController
    {
        private const ushort HeaderPulse = 3468;
        private const ushort HeaderSpace = 1767;
        private const ushort Pulse = 432;
        private const ushort ZeroSpace = 432;
        private const ushort OneSpace = 1296;
        private const ushort PauseSpace = 10000;

        private const int OnOffModeByteIndex = 13;
        private const int TempByteIndex = 14;
        private const int ProfileByteIndex = 21;

        private static RmtCommand Header = new RmtCommand(HeaderPulse, true, HeaderSpace, false);
        private static RmtCommand ZeroBit = new RmtCommand(Pulse, true, ZeroSpace, false);
        private static RmtCommand OneBit = new RmtCommand(Pulse, true, OneSpace, false);
        private static RmtCommand Pause = new RmtCommand(Pulse, true, PauseSpace, false);
        private static RmtCommand End = new RmtCommand(Pulse, true, 0, false);

        public static void TurnOn(byte temp, PanasonicACMode mode, int irChannelPinNumber)
        {
            var commandData = GetStartingCommandData();

            // set on flag
            commandData[OnOffModeByteIndex] |= 0x01;

            // set mode
            commandData[OnOffModeByteIndex] |= (byte)mode;

            //set temp
            commandData[TempByteIndex] = (byte)(temp * 2);

            SendIRCommand(irChannelPinNumber, commandData);
        }

        public static void TurnOff(int irChannelPinNumber)
        {
            var commandData = GetStartingCommandData(); // starting command data is by default OFF
            SendIRCommand(irChannelPinNumber, commandData);
        }

        private static TransmitChannelSettings GetTransmitChannelSettings(int irChannelPinNumber)
            => new(irChannelPinNumber)
            {
                ClockDivider = 80,

                EnableCarrierWave = true,
                CarrierLevel = true,
                CarrierWaveFrequency = 38_000,
                CarrierWaveDutyPercentage = 50,

                IdleLevel = false,
                EnableIdleLevelOutput = true,

                NumberOfMemoryBlocks = 4,
                SignalInverterEnabled = false,
            };

        private static byte[] GetStartingCommandData()
            => new byte[]
        {
            /*  Byte 0 - 7   */ 0x40, 0x04, 0x07, 0x20, 0x00, 0x00, 0x00, 0x60, // frame 1: static

            //  frame 2
            /*  Byte 8 - 12  */ 0x02, 0x20, 0xE0, 0x04, 0x00, // this is static
            
            /* Byte 13       */ 0x38, // On/Off + Mode
            /* Byte 14       */ 0x20, // Temperature
            /* Byte 15       */ 0x80,
            /* Byte 16       */ 0x31,
            /* Byte 17       */ 0x00,
            /* Byte 18       */ 0x00,
            /* Byte 19       */ 0x08,
            /* Byte 20       */ 0x80,
            /* Byte 21       */ 0x00, // Profile
            /* Byte 22       */ 0x00,
            /* Byte 23       */ 0x81,
            /* Byte 24       */ 0x00,
            /* Byte 25       */ 0x00,
            /* Byte 26       */ 0x7E // crc
        };

        private static void AddCommands(this TransmitterChannel txChannel, byte[] data, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                byte b = data[i];
                for (byte mask = 0x01; mask > 0x00 && mask < 0xFF; mask <<= 1)
                {
                    if ((b & mask) > 0)
                    {
                        txChannel.AddCommand(OneBit);
                    }
                    else
                    {
                        txChannel.AddCommand(ZeroBit);
                    }
                }
            }
        }

        private static byte CalcCrc(byte[] data, int start, int end)
        {
            byte crc = 0x00;
            for (var i = start; i < end; i++)
            {
                crc += data[i];
            }

            return crc;
        }

        private static void SendIRCommand(int irChannelPinNumber, byte[] commandData)
        {
            // setup an IR trasnmitter channel
            var irTxChannelSettings = GetTransmitChannelSettings(irChannelPinNumber);
            using var irTxChannel = new TransmitterChannel(irTxChannelSettings);
            irTxChannel.ClearCommands();

            // add command data to the tx channel buffer
            irTxChannel.AddCommand(Header);
            irTxChannel.AddCommands(commandData, 0, 8); // frame 1
            irTxChannel.AddCommand(Pause);
            irTxChannel.AddCommand(Header);
            irTxChannel.AddCommands(commandData, 8, commandData.Length - 1); // frame 2 without CRC

            // calculate the crc
            var crc = CalcCrc(commandData, 8, commandData.Length - 1);
            irTxChannel.AddCommands(new byte[] { crc }, 0, 1); // CRC
            irTxChannel.AddCommand(End);

            // finally, send the command
            irTxChannel.Send(waitTxDone: true);
        }
    }

    internal enum PanasonicACMode : byte
    {
        // bits already left shifted by 4 (<< 4)

        Auto = 0x00,

        Fan = 0x60,

        Dry = 0x20,

        Cool = 0x30,

        Heat = 0x40,
    }

    internal enum PanasonicACProfile : byte
    {
        Normal = 0x10,

        Boost = 0x11,

        Quiet = 0x30
    }
}
