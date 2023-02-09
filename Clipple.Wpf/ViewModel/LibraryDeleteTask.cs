using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipple.ViewModel;

public class LibraryDeleteTask : ObservableObject
{
    public class SelectableMedia : ObservableObject
    {
        public SelectableMedia(Media media)
        {
            Media = media;
        }

        private bool selected = true;
        
        public bool Selected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        public Media Media { get; set; }
    }

    public LibraryDeleteTask(IEnumerable<Media> mediaList)
    {
        foreach (var media in mediaList)
            SelectedMedia.Add(new SelectableMedia(media));
    }

    #region Members
    private bool keepFiles = true;
    #endregion

    #region Properties
    public ObservableCollection<SelectableMedia> SelectedMedia { get; } = new();

    public bool KeepFiles
    {
        get => keepFiles;
        set => SetProperty(ref keepFiles, value);
    }
    #endregion

    #region Commands
    public ICommand DeleteCommand => new RelayCommand(() =>
    {
        foreach (var wrapper in SelectedMedia)
            wrapper.Media.RequestDelete(!KeepFiles);

        DialogHost.Close(null);
    }); 
    #endregion
}

