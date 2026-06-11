using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MiLauncher.Controls;
using MiLauncher.Models;
using MiLauncher.launcher.net;
using MiLauncher.launcher.tools;

namespace MiLauncher.launcher.ui
{
    public partial class SkinsView : System.Windows.Controls.UserControl
    {
        private bool _isGeneratingPreviews = false;

        public SkinsView()
        {
            InitializeComponent();
            this.DataContextChanged += SkinsView_DataContextChanged;
            this.Loaded += SkinsView_Loaded;
            this.Unloaded += SkinsView_Unloaded;
        }

        private void SkinsView_Unloaded(object sender, RoutedEventArgs e)
        {
            MainViewModel.MinimizeMemory();
        }

        private void SkinsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SkinsViewModel vm)
            {
                StartSequentialPreviewGeneration(vm);
            }
        }

        private void SkinsView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SkinsViewModel vm)
            {
                vm.CaptureScreenshotFunc = async () =>
                {
                    if (Editor3DViewer != null)
                    {
                        return await Editor3DViewer.CaptureScreenshotAsync();
                    }
                    return null;
                };

                StartSequentialPreviewGeneration(vm);
            }
        }

        private async void StartSequentialPreviewGeneration(SkinsViewModel vm)
        {
            if (_isGeneratingPreviews) return;
            _isGeneratingPreviews = true;

            try
            {
                // Esperar a que el WebView2 del generador oculto se inicialice
                await System.Threading.Tasks.Task.Delay(1500);

                while (true)
                {
                    // Si la vista ya no está cargada en la UI, abortamos
                    if (!this.IsLoaded) break;

                    // Buscar la primera skin de la biblioteca que no tenga previsualización guardada
                    var skinToProcess = vm.SavedSkins.FirstOrDefault(s => string.IsNullOrEmpty(s.PreviewPath) || !File.Exists(s.PreviewPath));
                    if (skinToProcess == null)
                        break; // Todas tienen previsualización estática

                    // Configurar el generador oculto con los datos de esta skin
                    HiddenPreviewGenerator.SkinPath = skinToProcess.SkinPath;
                    HiddenPreviewGenerator.CapePath = skinToProcess.CapePath;
                    HiddenPreviewGenerator.IsSlimModel = skinToProcess.IsSlimModel;

                    string dataUrl = null;
                    // Intentar capturar la pantalla del visor oculto
                    for (int i = 0; i < 5; i++)
                    {
                        await System.Threading.Tasks.Task.Delay(800 + i * 300);
                        if (!this.IsLoaded) return;

                        dataUrl = await HiddenPreviewGenerator.CaptureScreenshotAsync();
                        if (!string.IsNullOrEmpty(dataUrl) && dataUrl.StartsWith("data:image/png;base64,"))
                            break;
                    }

                    if (!string.IsNullOrEmpty(dataUrl) && dataUrl.StartsWith("data:image/png;base64,"))
                    {
                        try
                        {
                            string base64 = dataUrl.Substring("data:image/png;base64,".Length);
                            byte[] bytes = Convert.FromBase64String(base64);
                            bytes = SkinProcessor.CropTransparentMarginsAndSave(bytes);

                            string localSkinsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Skins");
                            string previewsDir = Path.Combine(localSkinsDir, "Previews");
                            Directory.CreateDirectory(previewsDir);

                            string previewPath = Path.Combine(previewsDir, "preview_" + Guid.NewGuid().ToString() + ".png");
                            File.WriteAllBytes(previewPath, bytes);

                            // Asignar en el hilo principal
                            skinToProcess.PreviewPath = previewPath;

                            SkinManager.SaveSkins(vm.SavedSkins, vm.CurrentSkin);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error en generación secuencial de captura: " + ex.Message);
                        }
                    }
                    else
                    {
                        // Si falló repetidamente, le ponemos un path temporal para no entrar en bucle infinito
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en bucle de generación de previsualizaciones: " + ex.Message);
            }
            finally
            {
                _isGeneratingPreviews = false;
                // Liberar recursos del visor de capturas en segundo plano
                this.Dispatcher.Invoke(() =>
                {
                    HiddenPreviewGenerator.Dispose();
                });
            }
        }
    }
}
