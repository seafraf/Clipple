using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipMan.ViewModel
{
    public class SettingsViewModel : ObservableObject
    {
        #region Properties
        private string defaultOutputFolder = Path.Combine(Environment.CurrentDirectory, "Output");
        public string DefaultOutputFolder
        {
            get => defaultOutputFolder;
            set => SetProperty(ref defaultOutputFolder, value);
        }

        #endregion
    }
}
