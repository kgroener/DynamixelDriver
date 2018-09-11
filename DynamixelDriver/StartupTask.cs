using DynamixelDriver.Serial;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace DynamixelDriver
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const int DEFAULT_DYNAMIXEL_BAUDRATE = 57600;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            await RunAsync().ContinueWith((t) => deferral.Complete());
        }

        private async Task RunAsync()
        {
            var uartDevice = await UARTDevice.GetUARTDeviceAsync();
            var gpioController = await GpioController.GetDefaultAsync();
            var directionPin = gpioController.OpenPin(27); // GPIO27 (pin #13)
            var halfDuplexUARTDevice = new HalfDuplexSerialDevice(uartDevice, directionPin, GpioPinValue.High, GpioPinValue.Low);

            halfDuplexUARTDevice.Initialize(DEFAULT_DYNAMIXEL_BAUDRATE);

            while (true)
            {
                await Task.Delay(1000);
                await halfDuplexUARTDevice.WriteBytesAsync(new byte[] { 255, 128, 0 });
            }
        }
    }
}
