using Clipple.Types;
using Clipple.Util.ISOBMFF;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Clipple.ViewModel
{
    public partial class VideoViewModel
    {
        #region Methods
        protected unsafe void GetStreamInfo()
        {
            AVFormatContext* formatContext  = null;
            AVCodecContext* codecContext    = null;

            // Attempt to read names of audio tracks
            List<Track> audioTracks = new();
            try
            {
                var parser = new SimpleParser(FilePath);
                parser.Parse();

                audioTracks = parser.Tracks;
            }
            catch (Exception) { }

            try
            {
                formatContext = CheckNull(ffmpeg.avformat_alloc_context(), 
                    "ffmpeg couldn't allocate context");

                CheckCode(ffmpeg.avformat_open_input(&formatContext, fileInfo.FullName, null, null),
                    $"ffmpeg couldn't open {fileInfo.FullName}");

                // Load stream information
                CheckCode(ffmpeg.avformat_find_stream_info(formatContext, null),
                    $"couldn't load stream info");

                // Try to find the best video stream
                var bestVideoStreamIndex = CheckCode(ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0),
                    "couldn't find best stream");

                var audioStreams = new List<AudioStreamViewModel>();
                for (var i = 0; i < formatContext->nb_streams; i++)
                {
                    var stream = formatContext->streams[i];
                    if (i == bestVideoStreamIndex)
                    {
                        VideoFPS        = (int)Math.Round(ffmpeg.av_q2d(ffmpeg.av_guess_frame_rate(formatContext, stream, null)));
                        VideoWidth      = stream->codecpar->width;
                        VideoHeight     = stream->codecpar->height;
                        VideoDuration   = TimeSpan.FromSeconds(stream->duration * ffmpeg.av_q2d(stream->time_base));
                    }
                    else if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                    {
                        audioStreams.Add(new AudioStreamViewModel(audioTracks[i].Name ?? $"Stream {audioStreams.Count}", i, audioStreams.Count));
                    }
                }

                AudioStreams = audioStreams.ToArray();
            }
            catch (Exception e)
            {
                App.Current.Dispatcher.Invoke(() => App.Logger.LogError($"Failed to load metadata for {FileInfo.FullName}", e));
            }
            finally
            {
                if (formatContext != null)
                    ffmpeg.avformat_close_input(&formatContext);

                if (codecContext != null)
                    ffmpeg.avcodec_free_context(&codecContext);
            }
        }

        protected unsafe int CheckCode(int code, string error)
        {
            if (code < 0)
            {
                var bufferSize = 1024;
                var buffer = stackalloc byte[bufferSize];
                ffmpeg.av_strerror(code, buffer, (ulong)bufferSize);

                throw new InvalidOperationException($"{error}: {Marshal.PtrToStringAnsi((IntPtr)buffer)}");
            }

            return code;
        }

        protected unsafe T* CheckNull<T>(T* nullable, string error) where T : unmanaged
        {
            if (nullable == null)
                throw new InvalidOperationException(error);

            return nullable;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Video width in pixels
        /// </summary>
        private int videoWidth = -1;
        public int VideoWidth
        {
            get => videoWidth;
            set => SetProperty(ref videoWidth, value);
        }

        /// <summary>
        /// Video height in pixels
        /// </summary>
        private int videoHeight = -1;
        public int VideoHeight
        {
            get => videoHeight;
            set => SetProperty(ref videoHeight, value);
        }

        /// <summary>
        /// Rounded video FPS
        /// </summary>
        private int videoFPS = -1;
        public int VideoFPS
        {
            get => videoFPS;
            set => SetProperty(ref videoFPS, value);
        }

        /// <summary>
        /// Video duration
        /// </summary>
        private TimeSpan videoDuration = TimeSpan.Zero;
        public TimeSpan VideoDuration
        {
            get => videoDuration;
            set => SetProperty(ref videoDuration, value);
        }

        /// <summary>
        /// True if all fields populated by GetStreamInfo have been populated.
        /// This can be false between Clipple versions that used older video view models, this will also always be
        /// true when a video is imported for the first time
        /// </summary>
        [JsonIgnore]
        public bool HasStreamInfo => VideoWidth != -1 && VideoHeight != -1 && VideoFPS != -1 && VideoDuration != TimeSpan.Zero && AudioStreams == null;
        #endregion
    }
}
