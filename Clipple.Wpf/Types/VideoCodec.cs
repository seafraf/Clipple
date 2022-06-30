using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public class VideoCodec
    {
        /// <summary>
        /// List of supported video encoders.  This is not the full list that ffmpeg supports, but the list that is confirmed 
        /// to work with the options provided in Clipple
        /// </summary>
        public static ObservableCollection<string> SupportedCodecs { get; } = new()
        {
            "libx264",
            "libx265",
            "libaom-av1",
            "libvpx-vp9",
            "libvpx"
        };
    }
}
