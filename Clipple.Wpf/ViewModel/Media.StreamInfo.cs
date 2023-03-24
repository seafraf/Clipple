using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Clipple.Util.ISOBMFF;
using FFmpeg.AutoGen;
using LiteDB;
using Matroska;
using Matroska.Models;
using NEbml.Core;

namespace Clipple.ViewModel;

public partial class Media
{
    #region Methods
    private List<string?> GetTrackNames()
    {
        // isobmff
        try
        {
            var parser = new SimpleParser(FilePath);
            parser.Parse();

            return parser.Tracks
                .Select(x => x.Name)
                .ToList();
        }
        catch (Exception)
        {
            // ignored
        }

        // ebml
        try
        {
            using var fs   = new FileStream(FilePath, FileMode.Open);
            var       reader = new EbmlReader(fs);
            
            // This serves to skip as much of the file as possible reading only the Tracks element that 
            // we need
            while (reader.ReadNext())
            {
                switch (reader.ElementId.EncodedValue)
                {
                    case 0x18538067: // Segment
                        reader.EnterContainer();
                        break;
                    case 0x1654AE6B: // Tracks
                    {
                        var tracks = MatroskaSerializer.Deserialize<Tracks>(reader);
                        return tracks.TrackEntries.Select(x => x.Name).ToList();
                    }
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
        
        return new();
    }

    private unsafe void GetMediaInfo()
    {
        AVFormatContext* formatContext = null;
        AVCodecContext*  codecContext  = null;

        // Attempt to read names of audio tracks
        var audioTracks = GetTrackNames();

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

            var formatNames = Marshal.PtrToStringAnsi((nint)formatContext->iformat->name)?.Split(",");
            if (formatNames == null || !formatNames.Any(x => App.ContainerFormatCollection.SupportedFormatNames.Contains(x)))
                throw new NotSupportedException($"{fileInfo.FullName} is not in a supported format");

            Duration = TimeSpan.FromSeconds((double)formatContext->duration / ffmpeg.AV_TIME_BASE);

            var audioStreams = new List<AudioStreamSettings>();
            for (var i = 0; i < formatContext->nb_streams; i++)
            {
                var stream = formatContext->streams[i];

                if (i == bestVideoStreamIndex)
                {
                    VideoFps     = (int)Math.Round(ffmpeg.av_q2d(ffmpeg.av_guess_frame_rate(formatContext, stream, null)));
                    VideoWidth   = stream->codecpar->width;
                    VideoHeight  = stream->codecpar->height;
                    VideoCodecID = stream->codecpar->codec_id;
                }
                else if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    audioStreams.Add(new(audioTracks.ElementAtOrDefault(i) ?? $"Stream {audioStreams.Count}", i, audioStreams.Count, stream->codecpar->codec_id));
                }
            }

            AudioStreams = audioStreams.ToArray();
        }
        finally
        {
            if (formatContext != null)
                ffmpeg.avformat_close_input(&formatContext);

            if (codecContext != null)
                ffmpeg.avcodec_free_context(&codecContext);
        }
    }

    private unsafe void CheckCode(int code, string error)
    {
        if (code >= 0)
            return;
        
        const int bufferSize = 1024;
        var       buffer     = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(code, buffer, (ulong)bufferSize);

        throw new InvalidOperationException($"{error}: {Marshal.PtrToStringAnsi((nint)buffer)}");
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
    private bool     hasAudio;
    private bool     hasVideo;

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
    ///     This property is used to control whether or not GetMediaInfo has been called for this media yet.
    ///     Easiest way to check is by checking the duration, other properties filled by GetMediaInfo are optional
    /// </summary>
    [BsonIgnore]
    private bool HasMediaInfo => Duration != TimeSpan.Zero;

    #endregion
}