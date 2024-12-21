using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Clipple.Util;
using Clipple.ViewModel;
using FFmpeg.AutoGen;

namespace Clipple.View;

public partial class Waveform : UserControl
{
    public Waveform()
    {
        InitializeComponent();
    }
    
    #region Members
    /// <summary>
    /// Width of the Waveform bitmap.  This should be a large number.  If the timeline width in pixels is 2000 and the
    /// waveform resolution is 4000 pixels, the total zoom will only be 2x.  A number too large results in too much
    /// memory usage and too time spent generating the bitmap.
    /// </summary>
    private const int ResolutionX = 2160 * 32; //  32 times zoom on a 4K monitor

    /// <summary>
    /// The resolution width as a double, this is required for some XAML bindings
    /// </summary>
    public const double DoubleResolutionX = ResolutionX;

    /// <summary>
    /// The height of a Waveform, this number should be uneven to allow a 1px wide line down the middle of the waveform
    /// </summary>
    private const int ResolutionY = 49;
    
    /// <summary>
    /// The resolution height as a double, this is required for some XAML bindings
    /// </summary>
    public const double DoubleResolutionY = ResolutionY;
    
    /// <summary>
    /// Whether or not the waveform is currently in the generation process
    /// </summary>
    private bool isWaveformGenerating;

    /// <summary>
    /// The task that is generating the waveform
    /// </summary>
    private CancellationTokenSource cancellationTokenSource = new();
    #endregion

    #region Dependency Properties
    public static readonly DependencyProperty AudioStreamIndexProperty =
        DependencyProperty.Register(
            nameof(AudioStreamIndex),
            typeof(Int32),
            typeof(Waveform),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    
    public static readonly DependencyProperty MediaProperty =
        DependencyProperty.Register(
            nameof(Media),
            typeof(Media),
            typeof(Waveform),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    #endregion
    
    #region Properties
    /// <summary>
    /// The index of this specific stream from the media that this waveform should be generated for
    /// </summary>
    public int AudioStreamIndex
    {
        get => (int)GetValue(AudioStreamIndexProperty);
        set => SetValue(AudioStreamIndexProperty, value);
    }
    
    /// <summary>
    /// The selected media that this waveform comes from
    /// </summary>
    public Media Media
    {
        get => (Media)GetValue(MediaProperty);
        set => SetValue(MediaProperty, value);
    }
    #endregion
    
    #region Methods

    private unsafe void ReadFrame(AVFrame* frame)
    {
        
    }

    private unsafe float ReadSample(AVFrame* sampleFrame, int sampleIndex, AVSampleFormat sampleFormat)
    {
        var channelCount = sampleFrame->ch_layout.nb_channels;
        var sampleAvg = 0.0f;

        for (var i = 0; i < channelCount; i++)
        {
            var buf = ffmpeg.av_sample_fmt_is_planar(sampleFormat) == 1 ? sampleFrame->extended_data[i] : sampleFrame->extended_data[0];
            switch (sampleFormat)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                    sampleAvg += buf[sampleIndex] / (float)byte.MaxValue;
                    break;
                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                    sampleAvg += (float)((ushort*)buf)[sampleIndex] / ushort.MaxValue;
                    break;
                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                    sampleAvg += (float)((uint*)buf)[sampleIndex] / uint.MaxValue;
                    break;
                case AVSampleFormat.AV_SAMPLE_FMT_S64:
                case AVSampleFormat.AV_SAMPLE_FMT_S64P:
                    sampleAvg += (float)((ulong*)buf)[sampleIndex] / ulong.MaxValue;
                    break;
                case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                    sampleAvg += Math.Clamp(((float*)buf)[sampleIndex], -1.0f, 1.0f);
                    break;
                case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                    sampleAvg += (float)Math.Clamp(((double*)buf)[sampleIndex], -1.0, 1.0);
                    break;
                case AVSampleFormat.AV_SAMPLE_FMT_NONE:
                case AVSampleFormat.AV_SAMPLE_FMT_NB:
                default:
                    throw new FormatException("unsupported sample format");
            }   
        }

        return Math.Clamp(sampleAvg / channelCount, -1.0f, 1.0f);
    }
    
    private unsafe void DrawSamples(string filePath, int streamIndex, WriteableBitmap bitmap, Color color)
    {
        AVFormatContext* formatContext = null;
        AVCodecContext*  codecContext  = null;
        var frame = FFMPEGHelpers.CheckNull(ffmpeg.av_frame_alloc(), "couldn't allocate frame buffer");
        var packet = FFMPEGHelpers.CheckNull(ffmpeg.av_packet_alloc(), "couldn't packet frame buffer");

        try
        {
            formatContext = FFMPEGHelpers.CheckNull(ffmpeg.avformat_alloc_context(),
                "ffmpeg couldn't allocate context");

            FFMPEGHelpers.CheckCode(ffmpeg.avformat_open_input(&formatContext, filePath, null, null),
                $"ffmpeg couldn't open {filePath}");

            // Load stream information
            FFMPEGHelpers.CheckCode(ffmpeg.avformat_find_stream_info(formatContext, null),
                "couldn't load stream info");

            var stream = formatContext->streams[streamIndex];
            var codec = FFMPEGHelpers.CheckNull(ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id),
                $"couldn't find decoder for {stream->codecpar->codec_id}");

            codecContext = FFMPEGHelpers.CheckNull(ffmpeg.avcodec_alloc_context3(codec), 
                "couldn't allocate codec context");

            FFMPEGHelpers.CheckCode(ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar), 
                "couldn't copy codec params");
            
            FFMPEGHelpers.CheckCode(ffmpeg.avcodec_open2(codecContext, codec, null), 
                $"couldn't open codec {stream->codecpar->codec_id}");

            var totalSampleCount = (long)(codecContext->sample_rate * ((double)formatContext->duration / ffmpeg.AV_TIME_BASE));
            var samplesPerPixel = totalSampleCount / ResolutionX;

            
            var currentSampleCount = 0;
            var partIndex = 0;
            var maxSample = 0.0f;
            var minSample = 0.0f;
            const int waveformMiddle = ResolutionY / 2;
            
            do
            {
                var response = ffmpeg.av_read_frame(formatContext, packet);
                if (response == ffmpeg.AVERROR_EOF)
                    break;

                if (packet->stream_index != streamIndex)
                    continue;
                
                FFMPEGHelpers.CheckCode(response, "bad packet");
                FFMPEGHelpers.CheckCode(ffmpeg.avcodec_send_packet(codecContext, packet),
                    "couldn't send packet to the decoder");
                
                do
                {
                    response = ffmpeg.avcodec_receive_frame(codecContext, frame);
                    if (response == ffmpeg.AVERROR(ffmpeg.EAGAIN) || response == ffmpeg.AVERROR_EOF)
                        break;
                    
                    FFMPEGHelpers.CheckCode(response, "bad frame");

                    // some planar checking todo
                    for (var i = 0; i < frame->nb_samples; i++)
                    {
                        var sample = ReadSample(frame, i, codecContext->sample_fmt);
                        if (!float.IsNaN(sample))
                        {
                            maxSample = Math.Max(maxSample, sample);
                            minSample = Math.Min(minSample, sample);
                        }

                        currentSampleCount++;

                        if (currentSampleCount >= samplesPerPixel)
                        {
                            currentSampleCount = 0;

                            var low = (int)(waveformMiddle * Math.Abs(minSample));
                            var high = (int)(waveformMiddle * Math.Abs(maxSample));
                            if (low != 0 || high != 0)
                                bitmap.DrawLine(partIndex, waveformMiddle - high, partIndex, waveformMiddle + low, color);
                            
                            maxSample = 0.0f;
                            minSample = 0.0f;

                            partIndex++;
                        }
                    }
                } while (true);
            } while (true);

            bitmap.DrawLine(0, waveformMiddle, ResolutionX, waveformMiddle, color);
        }
        finally
        {
            if (formatContext != null)
                ffmpeg.avformat_close_input(&formatContext);

            if (codecContext != null)
                ffmpeg.avcodec_free_context(&codecContext);
            
            ffmpeg.av_frame_free(&frame);
            ffmpeg.av_packet_free(&packet);
        }
    }
    
    private void GenerateWaveform(string filePath, AudioStreamSettings settings)
    {
        isWaveformGenerating = true;

        var bitmap = BitmapFactory.New(ResolutionX, ResolutionY);
        using(bitmap.GetBitmapContext())
        {
            DrawSamples(filePath, settings.StreamIndex, bitmap, settings.Color);
        }
        bitmap.Freeze();
        
        if (!cancellationTokenSource.IsCancellationRequested)
            Application.Current.Dispatcher.Invoke(() => Image.Source = bitmap);

        isWaveformGenerating = false;
    }
    
    private void StartWaveformGeneration()
    {
        if (isWaveformGenerating)
            return;

        var filePath = Media.FilePath;
        var settings = Media.AudioStreams[AudioStreamIndex];
        Task.Run(() => GenerateWaveform(filePath, settings));
    }

    private void AbortWaveformGeneration()
    {
        if (!isWaveformGenerating) 
            return;
        
        // Cancel any processing
        cancellationTokenSource.Cancel();
        
        isWaveformGenerating = false;
        cancellationTokenSource = new();
        Image.Source = null;
    }
    #endregion
    
    #region Events
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // The waveform is usually unloaded when the selected media changes.  But in any case, abort any attempt at 
        // generating a waveform
        AbortWaveformGeneration();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Visibility can change when the active tab is no longer the editor and also when the user deselects this 
        // waveform.  Check if the waveform is no longer selected and abort and waveform generation if so
        if (e.NewValue is false && !Media.AudioStreams[AudioStreamIndex].IsWaveformEnabled)
            AbortWaveformGeneration();
        
        // The call to StartWaveformGeneration won't do anything if an active waveform generation process is ongoing
        if (e is { NewValue: true, OldValue: false })
            StartWaveformGeneration();
    }
    #endregion
}