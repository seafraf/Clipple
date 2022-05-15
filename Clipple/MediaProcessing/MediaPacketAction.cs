using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.MediaProcessing
{
    public enum MediaPacketAction
    {
        // Timestamp too early, do nothing
        Early,

        // Timestamp too late, do nothing
        Late,

        // Timestamp good, decode packet and send frames
        Decode,

        // Timestamp good, do nothing
        NOP,

        // Packet from unknown/ignored stream
        BadStream,
    }
}
