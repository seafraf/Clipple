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

            CropWidth  = SourceWidth;
            CropHeight = SourceHeight;
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
                maxTotalBitrate -= AudioBitrate * Math.Min(enabledTracks, MergeAudio ? 1 : enabledTracks);

                // subtract 1% for muxing overhead
                return (long)(maxTotalBitrate * 0.99);
            }
            set => SetProperty(ref videoBitrate, value);
        }

        /// <summary>
        /// Video FPS
        /// </summary>
        private int? targetFPS = null;
        public int? TargetFPS
        {
            get => useSourceFPS ? SourceFPS : (targetFPS ?? SourceFPS);
            set => SetProperty(ref targetFPS, value);
        }

        /// <summary>
        /// Video width
        /// </summary>
        private int? targetWidth = null;
        public int? TargetWidth
        {
            get => useSourceResolution ? SourceWidth : (targetWidth ?? SourceWidth);
            set
            {
                ResolutionPreset = null;
                SetProperty(ref targetWidth, value);
            }
        }

        /// <summary>
        /// Video height
        /// </summary>
        private int? targetHeight = null;
        public int? TargetHeight
        {
            get => useSourceResolution ? SourceHeight : (targetHeight ?? SourceHeight);
            set
            {
                ResolutionPreset = null;
                SetProperty(ref targetHeight, value);
            }
        }

        /// <summary>
        /// Should the source video resolution be used in the the output clip?
        /// </summary>
        private bool useSourceResolution;
        public bool UseSourceResolution
        {
            get => useSourceResolution;
            set
            {
                SetProperty(ref useSourceResolution, value);
                OnPropertyChanged(nameof(TargetWidth));
                OnPropertyChanged(nameof(TargetHeight));
            }
        }

        /// <summary>
        /// Should the source video FPS be used in the output clip?
        /// </summary>
        private bool useSourceFPS;
        public bool UseSourceFPS
        {
            get => useSourceFPS;
            set
            {
                SetProperty(ref useSourceFPS, value);
                OnPropertyChanged(nameof(TargetFPS));
            }
        }

        /// <summary>
        /// Should the video be cropped according to the crop(x/y/width/height) settings?
        /// </summary>
        private bool shouldCrop;
        public bool ShouldCrop
        {
            get => shouldCrop;
            set => SetProperty(ref shouldCrop, value);
        }

        /// <summary>
        /// Crop x-position
        /// </summary>
        private int cropX = 0;
        public int CropX
        {
            get => cropX;
            set => SetProperty(ref cropX, value);
        }

        /// <summary>
        /// Crop y-position
        /// </summary>
        private int cropY = 0;
        public int CropY
        {
            get => cropY;
            set => SetProperty(ref cropY, value);
        }

        /// <summary>
        /// Crop width
        /// </summary>
        private int cropWidth = 0;
        public int CropWidth
        {
            get => cropWidth;
            set => SetProperty(ref cropWidth, value);
        }


        /// <summary>
        /// Crop y-position
        /// </summary>
        private int cropHeight = 0;
        public int CropHeight
        {
            get => cropHeight;
            set => SetProperty(ref cropHeight, value);
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
    }
}
