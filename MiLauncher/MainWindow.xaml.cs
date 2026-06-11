using System;
using System.Windows;
using System.ComponentModel;

namespace MiLauncher
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        public bool IsExplicitExit { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Text = "MiLauncher";
            
            try
            {
                string pngPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "program_info", "milauncher_icon.png");
                if (System.IO.File.Exists(pngPath))
                {
                    // 1. Icono de Bandeja
                    using (var bitmap = new System.Drawing.Bitmap(pngPath))
                    {
                        IntPtr hIcon = bitmap.GetHicon();
                        _notifyIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);
                    }
                    
                    // 2. Icono de Ventana y Taskbar
                    this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(pngPath));
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            _notifyIcon.Visible = true;
            
            _notifyIcon.MouseUp += (s, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
                else if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    bool isGameRunning = false;
                    if (this.DataContext is launcher.ui.MainViewModel vm)
                    {
                        isGameRunning = vm.IsGameRunning;
                    }
                    
                    var trayMenu = new launcher.ui.TrayMenuWindow(isGameRunning);
                    trayMenu.Show();
                    trayMenu.Activate();
                }
            };
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (IsExplicitExit)
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                base.OnClosing(e);
                System.Environment.Exit(0); // Salida instantánea y limpia
            }
            else
            {
                e.Cancel = true;
                this.Hide();
                
                // Actuar como cancelar implícito para limpiar estados de skins
                if (this.DataContext is launcher.ui.MainViewModel vm)
                {
                    vm.ResetSkinsState();
                }

                // Reducir consumo en segundo plano
                launcher.ui.MainViewModel.MinimizeMemory();
            }
        }
    }
}