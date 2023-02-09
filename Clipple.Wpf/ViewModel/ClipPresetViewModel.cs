using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipple.ViewModel
{
    public class ClipPresetViewModel : ObservableObject, IEquatable<ClipPresetViewModel?>, IJsonOnDeserialized
    {
        public ClipPresetViewModel(
            string name, string category, 
            long? videoBitrate = null, long? audioBitrate = null, 
            int? targetWidth = null, int? targetHeight = null,
            int? fps = null,
            bool useTargetSize = false, double? targetSize = null, 
            string? videoCodec = null, string? audioCodec = null, 
            bool shouldCrop = false, int? cropX = null, int? cropY = null, int? cropWidth = null, int? cropHeight = null,
            ContainerFormat? outputFormat = null, 
            long priority = 0)
        {
            this.name           = name;
            this.category       = category;
            this.videoBitrate   = videoBitrate;
            this.audioBitrate   = audioBitrate;
            this.targetWidth    = targetWidth;
            this.targetHeight   = targetHeight;
            this.fps            = fps;
            this.videoCodec     = videoCodec;
            this.audioCodec     = audioCodec;
            this.audioBitrate   = audioBitrate;
            this.useTargetSize  = useTargetSize;
            this.shouldCrop     = shouldCrop;
            this.cropX          = cropX;
            this.cropY          = cropY;
            this.cropWidth      = cropWidth;
            this.cropHeight     = cropHeight;
            this.targetSize     = targetSize;
            this.outputFormat   = outputFormat;
            this.priority       = priority;

            LoadComboBoxes();
        }

        /// <summary>
        /// For deserialization only
        /// </summary>
        public ClipPresetViewModel()
        {

        }

        #region Properties
        private string name = "";
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string category = "";
        public string Category
        {
            get => category;
            set => SetProperty(ref category, value);
        }

        private long priority;
        public long Priority
        {
            get => priority;
            set => SetProperty(ref priority, value);
        }

        private long? videoBitrate;
        public long? VideoBitrate
        {
            get => videoBitrate;
            set => SetProperty(ref videoBitrate, value);
        }


        private long? audioBitrate;
        public long? AudioBitrate
        {
            get => audioBitrate;
            set => SetProperty(ref audioBitrate, value);
        }

        private int? targetWidth;
        public int? TargetWidth
        {
            get => targetWidth;
            set => SetProperty(ref targetWidth, value);
        }

        private int? targetHeight;
        public int? TargetHeight
        {
            get => targetHeight;
            set => SetProperty(ref targetHeight, value);
        }

        private int? fps;
        public int? FPS
        {
            get => fps;
            set => SetProperty(ref fps, value);
        }

        private bool useTargetSize;
        public bool UseTargetSize
        {
            get => useTargetSize;
            set => SetProperty(ref useTargetSize, value);
        }

        private double? targetSize;
        public double? TargetSize
        {
            get => targetSize;
            set => SetProperty(ref targetSize, value);
        }

        private bool shouldCrop;
        public bool ShouldCrop
        {
            get => shouldCrop;
            set => SetProperty(ref shouldCrop, value);
        }

        private int? cropX;
        public int? CropX
        {
            get => cropX;
            set => SetProperty(ref cropX, value);
        }

        private int? cropY;
        public int? CropY
        {
            get => cropY;
            set => SetProperty(ref cropY, value);
        }

        private int? cropWidth;
        public int? CropWidth
        {
            get => cropWidth;
            set => SetProperty(ref cropWidth, value);
        }

        private int? cropHeight;
        public int? CropHeight
        {
            get => cropHeight;
            set => SetProperty(ref cropHeight, value);
        }

        private string? videoCodec;
        public string? VideoCodec
        {
            get => videoCodec;
            set => SetProperty(ref videoCodec, value);
        }

        private int videoCodecIndex = -1;
        [BsonIgnore]
        public int VideoCodecIndex
        {
            get => videoCodecIndex;
            set
            {
                SetProperty(ref videoCodecIndex, value);

                VideoCodec = Types.VideoCodec.SupportedCodecs.ElementAtOrDefault(value);
            }
        }

        private string? audioCodec;
        public string? AudioCodec
        {
            get => audioCodec;
            set => SetProperty(ref audioCodec, value);
        }

        private int audioCodecIndex = -1;
        [BsonIgnore]
        public int AudioCodecIndex
        {
            get => audioCodecIndex;
            set
            {
                SetProperty(ref audioCodecIndex, value);

                AudioCodec = Types.AudioCodec.SupportedCodecs.ElementAtOrDefault(value);
            }
        }

        private ContainerFormat? outputFormat;
        public ContainerFormat? OutputFormat
        {
            get => outputFormat;
            set => SetProperty(ref outputFormat, value);
        }

        private int outputFormatIndex = -1;
        [BsonIgnore]
        public int OutputFormatIndex
        {
            get => outputFormatIndex;
            set
            {
                SetProperty(ref outputFormatIndex, value);

                //OutputFormat = MediaFormat.SupportedFormats.ElementAtOrDefault(value);
            }
        }
        #endregion

        #region Commands
        [BsonIgnore]
        public ICommand ClearVideoCodecCommand => new RelayCommand(() => VideoCodecIndex = -1);

        [BsonIgnore]
        public ICommand ClearAudioCodecCommand => new RelayCommand(() => AudioCodecIndex = -1);

        [BsonIgnore]
        public ICommand ClearOutputFormatCommand => new RelayCommand(() => OutputFormatIndex = -1);
        #endregion

        #region Overrides and implementations
        public override string? ToString()
        {
            return $"{Category}, {Name}";
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(name);
            hash.Add(category);
            hash.Add(videoBitrate);
            hash.Add(audioBitrate);
            hash.Add(targetWidth);
            hash.Add(targetHeight);
            hash.Add(fps);
            hash.Add(useTargetSize);
            hash.Add(targetSize);
            hash.Add(videoCodec);
            hash.Add(audioCodec);
            hash.Add(outputFormat);
            return hash.ToHashCode();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ClipPresetViewModel);
        }

        public bool Equals(ClipPresetViewModel? other)
        {
            return other is not null &&
                   name == other.name &&
                   category == other.category &&
                   videoBitrate == other.videoBitrate &&
                   audioBitrate == other.audioBitrate &&
                   targetWidth == other.targetWidth &&
                   targetHeight == other.targetHeight &&
                   fps == other.fps &&
                   useTargetSize == other.useTargetSize &&
                   targetSize == other.targetSize &&
                   videoCodec == other.videoCodec &&
                   audioCodec == other.audioCodec &&
                   outputFormat == other.outputFormat;
        }

        public void OnDeserialized()
        {
            LoadComboBoxes();
        }

        private void LoadComboBoxes()
        {
            if (VideoCodec != null)
                VideoCodecIndex = Types.VideoCodec.SupportedCodecs.IndexOf(VideoCodec);

            if (AudioCodec != null)
                AudioCodecIndex = Types.AudioCodec.SupportedCodecs.IndexOf(AudioCodec);

            // if (OutputFormat != null)
            //     OutputFormatIndex = MediaFormat.SupportedFormats.IndexOf(OutputFormat);
        }
        #endregion
    }
}
