using Clipple.View;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace Clipple.ViewModel
{
    public partial class ClipViewModel
    {
        #region Properties
        private bool presetMenuOpen;
        public bool PresetMenuOpen
        {
            get => presetMenuOpen;
            set => SetProperty(ref presetMenuOpen, value);
        }


        /// <summary>
        /// Current transcoding preset
        /// </summary>
        private ClipPresetViewModel? transcodingPreset;
        [JsonIgnore]
        public ClipPresetViewModel? TranscodingPreset
        {
            get => transcodingPreset;
            set
            {
                if (value != null)
                {
                    VideoBitrate      = value.VideoBitrate ?? videoBitrate;
                    TargetWidth       = value.TargetWidth ?? targetWidth;
                    TargetHeight      = value.TargetHeight ?? targetHeight;
                    TargetFPS         = value.FPS ?? targetFPS;
                    VideoCodec        = value.VideoCodec ?? videoCodec;
                    AudioCodec        = value.AudioCodec ?? audioCodec;
                    AudioBitrate      = value.AudioBitrate ?? audioBitrate;
                    UseTargetSize     = value.UseTargetSize;
                    OutputTargetSize  = value.TargetSize ?? outputTargetSize;

                    if (value.OutputFormat != null)
                    {
                        OutputFormat = value.OutputFormat;
                        OutputFormatIndex = OutputFormatViewModel.SupportedFormats.IndexOf(value.OutputFormat);
                    }
                }

                SetProperty(ref transcodingPreset, value);
                OnPropertyChanged(nameof(TranscodingPreset));
            }
        }

        /// <summary>
        /// Index of transcoding preset, for serialization
        /// </summary>
        private int transcodingPresetIndex = -1;
        public int TranscodingPresetIndex
        {
            get => transcodingPresetIndex;
            set => SetProperty(ref transcodingPresetIndex, value);
        }

        #endregion

        #region Commands
        public ICommand OpenPresetManagerCommand => new RelayCommand(OpenPresetManager);
        #endregion

        #region Methods
        private void OpenPresetManager()
        {
            var manager = new ClipPresetManager()
            {
                DataContext = new ClipPresetManagerViewModel(this)
            };

            manager.ShowDialog();
        }
        #endregion
    }
}
