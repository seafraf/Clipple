using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public class OutputFormat
    {
        public OutputFormat(string ffmpegName, string extension, string displayName, bool supportsAudio = true, bool supportsVideo = true)
        {
            Name            = ffmpegName;
            Extension       = extension;
            DisplayName     = displayName;
            SupportsAudio   = supportsAudio;
            SupportsVideo   = supportsVideo;
        }

        #region Properties
        public string Name { get; }
        public string Extension { get; }
        public string DisplayName { get; }
        public bool SupportsAudio { get; }
        public bool SupportsVideo { get; }
        public string Category
        {
            get
            {
                if (SupportsVideo && SupportsAudio)
                    return "Audio & Video";

                if (SupportsVideo && !SupportsAudio)
                    return "Video only";

                if (!SupportsVideo && SupportsAudio)
                    return "Audio only";

                return "?";
            }
        }

        public override string? ToString()
        {
            return $"{DisplayName} ({Extension})";
        }
        #endregion
    }
}
