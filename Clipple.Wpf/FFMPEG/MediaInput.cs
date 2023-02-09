using Clipple.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.FFMPEG
{
    public class MediaInput
    {
        public MediaInput(string inputFile, Media media)
        {
            InputFile = inputFile;
            this.media = media;
        }

        #region Members
        private readonly Media media;
        #endregion

        #region Properties
        public string InputFile { get; }
        private Clip Clip => media.Clip;

        public TimeSpan StartTime => Clip.StartTime;
        public TimeSpan Duration => Clip.Duration;
        #endregion

        public override string? ToString()
        {
            return $"-ss {StartTime} -t {Duration} -i \"{InputFile}\"";
        }
    }
}
