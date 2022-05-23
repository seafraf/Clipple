using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Util
{
    public class BigEndianReader
    {
        private Stream stream;

        public BigEndianReader(Stream stream)
        {
            this.stream = stream;  
        }

        public byte[] ReadBytes(int n, bool reverse = true)
        {
            byte[] bytes = new byte[n];
            stream.Read(bytes, 0, n);

            if (reverse)
                Array.Reverse(bytes);

            return bytes;
        }

        public uint ReadU32()
        {
            return BitConverter.ToUInt32(ReadBytes(4));
        }

        public ulong ReadU64()
        {
            return BitConverter.ToUInt64(ReadBytes(8));
        }

        public int ReadI32()
        {
            return BitConverter.ToInt32(ReadBytes(4));
        }

        public long ReadI64()
        {
            return BitConverter.ToInt64(ReadBytes(8));
        }
    }
}
