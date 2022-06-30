using Microsoft.Toolkit.Mvvm.ComponentModel;
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
    public class OutputFormatViewModel :  ObservableObject, IEquatable<OutputFormatViewModel>
    {
        public OutputFormatViewModel(string name, string extension, string displayName, bool supportsAudio = true, bool supportsVideo = true)
        {
            this.name           = name;
            this.extension      = extension;
            this.displayName    = displayName;
            this.supportsAudio  = supportsAudio;
            this.supportsVideo  = supportsVideo;
        }

        /// <summary>
        /// For deserialization only
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public OutputFormatViewModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        static OutputFormatViewModel()
        {
            var cvs = CollectionViewSource.GetDefaultView(SupportedFormats);
            cvs.GroupDescriptions.Clear();
            cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Category)));
        }

        #region Properties
        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string extension;
        public string Extension
        {
            get => extension;
            set => SetProperty(ref extension, value);
        }

        private string displayName;
        public string DisplayName
        {
            get => displayName;
            set => SetProperty(ref displayName, value);
        }

        private bool supportsAudio;
        public bool SupportsAudio
        {
            get => supportsAudio;
            set => SetProperty(ref supportsAudio, value);
        }

        private bool supportsVideo;
        public bool SupportsVideo
        {
            get => supportsVideo;
            set => SetProperty(ref supportsVideo, value);
        }

        [JsonIgnore]
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

        /// <summary>
        /// List of supported output formats, FFmpeg supports many more but these are the most popular + tested to work
        /// </summary>
        public static ObservableCollection<OutputFormatViewModel> SupportedFormats { get; } = new()
        {
            // Audio only
            new OutputFormatViewModel("wav", ".wav", "Waveform Audio", true, false),
            new OutputFormatViewModel("adts", ".aac", "Advanced Audio Coding", true, false),
            new OutputFormatViewModel("mp3", ".mp3", "MPEG audio layer 3", true, false),

            // Video and audio
            new OutputFormatViewModel("webm", ".webm", "WebM"),
            new OutputFormatViewModel("avi", ".avi", "Audio Video Interleaved"),
            new OutputFormatViewModel("mov", ".mov", "QuickTime"),
            new OutputFormatViewModel("mp4", ".mp4", "MPEG-4 Part 14"),
            new OutputFormatViewModel("matroska", ".mkv", "Matroska"),
        };
        #endregion

        #region Methods
        public override string? ToString()
        {
            return $"{DisplayName} ({Extension})";
        }

        public override bool Equals(object? obj)
        {
            return obj is OutputFormatViewModel format &&
                   Name == format.Name &&
                   Extension == format.Extension &&
                   DisplayName == format.DisplayName &&
                   SupportsAudio == format.SupportsAudio &&
                   SupportsVideo == format.SupportsVideo &&
                   Category == format.Category;
        }

        public bool Equals(OutputFormatViewModel? other)
        {
            return Equals((object?)other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Extension, DisplayName, SupportsAudio, SupportsVideo, Category);
        }

        public static OutputFormatViewModel? GetByName(string name)
        {
            return SupportedFormats.Where(x => x.Name == name).FirstOrDefault();
        }
        #endregion
    }
}
