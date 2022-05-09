using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Util.ISOBMFF
{
    /// <summary>
    /// Represents a trak box in ISOBMFF, for right now the only thing we're interested in is the potential
    /// name under the trak/udta/name 
    /// </summary>
    public class Track
    {
        public string? Name { get; set; }
    }
}
