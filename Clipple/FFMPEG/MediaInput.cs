using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.FFMPEG
{
    public class MediaInput
    {
        public MediaInput(string inputFile)
        {
            InputFile = inputFile;
        }

        #region Properties
        public string InputFile { get; }
        #endregion

        public override string? ToString()
        {
            return $"-i \"{InputFile}\"";
        }
    }
}
