using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Util.ISOBMFF
{
    
    internal class SimpleParser
    {
        private readonly string file;

        public List<Track> Tracks { get; set; } = new List<Track>();

        public SimpleParser(string file)
        {
            this.file = file;
        }

        public void Parse()
        {
            using var fr = new FileStream(file, FileMode.Open, FileAccess.Read);

            // Use the first box as a magic number check
            var firstBox = new Box(fr);
            if (firstBox.Type != "ftyp")
                throw new InvalidOperationException("Input is not a ISOBMFF container");

            fr.Position += firstBox.Size;

            while (fr.Position < fr.Length)
            {
                var box = new Box(fr);
                if (box.Type == "moov")
                {
                    Read_MOOV(fr, fr.Position + box.Size);
                }
                else
                    fr.Position += box.Size;
            }
        }

        private void Read_MOOV(Stream stream, long boxEnd)
        {
            while (stream.Position < boxEnd)
            {
                var box = new Box(stream);
                if (box.Type == "trak")
                {
                    Read_TRAK(stream, stream.Position + box.Size);
                }
                else 
                    stream.Position += box.Size;
            }
        }

        private void Read_TRAK(Stream stream, long boxEnd)
        {
            var track = new Track();
            Tracks.Add(track);

            while (stream.Position < boxEnd)
            {
                var box = new Box(stream);
                if (box.Type == "udta")
                {
                    Read_UDTA(stream, track, stream.Position + box.Size);
                }
                else
                    stream.Position += box.Size;
            }
        }

        private void Read_UDTA(Stream stream, Track track, long boxEnd)
        {
            while (stream.Position < boxEnd)
            {
                var box = new Box(stream);
                if (box.Type == "name")
                {
                    // Name is null terminated
                    var buf = new byte[box.Size];
                    stream.Read(buf, 0, (int)box.Size);

                    // Order is important
                    track.Name = Encoding.UTF8.GetString(buf).Trim('\0');
                }
                else
                    stream.Position += box.Size;
            }
        }
    }
}
