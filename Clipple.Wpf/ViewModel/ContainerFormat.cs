using System;
using System.Collections.Generic;
using System.Linq;
using Clipple.Types;
using FFmpeg.AutoGen;
using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

public class ContainerFormat : ObservableObject, IEquatable<ContainerFormat>
{
    public ContainerFormat(string displayName, string[] names, string[] extensions, List<AudioVideoCodec> supportedCodecs)
    {
        this.displayName = displayName;

        this.names      = names.Distinct().ToList();
        this.extensions = extensions.Distinct().ToList();

        foreach (var codec in supportedCodecs)
            if (codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                AudioCodecs.Add(codec);
            else if (codec.Type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                VideoCodecs.Add(codec);
    }

    #region Members

    private string displayName;

    private List<string> names;

    private List<string> extensions;

    private List<AudioVideoCodec> audioCodecs = new();

    private List<AudioVideoCodec> videoCodecs = new();

    #endregion

    #region Properties

    /// <summary>
    ///     Long name for this container format.
    /// </summary>
    public string DisplayName
    {
        get => displayName;
        set => SetProperty(ref displayName, value);
    }

    /// <summary>
    ///     All names given by FFMPEG for this container format.  Most container formats have only one name,
    ///     no container formats have no names.
    /// </summary>
    public List<string> Names
    {
        get => names;
        set => SetProperty(ref names, value);
    }

    /// <summary>
    ///     All names given by FFMPEG for this container format.  Most container formats have only one extension,
    ///     no container formats have no extensions.
    /// </summary>
    public List<string> Extensions
    {
        get => extensions;
        set => SetProperty(ref extensions, value);
    }

    /// <summary>
    ///     A list of audio codecs that are supported by this container format.
    /// </summary>
    public List<AudioVideoCodec> AudioCodecs
    {
        get => audioCodecs;
        set => SetProperty(ref audioCodecs, value);
    }

    /// <summary>
    ///     A list of video codecs that are supported by this container format.
    /// </summary>
    public List<AudioVideoCodec> VideoCodecs
    {
        get => videoCodecs;
        set => SetProperty(ref videoCodecs, value);
    }

    /// <summary>
    ///     Whether or not this container format supports audio.
    /// </summary>
    public bool SupportsAudio => AudioCodecs.Count > 0;

    /// <summary>
    ///     Whether or not this container format supports video.
    /// </summary>
    public bool SupportsVideo => VideoCodecs.Count > 0;

    /// <summary>
    ///     The name first provided name for this container format.  This should be fine to use in all cases, the Names list
    ///     contains
    ///     usually only one name.  Formats that contain more than one name are not very often used and the differences between
    ///     the names is small
    /// </summary>
    public string Name => Names.First();

    /// <summary>
    ///     The extension to use for this container format.
    /// </summary>
    public string Extension => Extensions.First();

    [BsonIgnore]
    public string Category
    {
        get
        {
            if (SupportsVideo && SupportsAudio)
                return "Audio & Video";

            if (SupportsVideo && !SupportsAudio)
                return "Video only";

            if (!SupportsVideo && SupportsAudio)
                return "Audio only";

            return "?";
        }
    }

    #endregion

    #region Methods

    public override string? ToString()
    {
        return $"{DisplayName}";
    }

    public override bool Equals(object? obj)
    {
        return obj is ContainerFormat format &&
               Names == format.Names &&
               Extensions.SequenceEqual(format.Extensions) &&
               DisplayName == format.DisplayName &&
               SupportsAudio == format.SupportsAudio &&
               SupportsVideo == format.SupportsVideo &&
               Category == format.Category;
    }

    public bool Equals(ContainerFormat? other)
    {
        return Equals((object?)other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Names, Extensions, DisplayName, SupportsAudio, SupportsVideo, Category);
    }

    #endregion
}