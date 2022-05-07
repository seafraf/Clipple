using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipMan.Util
{
    public static class Formatting
    {
        /// <summary>
        /// https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
        /// </summary>
        public static string ByteCountToString(long count)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };

            double floatingCount = count;
            int order = 0;
            while (floatingCount >= 1024 && order < sizes.Length - 1)
            {
                order++;
                floatingCount /= 1024.0;
            }

            return string.Format("{0:0.##} {1}", floatingCount, sizes[order]);
        }
    }
}
