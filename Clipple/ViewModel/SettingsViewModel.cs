using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class SettingsViewModel : ObservableObject
    {
        #region Properties
        private string defaultOutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public string DefaultOutputFolder
        {
            get => defaultOutputFolder;
            set => SetProperty(ref defaultOutputFolder, value);
        }

        #endregion
    }
}
