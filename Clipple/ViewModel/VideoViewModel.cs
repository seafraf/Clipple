using Clipple.Util;
using Clipple.Util.ISOBMFF;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class VideoViewModel : ObservableObject, IJsonOnDeserialized
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// This constructor only exists for the deserializer to call it, the OnDeserialized method will initiate 
        /// all of the missing properties and fields
        /// </summary>
        public VideoViewModel()
        {
            Clips.CollectionChanged += (s, e) => App.ViewModel.NotifyClipsChanged();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public VideoViewModel(string filePath) : this()
        {
            FilePath = filePath;
            InitializeFromFilePath();
        }

        #region Methods
        private void InitializeFromFilePath()
        {
            fileInfo = new FileInfo(FilePath);

            // Try and parse the container format to extract track names
            var parser = new SimpleParser(FilePath);
            parser.Parse();

            trackNames = parser.Tracks.Select(x => x.Name).ToArray();
        }

        public void OnDeserialized()
        {
            InitializeFromFilePath();
        }
        #endregion

        #region Properties
        private FileInfo fileInfo;
        [JsonIgnore]
        public FileInfo FileInfo
        {
            get => fileInfo;
            set
            {
                SetProperty(ref fileInfo, value);
                OnPropertyChanged(nameof(FileSize));
            }
        }

        private ObservableCollection<ClipViewModel> clips = new ();
        public ObservableCollection<ClipViewModel> Clips
        {
            get => clips;
            set => SetProperty(ref clips, value);
        }

        private bool delete = new();
        public bool Delete
        {
            get => delete;
            set => SetProperty(ref delete, value);
        }

        private string?[] trackNames;
        [JsonIgnore]
        public string?[] TrackNames
        {
            get => trackNames;
            set => SetProperty(ref trackNames, value);
        }

        [JsonIgnore]
        public string FileSize => Formatting.ByteCountToString(FileInfo.Length);

        public string FilePath { get; set; }
        #endregion 
    }
}
