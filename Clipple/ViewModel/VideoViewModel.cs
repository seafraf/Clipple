using Clipple.MediaProcessing;
using Clipple.Util;
using Clipple.Util.ISOBMFF;
using FFmpeg.AutoGen;
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

        /// <summary>
        /// This constructor only exists for the deserializer to call it, the OnDeserialized method will initiate 
        /// all of the missing properties and fields
        /// </summary>
#pragma warning disable CS8618
        public VideoViewModel()
#pragma warning restore CS8618

        {
            Clips.CollectionChanged += (s, e) => App.ViewModel.NotifyClipsChanged();
        }

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

            // Use ffmpeg to try and determine FPS and video resolution
            unsafe
            {
                AVFormatContext* formatContext = null;
                try
                {
                    formatContext = FE.Null(ffmpeg.avformat_alloc_context());
                    var input = FE.Code(ffmpeg.avformat_open_input(&formatContext, fileInfo.FullName, null, null));

                    // Load stream information
                    FE.Code(ffmpeg.avformat_find_stream_info(formatContext, null));

                    // Try to find the best video stream
                    var streamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
                    FE.Code(streamIndex);

                    var stream = formatContext->streams[streamIndex];

                    VideoFPS    = (int)Math.Round(ffmpeg.av_q2d(ffmpeg.av_guess_frame_rate(formatContext, stream, null)));
                    VideoWidth  = stream->codecpar->width;
                    VideoHeight = stream->codecpar->height;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (formatContext != null)
                        ffmpeg.avformat_close_input(&formatContext);
                }
            }
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

        private int videoWidth = -1;
        public int VideoWidth
        {
            get => videoWidth;
            set => SetProperty(ref videoWidth, value);
        }

        private int videoHeight = -1;
        public int VideoHeight
        {
            get => videoHeight;
            set => SetProperty(ref videoHeight, value);
        }

        private int videoFPS = -1;
        public int VideoFPS
        {
            get => videoFPS;
            set => SetProperty(ref videoFPS, value);
        }

        private bool delete = false;
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
