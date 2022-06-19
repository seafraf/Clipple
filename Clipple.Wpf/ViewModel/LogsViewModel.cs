using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class LogsViewModel : ObservableObject
    {
        public LogsViewModel()
        {
            LogEntries.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(LastLogEntry));
                OnPropertyChanged(nameof(HasLogs));
                OnPropertyChanged(nameof(FullLog));
            };
        }

        #region Properties
        /// <summary>
        /// All log entries
        /// </summary>
        public ObservableCollection<LogViewModel> LogEntries { get; } = new();

        /// <summary>
        /// The last log entry
        /// </summary>
        public LogViewModel? LastLogEntry => LogEntries.LastOrDefault();

        /// <summary>
        /// True if at least one log entry exists
        /// </summary>
        public bool HasLogs => LastLogEntry != null;

        /// <summary>
        /// Generates a full log string from every log entry
        /// </summary>
        public string FullLog
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var log in LogEntries)
                {
                    builder.Append($"[{log.Time:hh:mm:ss}] {log.Content}\n");
                    if (log.ExtraContent != null)
                        builder.Append($"\t{log.ExtraContent}\n");

                    if (log.Error != null)
                        builder.Append($"\t{log.Error}\n");
                }

                return builder.ToString();
            }
        }
        #endregion

        #region Methods
        public void Log(string content)
        {
            LogEntries.Add(new LogViewModel()
            {
                Content = content
            });
        }

        public void Log(string content, string extraContent)
        {
            LogEntries.Add(new LogViewModel()
            {
                Content = content,
                ExtraContent = extraContent
            });
        }

        public void LogError(string content, Exception error)
        {
            LogEntries.Add(new LogViewModel()
            {
                Content = content,
                Error = error
            });
        }

        public void LogError(string content, string extraContent, Exception error)
        {
            LogEntries.Add(new LogViewModel()
            {
                Content = content,
                ExtraContent = extraContent,
                Error = error
            });
        }
        #endregion
    }
}
