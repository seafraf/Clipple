using System.Collections.Generic;
using Clipple.ViewModel;

namespace Clipple.Types;

public class EditClassesTask
{
    public EditClassesTask(MediaClass @class, IEnumerable<Media> media)
    {
        Class = @class;
        SelectedMedia.AddRange(media);
    }

    public MediaClass Class { get; }

    public List<Media> SelectedMedia { get; } = new();
}