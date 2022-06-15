using Microsoft.Toolkit.Mvvm.ComponentModel;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipple.Wpf.ViewModel
{
    public class UpdateDialogViewModel : ObservableObject
    {
        public UpdateDialogViewModel(UpdateViewModel updateViewModel, ICommand downloadCommand, ICommand closeCommand)
        {
            this.updateViewModel = updateViewModel;

            CloseCommand    = closeCommand;
            DownloadCommand = downloadCommand;  
        }

        #region Properties
        private UpdateViewModel updateViewModel;
        public UpdateViewModel UpdateViewModel
        {
            get => updateViewModel;
            set => SetProperty(ref updateViewModel, value);
        }
        #endregion

        #region Command
        public ICommand CloseCommand { get; }
        public ICommand DownloadCommand { get; }
        #endregion
    }
}
