using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public partial class ClipViewModel
    {
        /// <summary>
        /// Whether or not the video bitrate should be set to try and achieve a specific output size
        /// </summary>
        private bool useTargetSize = false;
        public bool UseTargetSize
        {
            get => useTargetSize;
            set
            {
                SetProperty(ref useTargetSize, value);
                OnPropertyChanged(nameof(VideoBitrate));
                OnPropertyChanged(nameof(TwoPassEncoding));
            }
        }

        /// <summary>
        /// Media output target size in megabytes. It is important that the full media including all the size of the video, audio and container
        /// remains at or below this target size as users use it mostly to upload with upload size maximums (e.g Discord).
        /// </summary>
        private double outputTargetSize = 100;
        public double OutputTargetSize
        {
            get => outputTargetSize;
            set
            {
                SetProperty(ref outputTargetSize, value);
                OnPropertyChanged(nameof(VideoBitrate));
            }
        }

        /// <summary>
        /// Whether or not to use two pass encoding, currently this is only required to reach specific target sizes and the user has no other control
        /// over whether or not two pass encoding is used
        /// </summary>
        public bool TwoPassEncoding => UseTargetSize;
    }
}
