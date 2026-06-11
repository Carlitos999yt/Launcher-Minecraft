using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MiLauncher.Core;

namespace MiLauncher.launcher.ui.Dialogs
{
    public partial class SkinsDialog : Window
    {
        private string? _selectedFilePath = null;
        private readonly bool _isGameRunning;
        private readonly string _modpackName;
        private readonly string? _customInstancePath;

        public SkinsDialog(bool isGameRunning, string modpackName, string? customInstancePath)
        {
            InitializeComponent();
            _isGameRunning = isGameRunning;
            _modpackName = modpackName;
            _customInstancePath = customInstancePath;
        }

        private void SelectSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos de imagen PNG (*.png)|*.png",
                Title = "Seleccionar archivo de Skin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                SelectedFileText.Text = Path.GetFileName(_selectedFilePath);
                SaveSkinButton.IsEnabled = true;
            }
        }

        private void SaveSkinButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath)) return;

            try
            {
                string instancePath = PathManager.GetInstancePath(_modpackName, _customInstancePath);
                
                // Ruta estandar que lee el mod de OfflineSkins
                string skinDir = Path.Combine(instancePath, "cachedImages", "skins");
                Directory.CreateDirectory(skinDir);
                
                // Nombre quemado por ahora, esto luego saldrá del nombre del usuario ingresado
                string destPath = Path.Combine(skinDir, "TuNombreAqui.png");
                
                File.Copy(_selectedFilePath, destPath, overwrite: true);

                if (_isGameRunning)
                {
                    MessageBox.Show("Skin guardada localmente.\n\nPara aplicar los cambios DEBES reiniciar el juego (Cerrar Minecraft y volver a iniciar).", "Aviso de Reinicio", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Skin guardada localmente. ¡Tu avatar se verá cuando entres al servidor!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar la skin: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
