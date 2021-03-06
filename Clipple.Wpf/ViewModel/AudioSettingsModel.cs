using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class AudioSettingsModel : ObservableObject
    {
        public AudioSettingsModel(int trackID, string trackName)
        {
            this.trackID   = trackID;
            this.trackName = trackName;
        }

        #region Properties
        private int trackID;
        public int TrackID
        {
            get => trackID;
            set => SetProperty(ref trackID, value);
        }

        private string trackName;
        public string TrackName
        {
            get => trackName;
            set => SetProperty(ref trackName, value);
        }

        private bool isEnabled = true;
        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        private bool convertMono = false;
        public bool ConvertMono
        {
            get => convertMono;
            set => SetProperty(ref convertMono, value);
        }

        private int volume = 100;
        public int Volume
        {
            get => volume;
            set => SetProperty(ref volume, Math.Max(0, Math.Min(100, value)));
        }

        [JsonIgnore]
        public double VolumeDecimal => volume / 100.0;
        #endregion
    }
}
