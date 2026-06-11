using System;
using System.Windows;
using System.Windows.Controls;

namespace MiLauncher.launcher.ui
{
    public partial class ConsoleView : UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
        }

        private void ConsoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleTextBox.ScrollToEnd();
        }

        private void CopyAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(ConsoleTextBox.Text))
                {
                    Clipboard.SetText(ConsoleTextBox.Text);
                    MessageBox.Show("¡Logs copiados al portapapeles!", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al copiar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
