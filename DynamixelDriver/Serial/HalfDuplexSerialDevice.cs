using DynamixelDriver.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace DynamixelDriver.Serial
{
    internal class HalfDuplexSerialDevice : ISerialDevice
    {
        private readonly ISerialDevice _serialDevice;
        private readonly GpioPin _directionPin;
        private readonly GpioPinValue _txDirectionPinValue;
        private readonly GpioPinValue _rxDirectionPinValue;

        public HalfDuplexSerialDevice(ISerialDevice serialDevice, GpioPin directionPin, GpioPinValue txDirection, GpioPinValue rxDirection)
        {
            _serialDevice = serialDevice;
            _directionPin = directionPin;
            _txDirectionPinValue = txDirection;
            _rxDirectionPinValue = rxDirection;

        }

        public IEnumerable<byte> ReadContinuous => _serialDevice.ReadContinuous;

        public void Initialize(uint baudrate)
        {
            _serialDevice.Initialize(baudrate);
            _directionPin.SetDriveMode(GpioPinDriveMode.Output);
            _directionPin.Write(_rxDirectionPinValue);
        }

        public async Task WriteBytesAsync(byte[] bytes)
        {
            _directionPin.Write(_txDirectionPinValue);
            await _serialDevice.WriteBytesAsync(bytes);
            _directionPin.Write(_rxDirectionPinValue);
        }
    }
}
