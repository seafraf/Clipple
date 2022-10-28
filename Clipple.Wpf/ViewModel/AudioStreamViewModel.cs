using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace Clipple.ViewModel
{
    public class AudioStreamViewModel : ObservableObject
    {
        public AudioStreamViewModel(string name, int streamIndex, int audioStreamIndex)
        {
            Name             = name;
            StreamIndex      = streamIndex;
            AudioStreamIndex = audioStreamIndex;

            OpenCommand = new RelayCommand(() => IsOpen = !IsOpen);
        }

        #region Properties
        /// <summary>
        /// The name of this stream, this is only provided in some containers.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Stream index.  This is the index of this audio stream in a list of all streams.
        /// </summary>
        public int StreamIndex { get; private set; }

        /// <summary>
        /// The index of this stream in the list of audio streams.  Note this is not the same as a stream index which would
        /// be the index in the list of all streams.
        /// </summary>
        public int AudioStreamIndex { get; private set; }

        /// <summary>
        /// Volume for this audio stream, 0 - 100
        /// </summary>
        private int volume = 100;
        public int Volume
        {
            get => volume;
            set => SetProperty(ref volume, value);
        }

        /// <summary>
        /// Whether or not this audio stream is muted
        /// </summary>
        private bool isMuted = false;
        public bool IsMuted
        {
            get => isMuted;
            set => SetProperty(ref isMuted, value);
        }

        /// <summary>
        /// Convert this stream to a mono-channel stream?
        /// </summary>
        private bool mono = false;
        public bool IsMono
        {
            get => mono;
            set => SetProperty(ref mono, value);
        }

        /// <summary>
        /// Whether or not this audio stream is enabled.  When disabled, the audio stream
        /// will be muted and will not show a waveform in the timeline.
        /// </summary>
        private bool enabled = true;
        public bool IsEnabled
        {
            get => enabled;
            set => SetProperty(ref enabled, value);
        }

        /// <summary>
        /// Whether or not the settings for this audio stream is open
        /// </summary>
        private bool open = false;
        [JsonIgnore]
        public bool IsOpen
        {
            get => open;
            set => SetProperty(ref open, value);
        }

        /// <summary>
        /// An image representing the waveform for this stream
        /// </summary>
        private ImageSource? waveform;
        [JsonIgnore]
        public ImageSource? Waveform
        {
            get => waveform;
            set => SetProperty(ref waveform, value);
        }

        /// <summary>
        /// The volume of this audio stream that should be passed to MPV's audio filter.  Note that this will result in a zero-volume when 
        /// the stream is muted or disabled
        /// </summary>
        public string VolumeString
        {
            get
            {
                if (IsMuted || !IsEnabled)
                    return "0.00";

                return (Volume / 100.0).ToString("0.00", CultureInfo.InvariantCulture);
            }
        }
        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand OpenCommand { get; set; }
        #endregion
    }
}
