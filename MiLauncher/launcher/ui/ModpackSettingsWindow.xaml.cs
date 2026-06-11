using System;
using System.Windows;

namespace MiLauncher.launcher.ui
{
    public partial class ModpackSettingsWindow : Window
    {
        public ModpackSettingsWindow(string instancePath)
        {
            InitializeComponent();
            DataContext = new ModpackSettingsViewModel(instancePath);

            try
            {
                string pngPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "program_info", "milauncher_icon.png");
                if (System.IO.File.Exists(pngPath))
                {
                    this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(pngPath));
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
