using Clipple.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public class EditClassesTask
    {
        public EditClassesTask(MediaClass @class, IEnumerable<Media> media)
        {
            Class = @class;
            SelectedMedia.AddRange(media);
        }

        public MediaClass Class { get; set; }

        public List<Media> SelectedMedia { get; } = new();
    }
}
