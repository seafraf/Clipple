using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Clipple.ViewModel;

namespace Clipple.Types;

public class ContainerFormatCollection
{
    public ContainerFormatCollection()
    {
        SupportedFormats = GetSupportedFormats().ToList();

        foreach (var format in SupportedFormats)
            SupportedFormatNames.UnionWith(format.Names);
    }

    #region Methods
    private static unsafe ContainerFormat? FormatFromNative(byte* namePtr, byte* longNamePtr, byte* extensionsPtr, List<AudioVideoCodec>? supportedCodecIDs = null)
    {
        if (namePtr == null || longNamePtr == null)
            return null;
        
        var name       = Marshal.PtrToStringAnsi((nint)namePtr);
        var longName   = Marshal.PtrToStringAnsi((nint)longNamePtr);
        var extensions = extensionsPtr == null ? name : Marshal.PtrToStringAnsi((nint)extensionsPtr);

        if (name == null || longName == null || extensions == null)
            return null;

        return new ContainerFormat(longName, name.Split(','), extensions.Split(','), supportedCodecIDs ?? new List<AudioVideoCodec>());
    }

    private static unsafe IEnumerable<ContainerFormat> GetSupportedFormats()
    {
        var dmx = new List<ContainerFormat>();
        var mx  = new List<ContainerFormat>();

        var supportedCodecs = GetSupportedEncoders();
        
        var state       = IntPtr.Zero;
        var inputFormat = ffmpeg.av_demuxer_iterate((void**)&state);
        while (inputFormat != null)
        {
            if (FormatFromNative(inputFormat->name, inputFormat->long_name, inputFormat->extensions) is { } containerFormat)
            {
                dmx.Add(containerFormat);   
            }

            inputFormat = ffmpeg.av_demuxer_iterate((void**)&state);
        }

        state = IntPtr.Zero;
        var outputFormat = ffmpeg.av_muxer_iterate((void**)&state);
        while (outputFormat != null)
        {
            var codecs = supportedCodecs.Where(x =>
            {
                // Looks like there is a bug with ffmpeg where query_codec does not return 1 for supported codecs
                if (outputFormat->audio_codec == AVCodecID.AV_CODEC_ID_MP3)
                    return x.Id == AVCodecID.AV_CODEC_ID_MP3;

                return ffmpeg.avformat_query_codec(outputFormat, x.Id, ffmpeg.FF_COMPLIANCE_STRICT) == 1;
            }).ToList();
            
            if (codecs.Count > 0 && FormatFromNative(outputFormat->name, outputFormat->long_name, outputFormat->extensions, codecs) is { } containerFormat)
                mx.Add(containerFormat);   

            outputFormat = ffmpeg.av_muxer_iterate((void**)&state);
        }

        // Create a joined list where muxers have a matching demuxer.  Extensions and names for the demuxer and muxer overlap but
        // are not always the same.  The list of joined (de)muxers should contain ContainerFormats that have names and extensions from
        // both.
        var joined = new List<ContainerFormat>();
        foreach (var demuxer in dmx)
        {
            var muxer = mx.FirstOrDefault(x => x.Names.Any(name => demuxer.Names.Contains(name)));
            if (muxer == null)
                continue;

            muxer.Names.UnionWith(demuxer.Names);
            muxer.Extensions.UnionWith(demuxer.Extensions);

            joined.Add(muxer);
        }

        return joined;
    }

    private static unsafe HashSet<AudioVideoCodec> GetSupportedEncoders()
    {
        var encoderIDs = new HashSet<AudioVideoCodec>();

        var state = IntPtr.Zero;
        var codec = ffmpeg.av_codec_iterate((void**)&state);

        while (codec != null)
        {
            if (codec->type == AVMediaType.AVMEDIA_TYPE_VIDEO || codec->type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                if (AudioVideoCodec.New(codec) is { } avc)
                {
                    if (ffmpeg.av_codec_is_encoder(codec) != 0)
                        encoderIDs.Add(avc);
                }
            }

            codec = ffmpeg.av_codec_iterate((void**)&state);
        }

        return encoderIDs;
    }
    #endregion
    
    #region Properties
    public List<ContainerFormat> SupportedFormats { get; }

    public HashSet<string> SupportedFormatNames { get; } = new();
    #endregion
}
