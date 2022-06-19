using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class LogViewModel : ObservableObject
    {
        #region Properties
        private DateTime time = DateTime.Now;
        public DateTime Time
        {
            get => time;
            set => SetProperty(ref time, value);
        }

        private string content = "";
        public string Content
        {
            get => content;
            set => SetProperty(ref content, value);
        }

        private string? extraContent;
        public string? ExtraContent
        {
            get => extraContent;
            set => SetProperty(ref extraContent, value);
        }

        private Exception? error;
        public Exception? Error
        {
            get => error;
            set
            {
                SetProperty(ref error, value);
                OnPropertyChanged(nameof(IsError));
            }
        }

        public bool IsError => error != null;
        #endregion
    }
}
