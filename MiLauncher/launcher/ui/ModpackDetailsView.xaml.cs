using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiLauncher.launcher.ui
{
    public partial class ModpackDetailsView : System.Windows.Controls.UserControl
    {
        public ModpackDetailsView()
        {
            InitializeComponent();
        }

        // Forzar que el scroll vertical funcione sin importar qué hijo esté debajo del mouse
        private void MainScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        // El ScrollViewer horizontal de imágenes reenvía el scroll al padre
        private void ImagesScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // No bloquear: dejar que el evento suba al MainScroll
        }
    }
}
