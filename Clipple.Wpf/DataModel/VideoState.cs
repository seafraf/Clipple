using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.DataModel
{
    /// <summary>
    /// This view model stores a video's current position and volume/mute settings.  These values are set by the video player view
    /// model.  This view model is owned by the Video view model so that these settings can persist when swapping currently active video
    /// or when restarting Clipple
    /// </summary>
    public class VideoState 
    {
        #region Properties
        /// <summary>
        /// Position of the video play head.
        /// </summary>
        public TimeSpan CurTime { get; set; }

        /// <summary>
        /// Volume modifier for all tracks.
        /// </summary>
        public double Volume { get; set; } = 100;

        /// <summary>
        /// Whether or not all audio tracks should be muted.
        /// </summary>
        public bool Muted { get; set; } = false;

        /// <summary>
        /// The volumes of individual tracks.  The key is the audio stream's stream index.
        /// </summary>
        public Dictionary<int, double> TrackVolume { get; set; } = new();

        /// <summary>
        /// Whether or not individual tracks are muted.  The key is the audio stream's stream index.
        /// </summary>
        public Dictionary<int, bool> MutedTracks { get; set; } = new();
        #endregion
    }
}
