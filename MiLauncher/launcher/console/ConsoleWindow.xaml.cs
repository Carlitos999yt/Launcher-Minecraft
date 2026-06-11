using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MiLauncher.launcher.ui
{
    public partial class ConsoleWindow : Window
    {
        public ConsoleWindow()
        {
            InitializeComponent();
        }

        public void AppendLog(string message)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
                Dispatcher.Invoke(() =>
                {
                    var run = new Run(message + "\n");
                    if (message.Contains("[ERROR]") || message.Contains("Exception"))
                    {
                        run.Foreground = Brushes.Red;
                    }
                    else if (message.Contains("[WARN]"))
                    {
                        run.Foreground = Brushes.Yellow;
                    }
                    else
                    {
                        run.Foreground = Brushes.LightGray;
                    }

                    LogParagraph.Inlines.Add(run);
                    ConsoleOutput.ScrollToEnd();
                });
            }
            catch
            {
                // Ignore console errors when shutting down
            }
        }
    }
}
