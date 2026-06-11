using System;
using System.Text;
using System.Windows.Input;
using MiLauncher.launcher.tools;

namespace MiLauncher.launcher.ui
{
    public class ConsoleViewModel : ViewModelBase
    {
        private static readonly StringBuilder _logBuffer = new StringBuilder();
        private static bool _isSubscribed = false;
        private static readonly object _lock = new object();

        public static string LogText
        {
            get
            {
                lock (_lock)
                {
                    return _logBuffer.ToString();
                }
            }
        }

        public static event Action? LogUpdated;

        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isSubscribed) return;

                // Load existing logs from latest.log if any
                try
                {
                    string logDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                        "MiLauncher"
                    );
                    string logPath = System.IO.Path.Combine(logDir, "latest.log");
                    if (System.IO.File.Exists(logPath))
                    {
                        string initialLogs = System.IO.File.ReadAllText(logPath);
                        _logBuffer.Append(initialLogs);
                    }
                }
                catch { }

                Logger.LogAdded += OnLogAdded;
                _isSubscribed = true;
            }
        }

        private static void OnLogAdded(string line)
        {
            lock (_lock)
            {
                _logBuffer.AppendLine(line);
            }
            LogUpdated?.Invoke();
        }

        public string LogContent => LogText;

        public ICommand ClearCommand { get; }

        public ConsoleViewModel()
        {
            Initialize();
            LogUpdated += OnLogUpdated;

            ClearCommand = new RelayCommand(_ =>
            {
                lock (_lock)
                {
                    _logBuffer.Clear();
                }
                OnLogUpdated();
            });
        }

        private void OnLogUpdated()
        {
            OnPropertyChanged(nameof(LogContent));
        }

        // Hook clean up if needed, but since it's 24/7, it lives forever.
    }
}
