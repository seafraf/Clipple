using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel;

public class LibraryEditClassesTask : ObservableObject
{
    public LibraryEditClassesTask(IEnumerable<Media> enumerable)
    {
        Enumerable = enumerable;
    }

    public IEnumerable<Media> Enumerable { get; }
}

