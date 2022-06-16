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
        public MediaInput(string inputFile, ClipViewModel clip)
        {
            InputFile = inputFile;
            this.clip = clip;
        }

        #region Members
        private readonly ClipViewModel clip;
        #endregion

        #region Properties
        public string InputFile { get; }
        public TimeSpan StartTime => clip.StartTime;
        public TimeSpan Duration => clip.Duration;
        #endregion

        public override string? ToString()
        {
            return $"-ss {StartTime} -t {Duration} -i \"{InputFile}\"";
        }
    }
}
