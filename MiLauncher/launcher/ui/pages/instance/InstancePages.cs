using MaterialDesignThemes.Wpf;
using System.Linq;

namespace MiLauncher.launcher.ui.pages.instance
{
    public class VersionPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Versión";
        public override PackIconKind Icon => PackIconKind.LayersOutline;
        public override string Description => "Instala o modifica las versiones de Minecraft, Forge y Fabric de esta instancia.";

        private string _instancePath;
        private MiLauncher.launcher.settings.InstanceConfig _config;

        public VersionPageViewModel(string instancePath)
        {
            _instancePath = instancePath;
            _config = MiLauncher.launcher.settings.InstanceConfig.Load(_instancePath);
        }



        public string GameVersion
        {
            get => _config.GameVersion;
            set
            {
                if (_config.GameVersion != value)
                {
                    _config.GameVersion = value;
                    OnPropertyChanged();
                    Save();
                }
            }
        }

        public string ModLoader
        {
            get => _config.ModLoader;
            set
            {
                if (_config.ModLoader != value)
                {
                    _config.ModLoader = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLoaderVersionEnabled));
                    Save();
                }
            }
        }

        public string LoaderVersion
        {
            get => _config.LoaderVersion;
            set
            {
                if (_config.LoaderVersion != value)
                {
                    _config.LoaderVersion = value;
                    OnPropertyChanged();
                    Save();
                }
            }
        }

        public bool IsLoaderVersionEnabled => ModLoader != "Vanilla";

        public System.Collections.Generic.List<string> GameVersionsList { get; } = new System.Collections.Generic.List<string>
        {
            "1.21", "1.20.6", "1.20.5", "1.20.4", "1.20.1", "1.20", "1.19.4", "1.19.2", "1.18.2", "1.17.1", "1.16.5", "1.12.2", "1.8.9", "1.7.10"
        };

        public System.Collections.Generic.List<string> ModLoadersList { get; } = new System.Collections.Generic.List<string>
        {
            "Vanilla", "Fabric", "Forge"
        };

        private void Save()
        {
            _config.Save(_instancePath);
        }
    }

    public class ModFolderPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Mods";
        public override PackIconKind Icon => PackIconKind.FolderOutline;
        public override string Description => "Habilita, deshabilita o elimina los mods de esta instancia.";

        private string _instancePath;
        public System.Collections.ObjectModel.ObservableCollection<ModItem> ModsList { get; } = new System.Collections.ObjectModel.ObservableCollection<ModItem>();

        private ModItem _selectedMod;
        public ModItem SelectedMod
        {
            get => _selectedMod;
            set 
            { 
                if (SetProperty(ref _selectedMod, value))
                {
                    OnPropertyChanged(nameof(IsModSelected));
                    OnPropertyChanged(nameof(IsDeleteAllowed));
                }
            }
        }

        public bool IsModSelected => SelectedMod != null;
        public bool IsDeleteAllowed => IsModSelected;

        public System.Windows.Input.ICommand OpenModsFolderCommand { get; }
        public System.Windows.Input.ICommand DeleteModCommand { get; }
        public System.Windows.Input.ICommand RefreshModsCommand { get; }

        public ModFolderPageViewModel(string instancePath)
        {
            _instancePath = instancePath;

            OpenModsFolderCommand = new RelayCommand(_ => 
            {
                try
                {
                    string modsDir = System.IO.Path.Combine(_instancePath, "mods");
                    if (!System.IO.Directory.Exists(modsDir))
                    {
                        System.IO.Directory.CreateDirectory(modsDir);
                    }
                    System.Diagnostics.Process.Start("explorer.exe", modsDir);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("No se pudo abrir la carpeta: " + ex.Message);
                }
            });

            DeleteModCommand = new RelayCommand(param => 
            {
                var mod = param as ModItem ?? SelectedMod;
                if (mod != null)
                {
                    var result = System.Windows.MessageBox.Show($"¿Seguro que deseas eliminar el mod '{mod.Name}'?", "Eliminar Mod", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        try
                        {
                            if (System.IO.File.Exists(mod.Path))
                            {
                                System.IO.File.Delete(mod.Path);
                            }
                            ModsList.Remove(mod);
                        }
                        catch (System.Exception ex)
                        {
                            System.Windows.MessageBox.Show("Error al eliminar el archivo: " + ex.Message);
                        }
                    }
                }
            });

            RefreshModsCommand = new RelayCommand(_ => LoadMods());
        }

        public override void OnPageLoaded()
        {
            base.OnPageLoaded();
            LoadMods();
        }

        private void LoadMods()
        {
            ModsList.Clear();
            string modsDir = System.IO.Path.Combine(_instancePath, "mods");
            if (System.IO.Directory.Exists(modsDir))
            {
                foreach (var file in System.IO.Directory.GetFiles(modsDir, "*.jar"))
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    double sizeMb = (double)fileInfo.Length / (1024 * 1024);
                    ModsList.Add(new ModItem
                    {
                        Name = fileInfo.Name,
                        Path = file,
                        SizeText = $"{sizeMb:F2} MB"
                    });
                }
            }
        }
    }

    public class ModItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string SizeText { get; set; }
    }

    public class ResourcePackPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Packs de Recursos";
        public override PackIconKind Icon => PackIconKind.ImageOutline;
        public override string Description => "Gestiona los resource packs instalados.";

        private string _instancePath;
        public System.Collections.ObjectModel.ObservableCollection<ResourcePackItem> ResourcePacksList { get; } = new System.Collections.ObjectModel.ObservableCollection<ResourcePackItem>();

        public System.Windows.Input.ICommand OpenFolderCommand { get; }
        public System.Windows.Input.ICommand RefreshCommand { get; }

        public ResourcePackPageViewModel(string instancePath)
        {
            _instancePath = instancePath;

            OpenFolderCommand = new RelayCommand(_ => 
            {
                try
                {
                    string rpDir = System.IO.Path.Combine(_instancePath, "resourcepacks");
                    if (!System.IO.Directory.Exists(rpDir))
                    {
                        System.IO.Directory.CreateDirectory(rpDir);
                    }
                    System.Diagnostics.Process.Start("explorer.exe", rpDir);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("No se pudo abrir la carpeta: " + ex.Message);
                }
            });

            RefreshCommand = new RelayCommand(_ => LoadPacks());
        }

        public override void OnPageLoaded()
        {
            base.OnPageLoaded();
            LoadPacks();
        }

        private void LoadPacks()
        {
            ResourcePacksList.Clear();
            string rpDir = System.IO.Path.Combine(_instancePath, "resourcepacks");
            if (System.IO.Directory.Exists(rpDir))
            {
                // List files (.zip)
                foreach (var file in System.IO.Directory.GetFiles(rpDir, "*.zip"))
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    ResourcePacksList.Add(new ResourcePackItem { Name = fileInfo.Name, IsFolder = false });
                }
                // List directories
                foreach (var dir in System.IO.Directory.GetDirectories(rpDir))
                {
                    var dirInfo = new System.IO.DirectoryInfo(dir);
                    ResourcePacksList.Add(new ResourcePackItem { Name = dirInfo.Name, IsFolder = true });
                }
            }
        }
    }

    public class ResourcePackItem
    {
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public string IconKind => IsFolder ? "FolderOutline" : "FileZipOutline";
    }

    public class GameOptionsPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Opciones del Juego";
        public override PackIconKind Icon => PackIconKind.GamepadVariantOutline;
        public override string Description => "Modifica el archivo options.txt sin abrir el juego.";

        private string _instancePath;
        private string _optionsContent = "Cargando archivo de opciones...";
        public string OptionsContent
        {
            get => _optionsContent;
            set { _optionsContent = value; OnPropertyChanged(); }
        }


        public System.Windows.Input.ICommand OpenOptionsCommand { get; }

        public GameOptionsPageViewModel(string instancePath)
        {
            _instancePath = instancePath;

            OpenOptionsCommand = new RelayCommand(_ => 
            {
                try
                {
                    string optionsFile = System.IO.Path.Combine(_instancePath, "options.txt");
                    if (!System.IO.File.Exists(optionsFile))
                    {
                        // Crear archivo vacío si no existe
                        System.IO.File.WriteAllText(optionsFile, "# Archivo de opciones de Minecraft\n");
                    }
                    System.Diagnostics.Process.Start("notepad.exe", optionsFile);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("No se pudo abrir options.txt: " + ex.Message);
                }
            });
        }

        public override void OnPageLoaded()
        {
            base.OnPageLoaded();
            LoadOptions();
        }

        private void LoadOptions()
        {
            try
            {
                string optionsFile = System.IO.Path.Combine(_instancePath, "options.txt");
                if (System.IO.File.Exists(optionsFile))
                {
                    OptionsContent = System.IO.File.ReadAllText(optionsFile);
                }
                else
                {
                    OptionsContent = "No se encontró el archivo options.txt en esta instancia.";
                }
            }
            catch (System.Exception ex)
            {
                OptionsContent = "Error al leer options.txt: " + ex.Message;
            }
        }
    }

    public class LogPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Registros (Logs)";
        public override PackIconKind Icon => PackIconKind.FileDocumentOutline;
        public override string Description => "Visualiza los registros generados en la última sesión de juego.";

        private string _logContent = "Cargando logs...";
        public string LogContent
        {
            get => _logContent;
            set { _logContent = value; OnPropertyChanged(); }
        }

        private string _instancePath;

        public LogPageViewModel(string instancePath)
        {
            _instancePath = instancePath;
        }

        public override void OnPageLoaded()
        {
            base.OnPageLoaded();
            string logPath = System.IO.Path.Combine(_instancePath, "launcher_log.txt");
            if (System.IO.File.Exists(logPath))
            {
                try
                {
                    LogContent = System.IO.File.ReadAllText(logPath);
                }
                catch (System.Exception ex)
                {
                    LogContent = "Error al leer los logs: " + ex.Message;
                }
            }
            else
            {
                LogContent = "No se encontraron registros. Asegúrate de iniciar el juego al menos una vez para generar logs.";
            }
        }
    }

    public class InstanceSettingsPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Ajustes de Instancia";
        public override PackIconKind Icon => PackIconKind.SettingsOutline;
        public override string Description => "Configura la memoria RAM y versión de Java específicas para este modpack.";

        private string _instancePath;
        private MiLauncher.launcher.settings.InstanceConfig _config;



        public System.Windows.Input.ICommand AutoDetectCommand { get; }
        public System.Windows.Input.ICommand BrowseJavaCommand { get; }
        public System.Windows.Input.ICommand OpenJavaFolderCommand { get; }
        public System.Windows.Input.ICommand FormatModpackCommand { get; }
        public System.Windows.Input.ICommand DeleteModpackCommand { get; }

        public InstanceSettingsPageViewModel(string instancePath)
        {
            _instancePath = instancePath;
            _config = MiLauncher.launcher.settings.InstanceConfig.Load(_instancePath);

            AutoDetectCommand = new RelayCommand(_ => 
            {
                var bestJava = MiLauncher.launcher.tools.JavaDetector.GetBestJavaPath();
                if (!string.IsNullOrEmpty(bestJava))
                {
                    JavaPath = bestJava;
                }
                else
                {
                    System.Windows.MessageBox.Show("No se encontraron instalaciones de Java en las carpetas comunes.", "Auto-detectar Java", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            });

            BrowseJavaCommand = new RelayCommand(_ =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Seleccionar ejecutable Java (java.exe / javaw.exe)";
                dialog.Filter = "Ejecutable Java (*.exe)|*.exe|Todos los archivos (*.*)|*.*";
                if (dialog.ShowDialog() == true)
                {
                    JavaPath = dialog.FileName;
                }
            });

            OpenJavaFolderCommand = new RelayCommand(_ =>
            {
                try
                {
                    string path = JavaPath;
                    if (System.IO.File.Exists(path))
                    {
                        path = System.IO.Path.GetDirectoryName(path);
                    }
                    if (System.IO.Directory.Exists(path))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", path);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("La ruta o directorio de Java no existe.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });

            FormatModpackCommand = new RelayCommand(_ =>
            {
                var result = System.Windows.MessageBox.Show("Esta acción eliminará todos los mods y archivos de configuración instalados de este modpack, pero mantendrá la configuración de la instancia. ¿Deseas continuar?", "Formatear Modpack", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        // Desvincular assets en el hilo principal antes de borrar
                        SafeDispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.NavigateHomeCommand.Execute(null);
                                var targetVm = mainVm.AvailableModpacks.FirstOrDefault(m => string.Equals(m.InstancePath, _instancePath, System.StringComparison.OrdinalIgnoreCase));
                                if (targetVm != null)
                                {
                                    targetVm.ClearAssetsBeforeDelete();
                                }
                            }
                        });

                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();

                        // Borrar carpeta mods
                        string modsPath = System.IO.Path.Combine(_instancePath, "mods");
                        if (System.IO.Directory.Exists(modsPath))
                        {
                            System.IO.Directory.Delete(modsPath, true);
                        }

                        // Borrar configs locales si existen
                        string configPath = System.IO.Path.Combine(_instancePath, "config");
                        if (System.IO.Directory.Exists(configPath))
                        {
                            System.IO.Directory.Delete(configPath, true);
                        }

                        System.Windows.MessageBox.Show("El modpack se ha formateado correctamente. Los mods y configuraciones se han eliminado.", "Formateado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                        // Recargar instancias
                        SafeDispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.LoadInstances();
                            }
                            
                            // Cerrar ventana de ajustes
                            foreach (System.Windows.Window win in System.Windows.Application.Current.Windows)
                            {
                                if (win is MiLauncher.launcher.ui.ModpackSettingsWindow)
                                {
                                    win.Close();
                                    break;
                                }
                            }
                        });
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error al formatear: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            });

            DeleteModpackCommand = new RelayCommand(_ =>
            {
                var result = System.Windows.MessageBox.Show("¿Estás completamente seguro de borrar este modpack? Se eliminarán todos los archivos físicos en el disco permanentemente. ¿Deseas continuar?", "Borrar Modpack", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Stop);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        // Desvincular assets en el hilo principal antes de borrar
                        SafeDispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.NavigateHomeCommand.Execute(null);
                                var targetVm = mainVm.AvailableModpacks.FirstOrDefault(m => string.Equals(m.InstancePath, _instancePath, System.StringComparison.OrdinalIgnoreCase));
                                if (targetVm != null)
                                {
                                    targetVm.ClearAssetsBeforeDelete();
                                }
                            }
                        });

                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();

                        if (System.IO.Directory.Exists(_instancePath))
                        {
                            System.IO.Directory.Delete(_instancePath, true);
                        }

                        System.Windows.MessageBox.Show("El modpack se ha eliminado por completo.", "Eliminado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                        SafeDispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.LoadInstances();
                                if (mainVm.NavigateHomeCommand.CanExecute(null))
                                {
                                    mainVm.NavigateHomeCommand.Execute(null);
                                }
                            }
                            
                            // Cerrar ventana de ajustes
                            foreach (System.Windows.Window win in System.Windows.Application.Current.Windows)
                            {
                                if (win is MiLauncher.launcher.ui.ModpackSettingsWindow)
                                {
                                    win.Close();
                                    break;
                                }
                            }
                        });
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error al borrar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            });
        }

        public bool UseGlobalSettings
        {
            get => _config.UseGlobalSettings;
            set
            {
                _config.UseGlobalSettings = value;
                if (!value && string.IsNullOrWhiteSpace(_config.JavaPath))
                {
                    JavaPath = ""; // Permitir que esté vacío para auto-detectar
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLocalSettingsEnabled));
                Save();
            }
        }

        public bool IsLocalSettingsEnabled => !UseGlobalSettings;

        public int MinMem
        {
            get => _config.MinMem;
            set { _config.MinMem = value; OnPropertyChanged(); Save(); }
        }

        public int MaxMem
        {
            get => _config.MaxMem;
            set { _config.MaxMem = value; OnPropertyChanged(); Save(); }
        }

        public int PermGen
        {
            get => _config.PermGen;
            set { _config.PermGen = value; OnPropertyChanged(); Save(); }
        }

        public string JavaPath
        {
            get => _config.JavaPath;
            set { _config.JavaPath = value; OnPropertyChanged(); Save(); }
        }

        public bool Fullscreen
        {
            get => _config.Fullscreen;
            set { _config.Fullscreen = value; OnPropertyChanged(); Save(); }
        }

        public int ResolutionWidth
        {
            get => _config.ResolutionWidth;
            set { _config.ResolutionWidth = value; OnPropertyChanged(); Save(); }
        }

        public int ResolutionHeight
        {
            get => _config.ResolutionHeight;
            set { _config.ResolutionHeight = value; OnPropertyChanged(); Save(); }
        }

        public string JvmArgs
        {
            get => _config.JvmArgs;
            set { _config.JvmArgs = value; OnPropertyChanged(); Save(); }
        }

        public string WrapperCommand
        {
            get => _config.WrapperCommand;
            set { _config.WrapperCommand = value; OnPropertyChanged(); Save(); }
        }

        public string PreLaunchCommand
        {
            get => _config.PreLaunchCommand;
            set { _config.PreLaunchCommand = value; OnPropertyChanged(); Save(); }
        }

        public string PostExitCommand
        {
            get => _config.PostExitCommand;
            set { _config.PostExitCommand = value; OnPropertyChanged(); Save(); }
        }

        public bool MaximizeWindow
        {
            get => _config.MaximizeWindow;
            set { _config.MaximizeWindow = value; OnPropertyChanged(); Save(); }
        }

        private void Save()
        {
            _config.Save(_instancePath);
        }
    }
}
