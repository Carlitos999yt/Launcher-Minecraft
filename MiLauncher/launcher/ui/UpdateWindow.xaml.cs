using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MiLauncher.launcher.ui
{
    public partial class UpdateWindow : Window
    {
        private readonly string _updateUrl;

        public UpdateWindow(string currentVersion, string newVersion, string changelog, string updateUrl)
        {
            InitializeComponent();
            
            VersionText.Text = $"Versión Actual: {currentVersion}  -->  Nueva Versión: {newVersion}";
            ChangelogText.Text = string.IsNullOrWhiteSpace(changelog) ? "No se proporcionaron detalles de la versión." : changelog;
            _updateUrl = updateUrl;

            // Make window draggable
            this.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    this.DragMove();
                }
            };
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_updateUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _updateUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir el enlace de descarga: {ex.Message}", "Error al actualizar", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}
