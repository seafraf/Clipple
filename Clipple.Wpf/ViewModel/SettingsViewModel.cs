using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Clipple.ViewModel
{
    public class Settings : ObservableObject
    {
        #region Methods
        private void ChangeTheme(bool dark, string colourName)
        {
            //if (App.Window != null)
            //    ThemeManager.Current.ChangeTheme(App.Window, ThemeKey);      
        }
        #endregion

        #region Properties
        private bool createOutputFolder = false;
        public bool CreateOutputFolder
        {
            get => createOutputFolder;
            set => SetProperty(ref createOutputFolder, value);
        }

        private string defaultOutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public string DefaultOutputFolder
        {
            get => defaultOutputFolder;
            set => SetProperty(ref defaultOutputFolder, value);
        }

        private bool ingestAutomatically = false;
        public bool IngestAutomatically
        {
            get => ingestAutomatically;
            set => SetProperty(ref ingestAutomatically, value);
        }

        private string ingestFolder = "";
        public string IngestFolder
        {
            get => ingestFolder;
            set => SetProperty(ref ingestFolder, value);
        }

        private bool defaultDeleteVideos = false;
        public bool DefaultDeleteVideos
        {
            get => defaultDeleteVideos;
            set => SetProperty(ref defaultDeleteVideos, value);
        }

        private bool defaultRemoveClips = false;
        public bool DefaultRemoveClips
        {
            get => defaultRemoveClips;
            set => SetProperty(ref defaultRemoveClips, value);
        }

        private bool startProcessingAutomatically = true;
        public bool StartProcessingAutomatically
        {
            get => startProcessingAutomatically;
            set => SetProperty(ref startProcessingAutomatically, value);
        }

        private bool defaultMicrophoneMono = true;
        public bool DefaultMicrophoneMono
        {
            get => defaultMicrophoneMono;
            set => SetProperty(ref defaultMicrophoneMono, value);
        }

        private bool autoSave = true;
        public bool AutoSave
        {
            get => autoSave;
            set => SetProperty(ref autoSave, value);
        }

        private bool saveOnExit = true;
        public bool SaveOnExit
        {
            get => saveOnExit;
            set => SetProperty(ref saveOnExit, value);
        }

        //private HotKey controlHotKey = new(Key.Space);
        //public HotKey ControlHotKey
        //{
        //    get => controlHotKey;
        //    set => SetProperty(ref controlHotKey, value);
        //}

        //private HotKey nextFrameHotKey = new(Key.Right);
        //public HotKey NextFrameHotKey
        //{
        //    get => nextFrameHotKey;
        //    set => SetProperty(ref nextFrameHotKey, value);
        //}

        //private HotKey previousFrameHotKey = new(Key.Left);
        //public HotKey PreviousFrameHotKey
        //{
        //    get => previousFrameHotKey;
        //    set => SetProperty(ref previousFrameHotKey, value);
        //}

        //private HotKey toggleMuteHotKey = new(Key.M);
        //public HotKey ToggleMuteHotKey
        //{
        //    get => toggleMuteHotKey;
        //    set => SetProperty(ref toggleMuteHotKey, value);
        //}

        //private HotKey volumeUpHotKey = new(Key.Up);
        //public HotKey VolumeUpHotKey
        //{
        //    get => volumeUpHotKey;
        //    set => SetProperty(ref volumeUpHotKey, value);
        //}

        //private HotKey volumeDownHotKey = new(Key.Down);
        //public HotKey VolumeDownHotKey
        //{
        //    get => volumeDownHotKey;
        //    set => SetProperty(ref volumeDownHotKey, value);
        //}

        //private HotKey nextVideoHotKey = new(Key.Right, ModifierKeys.Control);
        //public HotKey NextVideoHotKey
        //{
        //    get => nextVideoHotKey;
        //    set => SetProperty(ref nextVideoHotKey, value);
        //}

        //private HotKey previousVideoHotKey = new(Key.Left, ModifierKeys.Control);
        //public HotKey PreviousVideoHotKey
        //{
        //    get => previousVideoHotKey;
        //    set => SetProperty(ref previousVideoHotKey, value);
        //}

        //private HotKey createClipHotKey = new(Key.C, ModifierKeys.Shift);
        //public HotKey CreateClipHotKey
        //{
        //    get => createClipHotKey;
        //    set => SetProperty(ref createClipHotKey, value);
        //}

        //private HotKey seekStartHotKey = new(Key.Left, ModifierKeys.Shift);
        //public HotKey SeekStartHotKey
        //{
        //    get => seekStartHotKey;
        //    set => SetProperty(ref seekStartHotKey, value);
        //}

        //private HotKey seekEndHotKey = new(Key.Right, ModifierKeys.Shift);
        //public HotKey SeekEndHotKey
        //{
        //    get => seekEndHotKey;
        //    set => SetProperty(ref seekEndHotKey, value);
        //}

        //private HotKey saveHotKey = new(Key.S, ModifierKeys.Control);
        //public HotKey SaveHotKey
        //{
        //    get => saveHotKey;
        //    set => SetProperty(ref saveHotKey, value);
        //}

        private bool themeIsDark = true;
        public bool ThemeIsDark
        {
            get => themeIsDark;
            set
            {
                SetProperty(ref themeIsDark, value);
                ChangeTheme(value, themeColour);
            }
        }

        private string themeColour = "Steel";
        public string ThemeColour
        {
            get => themeColour;
            set
            {
                SetProperty(ref themeColour, value);
                ChangeTheme(themeIsDark, value);
            }
        }

        private int maxConcurrentJobs = 1;
        public int MaxConcurrentJobs
        {
            get => maxConcurrentJobs;
            set => SetProperty(ref maxConcurrentJobs, value);
        }

        //public ReadOnlyObservableCollection<string> AvailableColours => ThemeManager.Current.ColorSchemes;

        [BsonIgnore]
        public string ThemeKey => $"{(ThemeIsDark ? "Dark" : "Light")}.{ThemeColour}";
        #endregion
    }
}
