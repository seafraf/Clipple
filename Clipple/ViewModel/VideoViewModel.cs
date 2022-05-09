using Clipple.Util;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class VideoViewModel : ObservableObject
    {
        public VideoViewModel(FileInfo fileInfo, string?[] trackNames)
        {
            this.fileInfo   = fileInfo;
            this.trackNames = trackNames ?? Array.Empty<string>();
        }

        #region Properties
        private FileInfo fileInfo;
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
        public string?[] TrackNames
        {
            get => trackNames;
            set => SetProperty(ref trackNames, value);
        }

        public string FileSize => Formatting.ByteCountToString(FileInfo.Length);
        #endregion 
    }
}
