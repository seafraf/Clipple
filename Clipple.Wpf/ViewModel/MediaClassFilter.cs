using Clipple.Types;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

public class MediaClassFilter : ObservableObject
{
    public MediaClassFilter(MediaClass mediaClass)
    {
        this.mediaClass = mediaClass;
    }

    private MediaClass mediaClass;

	public MediaClass Class
	{
		get => mediaClass;
		set => SetProperty(ref mediaClass, value);
	}
}
