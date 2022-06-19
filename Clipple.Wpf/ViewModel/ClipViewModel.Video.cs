using Clipple.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Clipple.ViewModel
{
    public partial class ClipViewModel
    {
        private void InitialiseVideoViews()
        {
            ResolutionPresets.GroupDescriptions.Clear();
            ResolutionPresets.GroupDescriptions.Add(new PropertyGroupDescription("AspectRatioString"));
        }

        /// <summary>
        /// Video bitrate, kilobits/second.  
        /// If UseTargetSize is true, this will return the bitrate required to reach VideoTargetSize
        /// </summary>
        private long videoBitrate = 150000;
        public long VideoBitrate
        {
            get
            {
                if (!UseTargetSize)
                    return videoBitrate;

                // Total bitrate the whole media can take up
                var maxTotalBitrate = (long)((OutputTargetSize * 8000.0) / Duration.TotalSeconds);

                // Subtract audio from total bitrate, as that audio bitrate won't be changed when UseTargetSize is true
                // This has to be done for each enabled audio channel
                var enabledTracks = AudioSettings.Where(x => x.IsEnabled).Count();

                // Merging audio tracks means there is only one.. unless no audio tracks are enabled, in which case there is zero
                var totalAudioBitrate = AudioBitrate * Math.Min(enabledTracks, MergeAudio ? 1 : enabledTracks);
                maxTotalBitrate -= (int)(totalAudioBitrate * Duration.TotalSeconds);

                // subtract 1% for muxing overhead
                return (long)(maxTotalBitrate * 0.99);
            }
            set => SetProperty(ref videoBitrate, value);
        }

        /// <summary>
        /// Video FPS
        /// </summary>
        private int targetFPS;
        public int TargetFPS
        {
            get => Math.Max(1, Math.Min(targetFPS, SourceFPS));
            set => SetProperty(ref targetFPS, value);
        }

        /// <summary>
        /// Video width
        /// </summary>
        private int targetWidth;
        public int TargetWidth
        {
            get => Math.Max(1, Math.Min(targetWidth, SourceWidth));
            set
            {
                ResolutionPreset = null;
                SetProperty(ref targetWidth, value);
            }
        }

        /// <summary>
        /// Video height
        /// </summary>
        private int targetHeight;
        public int TargetHeight
        {
            get => Math.Max(1, Math.Min(targetHeight, SourceHeight));
            set
            {
                ResolutionPreset = null;
                SetProperty(ref targetHeight, value);
            }
        }

        /// <summary>
        /// List of resolution presets for the resolution preset checkbox
        /// </summary>
        [JsonIgnore]
        public ListCollectionView ResolutionPresets { get; } = new(new ObservableCollection<ResolutionPreset>()
        {
            // 32:9
            new ResolutionPreset(32, 9, 5120, 1440),
            new ResolutionPreset(32, 9, 3840, 1080),

            // 21:9
            new ResolutionPreset(21, 9, 5120, 2160),
            new ResolutionPreset(21, 9, 3440, 1440),

            // 16:9
            new ResolutionPreset(16, 9, 7680, 4320),
            new ResolutionPreset(16, 9, 5120, 2880),
            new ResolutionPreset(16, 9, 3840, 2160),
            new ResolutionPreset(16, 9, 2560, 1440),
            new ResolutionPreset(16, 9, 1920, 1080),
            new ResolutionPreset(16, 9, 1600, 900),
            new ResolutionPreset(16, 9, 1366, 768),
            new ResolutionPreset(16, 9, 1280, 720),
            new ResolutionPreset(16, 9, 7680, 4320),
            new ResolutionPreset(16, 9, 7680, 4320),

            // 16:10
            new ResolutionPreset(16, 10, 2560, 1600),
            new ResolutionPreset(16, 10, 1920, 1200),
            new ResolutionPreset(16, 10, 1280, 800),

            // 4:3
            new ResolutionPreset(4, 3, 2048, 1536),
            new ResolutionPreset(4, 3, 1920, 1440),
            new ResolutionPreset(4, 3, 1600, 1200),
            new ResolutionPreset(4, 3, 1440, 1080),
            new ResolutionPreset(4, 3, 1400, 1050),
        });

        /// <summary>
        /// Resolution preset set by the resolution preset combo box.  Setting this will update TargetWidth and TargetHeight
        /// </summary>
        private ResolutionPreset? resolutionPreset;
        [JsonIgnore]
        public ResolutionPreset? ResolutionPreset
        {
            get => resolutionPreset;
            set
            {
                SetProperty(ref resolutionPreset, value);

                if (value != null)
                {
                    targetWidth  = value.ScreenW;
                    targetHeight = value.ScreenH;

                    OnPropertyChanged(nameof(TargetWidth));
                    OnPropertyChanged(nameof(TargetHeight));
                }
            }
        }

        /// <summary>
        /// Videc codec for encoding
        /// </summary>
        private string videoCodec = "libx264";
        public string VideoCodec
        {
            get => videoCodec;
            set => SetProperty(ref videoCodec, value);
        }

        /// <summary>
        /// List of supported video encoders.  This is not the full list that ffmpeg supports, but the list that is confirmed 
        /// to work with the options provided in Clipple
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<string> SupportedVideoCodecs { get; } = new()
        {
            "libx264",
            "libx265",
            "libaom-av1",
            "libvpx-vp9",
            "libvpx"
        };
    }
}
