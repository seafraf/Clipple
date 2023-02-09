using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Clipple.Types;

/// <summary>
///     Clipple wrapper for the AVCodec struct.
/// </summary>
public class AudioVideoCodec : IEquatable<AudioVideoCodec>
{
    private AudioVideoCodec(AVCodecID id, AVMediaType type, string name, string displayName)
    {
        Id          = id;
        Type        = type;
        Name        = name;
        DisplayName = displayName;
    }

    public static unsafe AudioVideoCodec? New(AVCodec* codec)
    {
        if (codec->name == null || codec->long_name == null)
            return null;

        var name     = Marshal.PtrToStringAnsi((nint)codec->name);
        var longName = Marshal.PtrToStringAnsi((nint)codec->long_name);

        if (name == null || longName == null)
            return null;

        return new AudioVideoCodec(codec->id, codec->type, name, longName);
    }

    public AVCodecID   Id          { get; }
    public AVMediaType Type        { get; }
    public string      Name        { get; }
    public string      DisplayName { get; }

    public override bool Equals(object? obj)
    {
        return obj is AudioVideoCodec codec && Name == codec.Name;
    }

    public bool Equals(AudioVideoCodec? other)
    {
        return Equals((object?)other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }

    public override string? ToString()
    {
        return $"{DisplayName}";
    }
}