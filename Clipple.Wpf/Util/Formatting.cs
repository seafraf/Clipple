namespace Clipple.Util;

public static class Formatting
{
    /// <summary>
    ///     https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
    /// </summary>
    public static string ByteCountToString(long count)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };

        double floatingCount = count;
        var    order         = 0;
        while (floatingCount >= 1024 && order < sizes.Length - 1)
        {
            order++;
            floatingCount /= 1024.0;
        }

        return $"{floatingCount:0.##} {sizes[order]}";
    }
}