using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipMan.Util.ISOBMFF
{
    public class Box
    {
        /// <summary>
        /// The type of this box
        /// </summary>
        public string Type { get; }

        public string? ExtendedType { get; }

        /// <summary>
        /// The size (not including the header) of this box
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// The position in the stream that this box was read from
        /// </summary>
        public long Start { get; }

        public Box(Stream reader)
        {
            var ber = new BigEndianReader(reader);

            // Record start position
            Start = reader.Position;

            // Standard size
            var size = ber.ReadI32();

            // Standard type
            Type = Encoding.UTF8.GetString(ber.ReadBytes(4, false));

            if (size == 1)
            {
                // 
                // firstSize + 4
                // type      + 4
                // realSize  + 8
                //           = 16
                Size = ber.ReadI64() - 16;
            }
            else
            {
                // 
                // firstSize + 4
                // type      + 4
                //           = 8
                Size = size - 8;
            }

            // Extended type
            if (Type == "uuid")
            {
                ExtendedType = Encoding.UTF8.GetString(ber.ReadBytes(2, false));
                Size -= 2;
            }
        }
    }
}
