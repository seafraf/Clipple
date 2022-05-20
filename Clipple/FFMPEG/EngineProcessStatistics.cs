using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.FFMPEG
{
    public class EngineProcessStatistics
    {
        public EngineProcessStatistics(int frame, TimeSpan time, double bitrate)
        {
            Frame   = frame;
            Time    = time;
            Bitrate = bitrate;
        }

        public int Frame { get; }
        public TimeSpan Time { get; }
        public double Bitrate { get; }
    }
}
