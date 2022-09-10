using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Windows.Media;
using FFmpeg.AutoGen;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace Clipple.ViewModel
{
    public partial class VideoViewModel
    {
        private class AudioStreamState
        {
            public unsafe AudioStreamState(AVStream* stream, AVCodecContext* context, bool isPlanar, int bytesPerSample, int samplesPerPoint)
            {
                Stream              = stream;
                Context             = context;
                IsPlanar            = isPlanar;
                BytesPerSample      = bytesPerSample;
                SamplesPerPoint     = samplesPerPoint;
            }

            public unsafe AVStream* Stream { get; private set; }
            public unsafe AVCodecContext* Context { get; private set; }
            public bool IsPlanar { get; private set; }
            public PointCollection Waveform { get; } = new();
            public int BytesPerSample { get; private set; }
            public int SamplesPerPoint { get; private set; }
            public int CurrentSampleCount { get; set; } = 0;
            public float MinSample { get; set; } = float.MaxValue;
            public float MaxSample { get; set; } = float.MinValue;
        }

        // Number of points in the waveform (at a maximum)
        private const int WaveformResolution = 4096;

        #region Methods
        protected unsafe void ProcessAudio()
        {
            AVFormatContext* formatContext = null;
            AVFrame* inputFrame = null;
            AVPacket* packet = null;
            var stateDictionary = new Dictionary<int, AudioStreamState>();

            try
            {
                formatContext = CheckNull(ffmpeg.avformat_alloc_context(),
                    "ffmpeg couldn't allocate context");

                CheckCode(ffmpeg.avformat_open_input(&formatContext, fileInfo.FullName, null, null),
                    $"ffmpeg couldn't open {fileInfo.FullName}");

                // Load stream information
                CheckCode(ffmpeg.avformat_find_stream_info(formatContext, null),
                    $"couldn't load stream info");


                for (var i = 0; i < formatContext->nb_streams; i++)
                {
                    var stream = formatContext->streams[i];
                    if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                    {
                        var codec = CheckNull(ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id),
                            "couldn't find decoder codec");

                        var context = CheckNull(ffmpeg.avcodec_alloc_context3(codec),
                            "couldn't allocate decoder codec context");

                        CheckCode(ffmpeg.avcodec_parameters_to_context(context, stream->codecpar),
                            "couldn't copy codec params from stream");

                        // Skip audio streams with sample formats that we don't understand
                        if (!IsSupportedSampleFormat(context->sample_fmt))
                        {
                            ffmpeg.avcodec_free_context(&context);
                            continue;
                        }

                        CheckCode(ffmpeg.avcodec_open2(context, codec, null),
                            "couldn#t open codec");

                        // Add a state for this audio stream
                        stateDictionary.Add(i, new AudioStreamState(stream,
                            context, ffmpeg.av_sample_fmt_is_planar(context->sample_fmt) == 1,
                            ffmpeg.av_get_bytes_per_sample(context->sample_fmt),
                            (int)Math.Floor((context->sample_rate * videoDuration.TotalSeconds) / WaveformResolution)));
                    }
                }

                inputFrame = CheckNull(ffmpeg.av_frame_alloc(), "alloc failure");
                packet = CheckNull(ffmpeg.av_packet_alloc(), "alloc failure");

                while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
                {
                    var state = stateDictionary.GetValueOrDefault(packet->stream_index);
                    if (state != null)
                        HandleAudioPacket(formatContext, state, inputFrame, packet);
                }

            }
            catch (Exception e)
            {
                App.Current.Dispatcher.Invoke(() => App.Logger.LogError($"Failed to load metadata for {FileInfo.FullName}", e));
            }
            finally
            {
                if (formatContext != null)
                    ffmpeg.avformat_close_input(&formatContext);

                foreach (var state in stateDictionary.Values)
                {
                    // why
                    var context = state.Context;
                    ffmpeg.avcodec_free_context(&context);
                }


                if (inputFrame != null)
                    ffmpeg.av_frame_free(&inputFrame);

                if (packet != null)
                    ffmpeg.av_packet_free(&packet);
            }

            // move waveforms from state to view model
            foreach (var state in stateDictionary.Values)
            {
                state.Waveform.Freeze();
                AudioStreamWaveforms.Add(state.Waveform);
            }


            // bmp is the original BitmapImage
            var target = new RenderTargetBitmap(8192, 64, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();

            var random = new Random();
            using (var r = visual.RenderOpen())
            {
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(0, 0), false, false);
                    foreach (var point in stateDictionary[2].Waveform)
                    {
                        var rand = random.Next(32);
                        ctx.LineTo(new Point(point.X, 32 + (point.Y * 32)), true, true);
                        //rr.DrawLine(new Pen(Brushes.Red, 1), new Point(i, 16 + 32 - rand), new Point(i, 16 + rand));
                    }
                }
                r.DrawGeometry(Brushes.Red, new Pen(Brushes.Red, 1), geometry);
            }

            target.Render(visual);
            target.Freeze();
        }

        private unsafe void HandleAudioPacket(AVFormatContext* formatContext, AudioStreamState state, AVFrame* inputFrame, AVPacket* packet)
        {
            int code = ffmpeg.avcodec_send_packet(state.Context, packet);
            while (code >= 0)
            {
                code = ffmpeg.avcodec_receive_frame(state.Context, inputFrame);
                if (code == ffmpeg.AVERROR(ffmpeg.EAGAIN) || code == ffmpeg.AVERROR_EOF)
                {
                    break;
                }
                else if (code < 0)
                    CheckCode(code, "couldn't decode frame");

                if (state.IsPlanar)
                {
                    for (var i = 0; i < inputFrame->nb_samples; i++)
                    {
                        for (var ch = 0; ch < state.Context->channels; ch++)
                        {
                            var sample = Math.Clamp(*(float*)(inputFrame->extended_data[ch] + i * state.BytesPerSample), -1.0f, 1.0f);

                            if (state.CurrentSampleCount == state.SamplesPerPoint)
                            {
                                state.CurrentSampleCount = 0;
                                state.Waveform.Add(new System.Windows.Point(state.Waveform.Count, state.MinSample));
                                state.Waveform.Add(new System.Windows.Point(state.Waveform.Count, state.MaxSample));

                                state.MinSample = float.MaxValue;
                                state.MaxSample = float.MinValue;
                            }

                            state.MinSample = Math.Min(state.MinSample, sample);
                            state.MaxSample = Math.Max(state.MaxSample, sample);
                            state.CurrentSampleCount++;
                        }
                    }
                }
                //else
                //{
                //    for (var i = 0; i < inputFrame->nb_samples * codecContext->channels; i++)
                //    {
                //        currentSampleAverage[id] = Math.Clamp(*(float*)(inputFrame->extended_data[i * bytesPerSample]), -1.0f, 1.0f);
                //        currentSampleCount[id]++;

                //        if (currentSampleCount[id] >= samplesPerDot)
                //        {
                //            averageSamples[id][sampleIndex[id]] = currentSampleAverage[id] / (float)samplesPerDot;

                //            currentSampleCount[id] = 0;
                //            currentSampleAverage[id] = 0.0f;
                //            sampleIndex[id]++;
                //        }
                //    }
                //}
            }
        }

        private bool IsSupportedSampleFormat(AVSampleFormat format)
        {
            switch (format)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        private ObservableCollection<PointCollection> audioStreamWaveforms = new();
        [JsonIgnore]
        public ObservableCollection<PointCollection> AudioStreamWaveforms
        {
            get => audioStreamWaveforms;
            set => SetProperty(ref audioStreamWaveforms, value);
        }
        #endregion
    }
}
