using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public class AudioCodec
    {
        /// <summary>
        /// List of supported audio encoders.  This is not the full list that ffmpeg supports, but the list that is confirmed 
        /// to work with the options provided in Clipple
        /// </summary>
        public static ObservableCollection<string> SupportedCodecs { get; } = new()
        {
            "libopus",
            "libvorbis",
            "libfdk_aac",
            "libmp3lame",
            "eac3",
            "ac3",
            "aac",
            "libtwolame",
            "vorbis",
            "mp2",
            "wmav2",
            "wmav1"
        };
    }
}
