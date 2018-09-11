using DynamixelDriver.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace DynamixelDriver.Serial
{
    internal class UARTDevice : ISerialDevice
    {
        private readonly SerialDevice _device;
        private DataReader _dataReader;
        private DataWriter _dataWriter;
        private Task _listeningTask;
        private readonly BlockingCollection<byte> _dataRead;
        private readonly CancellationTokenSource _cancelationTokenSource;

        public event EventHandler<string> ReadingErrorOccured;

        private UARTDevice(SerialDevice device)
        {
            _device = device;
            _dataReader = new DataReader(_device.InputStream)
            {
                InputStreamOptions = InputStreamOptions.Partial
            };
            _dataWriter = new DataWriter(_device.OutputStream);
            _dataRead = new BlockingCollection<byte>();
            _cancelationTokenSource = new CancellationTokenSource();
        }

        public void Initialize(uint baudrate)
        {
            _device.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            _device.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            _device.BaudRate = baudrate;
            _device.Parity = SerialParity.None;
            _device.StopBits = SerialStopBitCount.One;
            _device.DataBits = 8;
            _device.ErrorReceived += HandleErrorReceived;
        }

        private void HandleErrorReceived(SerialDevice sender, ErrorReceivedEventArgs args)
        {
            ReadingErrorOccured?.Invoke(this, $"Error received: {args.Error}");
        }

        public IEnumerable<byte> ReadContinuous => _dataRead.GetConsumingEnumerable(_cancelationTokenSource.Token);

        public async Task WriteBytesAsync(byte[] bytes)
        {
            _dataWriter.WriteBytes(bytes);
            await _dataWriter.StoreAsync();
        }

        public void StartListening()
        {
            _listeningTask = Task.Run(async () =>
            {
                while (!_cancelationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await _dataReader.LoadAsync(1);
                        var b = _dataReader.ReadByte();
                        _dataRead.Add(b);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        ReadingErrorOccured?.Invoke(this, ex.Message);
                    }
                }
            });
        }

        public async Task StopListeningAsync()
        {
            if (_listeningTask == null)
            {
                return;
            }

            _dataReader.DetachStream();
            _dataReader.DetachBuffer();
            _dataReader.Dispose();
            _dataRead.CompleteAdding();
            _cancelationTokenSource.Cancel();

            await _listeningTask;
        }


        public static async Task<UARTDevice> GetUARTDeviceAsync()
        {
            try
            {
                var deviceSelector = SerialDevice.GetDeviceSelector("UART0");
                var deviceInformation = await DeviceInformation.FindAllAsync(deviceSelector);
                var uartPort = await SerialDevice.FromIdAsync(deviceInformation.Single().Id);

                return new UARTDevice(uartPort);
            }
            catch (Exception ex)
            {
                throw new Exception("UART could not be found or is already in use.", ex);
            }
        }
    }
}
