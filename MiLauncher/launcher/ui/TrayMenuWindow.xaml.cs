using System;
using System.Windows;

namespace MiLauncher.launcher.ui
{
    public partial class TrayMenuWindow : Window
    {
        public TrayMenuWindow(bool isGameRunning)
        {
            InitializeComponent();
            
            BtnForceClose.IsEnabled = isGameRunning;
            
            this.Loaded += (s, e) =>
            {
                // Posicionar la ventana cerca del cursor
                var mousePos = System.Windows.Forms.Cursor.Position;
                
                this.Left = mousePos.X - this.Width / 2;
                this.Top = mousePos.Y - this.Height; // Mostrar arriba del cursor por defecto
                
                // Asegurar que no se salga de la pantalla
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                
                if (this.Left < 0) this.Left = 0;
                if (this.Left + this.Width > screenWidth) this.Left = screenWidth - this.Width;
                if (this.Top < 0) this.Top = 0;
                if (this.Top + this.Height > screenHeight) this.Top = screenHeight - this.Height;
            };
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RestoreMainWindow(Action<MainViewModel> navigateAction)
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            if (mainWin != null)
            {
                mainWin.Show();
                mainWin.WindowState = WindowState.Normal;
                mainWin.Activate();
                
                if (mainWin.DataContext is MainViewModel vm)
                {
                    navigateAction(vm);
                }
            }
            this.Close();
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            RestoreMainWindow(vm => vm.NavigateHomeCommand.Execute(null));
        }

        private void Configuraciones_Click(object sender, RoutedEventArgs e)
        {
            RestoreMainWindow(vm => vm.NavigateToSettingsCommand.Execute(null));
        }

        private void Skins_Click(object sender, RoutedEventArgs e)
        {
            RestoreMainWindow(vm => vm.NavigateToSkinsCommand.Execute(null));
        }

        private void ForceClose_Click(object sender, RoutedEventArgs e)
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            if (mainWin != null && mainWin.DataContext is MainViewModel vm)
            {
                vm.ForceCloseActiveGame();
            }
            this.Close();
        }

        private void CerrarLauncher_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var mainWin = Application.Current.MainWindow as MainWindow;
            if (mainWin != null)
            {
                mainWin.IsExplicitExit = true;
                mainWin.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}
