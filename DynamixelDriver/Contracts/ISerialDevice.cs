using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamixelDriver.Contracts
{
    internal interface ISerialDevice
    {
        void Initialize(uint baudrate);

        IEnumerable<byte> ReadContinuous { get; }

        Task WriteBytesAsync(byte[] bytes);
    }
}
