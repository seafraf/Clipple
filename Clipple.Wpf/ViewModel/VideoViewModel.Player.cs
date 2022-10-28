using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Windows.Media;
using FFmpeg.AutoGen;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Clipple.Types;

namespace Clipple.ViewModel
{
    public partial class VideoViewModel
    {
        /// <summary>
        /// Position of the video play head.
        /// </summary>
        public TimeSpan CurrentTime { get; set; }

        /// <summary>
        /// Volume modifier for all tracks.
        /// </summary>
        public int Volume { get; set; } = 100;

        /// <summary>
        /// Whether or not all audio tracks should be muted.
        /// </summary>
        public bool IsMuted { get; set; } = false;

        /// <summary>
        /// Settings and information for the audio streams in this video
        /// </summary>
        public AudioStreamViewModel[]? AudioStreams { get; set; }

        /// <summary>
        /// Playback speed
        /// </summary>
        public double PlaybackSpeed { get; set; } = 1.0;

        /// <summary>
        /// Timeline zoom
        /// </summary>
        public double TimelineZoom { get; set; } = 0.0;
    }
}
