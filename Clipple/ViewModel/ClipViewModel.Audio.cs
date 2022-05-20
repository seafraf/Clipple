using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public partial class ClipViewModel
    {
        /// <summary>
        /// Selected audio codec, null will use the source audio codec
        /// </summary>
        private string? audioCodec = null;
        public string? AudioCodec
        {
            get => audioCodec;
            set => SetProperty(ref audioCodec, value);
        }

        /// <summary>
        /// Whether or not all audio streams should be merged into one audio stream, this is forced on 
        /// for output formats that only include audio
        /// </summary>
        private bool mergeAudio = true;
        public bool MergeAudio
        {
            get => mergeAudio || !OutputFormat.SupportsVideo;
            set
            {
                SetProperty(ref mergeAudio, value);

                if (UseTargetSize)
                    OnPropertyChanged(nameof(VideoBitrate));
            }
        }

        /// <summary>
        /// Settings for each input audio stream
        /// </summary>
        private ObservableCollection<AudioSettingsModel> audioSettings = new();
        public ObservableCollection<AudioSettingsModel> AudioSettings
        {
            get => audioSettings;
            set
            {
                foreach (var audioSetting in value)
                {
                    audioSetting.PropertyChanged -= OnAudioSettingPropertyChanged;
                    audioSetting.PropertyChanged += OnAudioSettingPropertyChanged;
                }

                SetProperty(ref audioSettings, value);
            }
        }

        /// <summary>
        /// Audio bitrate, kilobits/second.  
        /// If UseTargetSize is true, this value will effect video bitrate
        /// </summary>
        private long audioBitrate = 320;
        public long AudioBitrate
        {
            get => audioBitrate;
            set
            {
                SetProperty(ref audioBitrate, value);

                if (UseTargetSize)
                    OnPropertyChanged(nameof(VideoBitrate));
            }
        }

        #region Methods
        private void OnAudioSettingPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!UseTargetSize)
                return;

            // If UseTargetSize is true, the number of enabled audio streams changes the maximum video bitrate
            if (e.PropertyName == nameof(AudioSettingsModel.IsEnabled))
                OnPropertyChanged(nameof(VideoBitrate));
        }
        #endregion
    }
}
