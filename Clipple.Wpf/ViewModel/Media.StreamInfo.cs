using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Windows;
using Clipple.Types;
using Clipple.Util.ISOBMFF;
using FFmpeg.AutoGen;
using LiteDB;

namespace Clipple.ViewModel;

public partial class Media
{
    #region Methods

    protected unsafe bool GetMediaInfo()
    {
        AVFormatContext* formatContext = null;
        AVCodecContext*  codecContext  = null;

        // Attempt to read names of audio tracks
        List<Track> audioTracks = new();
        try
        {
            var parser = new SimpleParser(FilePath);
            parser.Parse();

            audioTracks = parser.Tracks;
        }
        catch (Exception)
        {
            // ignored
        }

        try
        {
            formatContext = CheckNull(ffmpeg.avformat_alloc_context(),
                "ffmpeg couldn't allocate context");
 
            CheckCode(ffmpeg.avformat_open_input(&formatContext, fileInfo.FullName, null, null),
                $"ffmpeg couldn't open {fileInfo.FullName}");

            // Load stream information
            CheckCode(ffmpeg.avformat_find_stream_info(formatContext, null),
                "couldn't load stream info");

            var bestVideoStreamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
            var bestAudioStreamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);

            HasVideo = bestVideoStreamIndex != ffmpeg.AVERROR_STREAM_NOT_FOUND;
            HasAudio = bestAudioStreamIndex != ffmpeg.AVERROR_STREAM_NOT_FOUND;

            if (!HasVideo && !HasAudio)
                throw new FormatException("Media doesn't contain audio or video");

            string[]? formatNames = Marshal.PtrToStringAnsi((nint)formatContext->iformat->name)?.Split(",");
            if (formatNames == null || !formatNames.Any(x => App.ViewModel.ContainerFormatCollection.SupportedFormatNames.Contains(x)))
                throw new NotSupportedException($"{fileInfo.FullName} is not in a supported format");

            var audioStreams = new List<AudioStreamSettings>();
            for (var i = 0; i < formatContext->nb_streams; i++)
            {
                var stream = formatContext->streams[i];

                // Duration should come from the longest stream
                Duration = TimeSpan.FromSeconds(Math.Max(stream->duration * ffmpeg.av_q2d(stream->time_base), Duration.TotalSeconds));

                if (i == bestVideoStreamIndex)
                {
                    VideoFps      = (int)Math.Round(ffmpeg.av_q2d(ffmpeg.av_guess_frame_rate(formatContext, stream, null)));
                    VideoWidth    = stream->codecpar->width;
                    VideoHeight   = stream->codecpar->height;
                    VideoCodecID  = stream->codecpar->codec_id; 
                }
                else if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    audioStreams.Add(new AudioStreamSettings(audioTracks.ElementAtOrDefault(i)?.Name ?? $"Stream {audioStreams.Count}", i, audioStreams.Count, stream->codecpar->codec_id));
                }
            }

            AudioStreams = audioStreams.ToArray();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (formatContext != null)
                ffmpeg.avformat_close_input(&formatContext);

            if (codecContext != null)
                ffmpeg.avcodec_free_context(&codecContext);
        }

        return true;
    }

    protected unsafe int CheckCode(int code, string error)
    {
        if (code < 0)
        {
            var bufferSize = 1024;
            var buffer     = stackalloc byte[bufferSize];
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

    #region Members
    private TimeSpan duration = TimeSpan.Zero;
    private bool hasAudio;
    private bool hasVideo;
    #endregion

    #region Properties

    /// <summary>
    ///     Media duration
    /// </summary>
    public TimeSpan Duration
    {
        get => duration;
        set => SetProperty(ref duration, value);
    }

    /// <summary>
    ///     Does the media contain audio?
    /// </summary>
    public bool HasAudio
    {
        get => hasAudio;
        set => SetProperty(ref hasAudio, value);
    }

    /// <summary>
    ///     Does the media contain video?
    /// </summary>
    public bool HasVideo
    {
        get => hasVideo;
        set => SetProperty(ref hasVideo, value);
    }

    /// <summary>
    ///     True if all fields populated by GetStreamInfo have been populated.
    ///     This can be false between Clipple versions that used older video view models, this will also always be
    ///     true when a video is imported for the first time
    /// </summary>
    [BsonIgnore]
    public bool HasMediaInfo => VideoWidth != -1 && VideoHeight != -1 && VideoFps != -1 && Duration != TimeSpan.Zero && AudioStreams != null;

    #endregion
}