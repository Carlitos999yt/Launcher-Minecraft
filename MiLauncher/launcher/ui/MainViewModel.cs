using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;

namespace MiLauncher.launcher.ui
{
    public class MainViewModel : ViewModelBase
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(System.IntPtr proc, System.IntPtr min, System.IntPtr max);

        public static void MinimizeMemory()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            try
            {
                if (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, new System.IntPtr(-1), new System.IntPtr(-1));
                }
            }
            catch { }
        }

        public bool IsGameRunning => AvailableModpacks != null && AvailableModpacks.Any(m => m.IsGameRunning);
        public string y => "v.1.0.0";

        public void ForceCloseActiveGame()
        {
            if (AvailableModpacks != null)
            {
                var active = AvailableModpacks.FirstOrDefault(m => m.IsGameRunning);
                if (active != null)
                {
                    active.PlayCommand.Execute(null);
                }
            }
        }

        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private System.Windows.Media.ImageSource _playerFace;
        public System.Windows.Media.ImageSource PlayerFace
        {
            get => _playerFace;
            set => SetProperty(ref _playerFace, value);
        }

        public void UpdateProfileFace(string skinPath)
        {
            PlayerFace = MiLauncher.launcher.tools.SkinProcessor.GetFaceFromSkin(skinPath);
        }

        private string _currentAccountName;
        public string CurrentAccountName
        {
            get => _currentAccountName;
            set => SetProperty(ref _currentAccountName, value);
        }

        private bool _isMenuVisible = true;
        public bool IsMenuVisible
        {
            get => _isMenuVisible;
            set => SetProperty(ref _isMenuVisible, value);
        }

        private bool _isRightSidebarVisible = true;
        public bool IsRightSidebarVisible
        {
            get => _isRightSidebarVisible;
            set => SetProperty(ref _isRightSidebarVisible, value);
        }

        public ObservableCollection<ModpackDetailsViewModel> AvailableModpacks { get; private set; }

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateToModpackCommand { get; }
        public ICommand NavigateToSkinsCommand { get; }
        public ICommand NavigateToConsoleCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }

        private HomeViewModel _homeViewModel;
        private SkinsViewModel _skinsViewModel;

        public MainViewModel()
        {
            ConsoleViewModel.Initialize();
            _homeViewModel = new HomeViewModel(this);

            _currentView = _homeViewModel;
            _currentAccountName = MiLauncher.launcher.settings.SettingsManager.LoadSettings().SelectedAccount;

            AvailableModpacks = new ObservableCollection<ModpackDetailsViewModel>();
            LoadInstances();

            // Comprobación de actualizaciones al iniciar el launcher
            System.Threading.Tasks.Task.Run(() => CheckForUpdatesAsync());

            // Pre-cargar el rostro del jugador para que no haya parpadeo
            var skinData = MiLauncher.launcher.net.SkinManager.LoadSkins();
            if (!string.IsNullOrEmpty(skinData.CurrentSkinId))
            {
                var currSkin = System.Linq.Enumerable.FirstOrDefault(skinData.Skins, s => s.Id == skinData.CurrentSkinId);
                if (currSkin != null)
                {
                    UpdateProfileFace(currSkin.SkinPath);
                }
            }

            NavigateHomeCommand = new RelayCommand(_ => 
            {
                LoadInstances();
                CurrentView = _homeViewModel;
                CurrentAccountName = MiLauncher.launcher.settings.SettingsManager.LoadSettings().SelectedAccount;
                IsMenuVisible = true;
                IsRightSidebarVisible = true;
            });

            NavigateToModpackCommand = new RelayCommand(modpack => 
            {
                if (modpack is ModpackDetailsViewModel vm)
                {
                    CurrentView = vm;
                    CurrentAccountName = MiLauncher.launcher.settings.SettingsManager.LoadSettings().SelectedAccount;
                    IsMenuVisible = true;
                    IsRightSidebarVisible = true;
                }
            });

            NavigateToSkinsCommand = new RelayCommand(_ => 
            {
                if (_skinsViewModel == null)
                    _skinsViewModel = new SkinsViewModel(this);
                _skinsViewModel.IsEditing = false;
                CurrentView = _skinsViewModel;
                // "Al entrar a ese de Mis Skins ya no puedes ver la columna de los mods ni el menú, nada."
                IsMenuVisible = false;
                IsRightSidebarVisible = false;
            });

            NavigateToConsoleCommand = new RelayCommand(_ => 
            {
                CurrentView = new ConsoleViewModel();
                IsMenuVisible = true;
                IsRightSidebarVisible = false;
            });

            NavigateToSettingsCommand = new RelayCommand(_ => 
            {
                CurrentView = new GeneralSettingsViewModel();
                IsMenuVisible = false;
                IsRightSidebarVisible = false;
            });
        }

        public void LoadInstances()
        {
            AvailableModpacks.Clear();
            var settings = MiLauncher.launcher.settings.SettingsManager.LoadSettings();

            // 1. Cargar la Whitelist de modpacks desde el caché local si existe
            string cachePath = System.IO.Path.Combine(settings.InstancesFolder, "whitelist_cache.json");
            System.Collections.Generic.List<MiLauncher.launcher.minecraft.WhitelistedModpack> whitelist = new System.Collections.Generic.List<MiLauncher.launcher.minecraft.WhitelistedModpack>();
            if (System.IO.File.Exists(cachePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(cachePath);
                    whitelist = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<MiLauncher.launcher.minecraft.WhitelistedModpack>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new System.Collections.Generic.List<MiLauncher.launcher.minecraft.WhitelistedModpack>();
                }
                catch { }
            }

            // 2. Escanear carpetas locales de instancias
            var installedPaths = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (System.IO.Directory.Exists(settings.InstancesFolder))
            {
                foreach (var dir in System.IO.Directory.GetDirectories(settings.InstancesFolder))
                {
                    installedPaths.Add(dir);
                    var config = MiLauncher.launcher.settings.InstanceConfig.Load(dir);
                    string name = string.IsNullOrEmpty(config.Name) ? System.IO.Path.GetFileName(dir) : config.Name;
                    string loader = string.IsNullOrEmpty(config.ModLoader) ? "Vanilla" : config.ModLoader;

                    // Buscar si coincide con algún modpack de la whitelist
                    var wlMatch = whitelist.FirstOrDefault(w => string.Equals(w.Id, System.IO.Path.GetFileName(dir), StringComparison.OrdinalIgnoreCase) || string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));

                    var vm = new ModpackDetailsViewModel(name, $"{loader} {config.GameVersion}", "Instancia instalada localmente.", dir);
                    if (wlMatch != null)
                    {
                        vm.WhitelistData = wlMatch;
                        vm.IsInstalled = true;
                    }
                    else
                    {
                        vm.IsInstalled = true;
                    }

                    AvailableModpacks.Add(vm);
                }
            }

            // 3. Agregar modpacks de la Whitelist que no están instalados localmente
            foreach (var wlPack in whitelist)
            {
                string targetDir = System.IO.Path.Combine(settings.InstancesFolder, wlPack.Id);
                if (!installedPaths.Contains(targetDir))
                {
                    var vm = new ModpackDetailsViewModel(wlPack.Name, $"{wlPack.ModLoader} {wlPack.GameVersion}", wlPack.Description, targetDir);
                    vm.IsInstalled = false;
                    vm.WhitelistData = wlPack;
                    vm.PlayButtonText = "DESCARGAR";
                    vm.PlayButtonColor = "#2196F3"; // Azul Premium
                    vm.PlayButtonStatusText = "Modpack oficial disponible en GitHub. Haz clic en DESCARGAR.";

                    AvailableModpacks.Add(vm);
                }
            }

            // Fallback harcodeado de pruebas si no hay nada en absoluto
            if (AvailableModpacks.Count == 0)
            {
                AvailableModpacks.Add(new ModpackDetailsViewModel("ZOMBIE APOCALIPSIS", "Versión 2.5", "El evento hardcore del fin de semana.", ""));
                AvailableModpacks.Add(new ModpackDetailsViewModel("VANILLA PLUS", "Versión 3.1", "Optimizado.", ""));
            }
        }

        private async void CheckForUpdatesAsync()
        {
            // Pequeño retardo al inicio
            await System.Threading.Tasks.Task.Delay(3000);

            try
            {
                var updateInfo = await MiLauncher.launcher.minecraft.GitHubSyncService.CheckForUpdatesAsync("1.0.0");
                if (!string.IsNullOrEmpty(updateInfo.newVersion))
                {
                    SafeDispatcher.Invoke(() =>
                    {
                        var updateWin = new UpdateWindow("1.0.0", updateInfo.newVersion, updateInfo.changelog, updateInfo.downloadUrl);
                        updateWin.Owner = System.Windows.Application.Current.MainWindow;
                        updateWin.ShowDialog();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al buscar actualizaciones en segundo plano: {ex.Message}");
            }
        }

        public void ResetSkinsState()
        {
            if (_skinsViewModel != null)
            {
                _skinsViewModel.IsEditing = false;
            }
        }
    }

    public class HomeViewModel : ViewModelBase 
    {
        public string WelcomeMessage => "Bienvenido al Launcher";

        private string _importProgressMessage = "";
        public string ImportProgressMessage
        {
            get => _importProgressMessage;
            set => SetProperty(ref _importProgressMessage, value);
        }

        private bool _isImporting = false;
        public bool IsImporting
        {
            get => _isImporting;
            set 
            {
                if (SetProperty(ref _isImporting, value))
                {
                    OnPropertyChanged(nameof(IsNotImporting));
                }
            }
        }

        public bool IsNotImporting => !IsImporting;

        public ICommand ImportMrPackCommand { get; }
        private MainViewModel _mainViewModel;

        public HomeViewModel(MainViewModel mainVm)
        {
            _mainViewModel = mainVm;
            ImportMrPackCommand = new RelayCommand(async _ => 
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Modrinth Modpack (*.mrpack)|*.mrpack|Todos los archivos (*.*)|*.*",
                    Title = "Selecciona el archivo .mrpack"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsImporting = true;
                    ImportProgressMessage = "Iniciando importación...";
                    
                    try
                    {
                        // Para simplificar, le pediremos el nombre al usuario con un input rápido, o usaremos el nombre del archivo sin extensión
                        string instanceName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);

                        await MiLauncher.launcher.modplatform.ModpackInstaller.InstallMrPackAsync(
                            dialog.FileName, 
                            instanceName, 
                            (msg, progress) => 
                            {
                                SafeDispatcher.Invoke(() => 
                                {
                                    ImportProgressMessage = $"{msg} ({progress:F1}%)";
                                });
                            });

                        ImportProgressMessage = "¡Importación exitosa!";
                        System.Windows.MessageBox.Show("Modpack importado exitosamente como: " + instanceName, "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        
                        // Recargar la lista de modpacks en la UI
                        SafeDispatcher.Invoke(() => 
                        {
                            _mainViewModel.LoadInstances();
                        });
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error al importar el modpack: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        ImportProgressMessage = "Error en la importación.";
                    }
                    finally
                    {
                        IsImporting = false;
                    }
                }
            });
        }
    }

    public class ModpackDetailsViewModel : ViewModelBase
    {
        public string Title { get; }
        public string Version { get; }
        public string Description { get; }
        
        private System.Windows.Media.ImageSource _backgroundImagePath;
        public System.Windows.Media.ImageSource BackgroundImagePath
        {
            get => _backgroundImagePath;
            private set => SetProperty(ref _backgroundImagePath, value);
        }

        private bool _isForceClosing = false;

        public bool IsGameRunning => _isGameRunning;
        private string _instancePath;

        public bool IsInstalled { get; set; } = true;
        public bool IsDownloading { get; set; } = false;
        public MiLauncher.launcher.minecraft.WhitelistedModpack? WhitelistData { get; set; } = null;

        private string _playButtonText = "JUGAR";
        public string PlayButtonText
        {
            get => _playButtonText;
            set => SetProperty(ref _playButtonText, value);
        }

        private string _playButtonColor = "#3C8527"; // Verde
        public string PlayButtonColor
        {
            get => _playButtonColor;
            set => SetProperty(ref _playButtonColor, value);
        }

        private double _playButtonProgress = 0;
        public double PlayButtonProgress
        {
            get => _playButtonProgress;
            set => SetProperty(ref _playButtonProgress, value);
        }

        private string _playButtonStatusText = "";
        public string PlayButtonStatusText
        {
            get => _playButtonStatusText;
            set => SetProperty(ref _playButtonStatusText, value);
        }

        private bool _isGameRunning = false;
        private MiLauncher.launcher.launch.LaunchController _currentLauncher;

        public void RefreshLocalAssets()
        {
            if (string.IsNullOrEmpty(_instancePath))
            {
                BackgroundImagePath = null;
                OnPropertyChanged(nameof(CustomIconSource));
                return;
            }
            string bgJpg = System.IO.Path.Combine(_instancePath, "background.jpg");
            string bgPng = System.IO.Path.Combine(_instancePath, "background.png");

            if (System.IO.File.Exists(bgJpg))
                BackgroundImagePath = LoadImageWithoutLock(bgJpg);
            else if (System.IO.File.Exists(bgPng))
                BackgroundImagePath = LoadImageWithoutLock(bgPng);
            else
            {
                // Fallback al fondo por defecto del launcher en la raíz de ejecución
                string defaultBg = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "background.png");
                if (System.IO.File.Exists(defaultBg))
                    BackgroundImagePath = LoadImageWithoutLock(defaultBg);
                else
                    BackgroundImagePath = null;
            }
            OnPropertyChanged(nameof(CustomIconSource));
        }

        public ModpackDetailsViewModel(string title, string version, string description, string instancePath)
        {
            Title = title;
            Version = version;
            Description = description;
            _instancePath = instancePath;

            // Search for local background fallback
            RefreshLocalAssets();

            OpenSettingsCommand = new RelayCommand(_ => 
            {
                if (!IsInstalled)
                {
                    System.Windows.MessageBox.Show("Primero debes descargar el modpack para poder configurar sus ajustes.", "Ajustes de Instancia", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }
                var window = new MiLauncher.launcher.ui.ModpackSettingsWindow(instancePath);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.ShowDialog();
            });

            PlayCommand = new RelayCommand(async _ => 
            {
                if (!IsInstalled)
                {
                    if (IsDownloading) return;
                    if (WhitelistData == null)
                    {
                        System.Windows.MessageBox.Show("No se encontraron datos de descarga oficiales para este modpack.", "Error de Descarga", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    IsDownloading = true;
                    PlayButtonText = "DESCARGANDO...";
                    PlayButtonColor = "#A0A0A0"; // Gris
                    
                    try
                    {
                        await System.Threading.Tasks.Task.Run(async () =>
                        {
                            await MiLauncher.launcher.minecraft.GitHubSyncService.DownloadModpackAsync(WhitelistData, _instancePath, (status, progress) =>
                            {
                                SafeDispatcher.Invoke(() =>
                                {
                                    PlayButtonStatusText = status;
                                    PlayButtonProgress = progress;
                                });
                            });
                        });
                        
                        IsInstalled = true;
                        IsDownloading = false;
                        PlayButtonText = "JUGAR";
                        PlayButtonColor = "#3C8527"; // Verde
                        PlayButtonStatusText = "¡Instalación completada! Listo para jugar.";
                        
                        RefreshLocalAssets();
                        LoadMetadata();
                        
                        // Recargar la lista principal en segundo plano
                        SafeDispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.LoadInstances();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        IsDownloading = false;
                        PlayButtonText = "DESCARGAR";
                        PlayButtonColor = "#2196F3"; // Azul
                        PlayButtonStatusText = "Error en descarga: " + ex.Message;
                        System.Windows.MessageBox.Show($"Ocurrió un error al descargar el modpack: {ex.Message}", "Error de Descarga", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                    return;
                }

                if (_isGameRunning)
                {
                    _isForceClosing = true;
                    if (_detectedPid != 0)
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                using (var proc = System.Diagnostics.Process.GetProcessById(_detectedPid))
                                {
                                    proc.Kill();
                                    proc.WaitForExit(3000);
                                }
                            }
                            catch (Exception ex)
                            {
                                SafeDispatcher.Invoke(() =>
                                {
                                    System.Windows.MessageBox.Show("No se pudo forzar el cierre del juego: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                });
                            }
                        });
                    }
                    else if (_currentLauncher != null)
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                _currentLauncher.KillGame();
                            }
                            catch { }
                        });
                    }
                    return;
                }

                // 1. Verificación rápida de whitelist/autorización y actualización
                if (WhitelistData != null)
                {
                    PlayButtonText = "VERIFICANDO...";
                    PlayButtonColor = "#A0A0A0"; // Gris
                    PlayButtonStatusText = "Verificando whitelist de jugadores...";
                    PlayButtonProgress = 20;

                    bool authorized = false;
                    try
                    {
                        // Verificación rápida con 2 segundos de timeout
                        var authTask = MiLauncher.launcher.minecraft.GitHubSyncService.IsPlayerAuthorizedAsync(WhitelistData.Id);
                        var delayTask = Task.Delay(2000);
                        var completedTask = await Task.WhenAny(authTask, delayTask);
                        if (completedTask == authTask)
                        {
                            authorized = await authTask;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Tiempo de espera agotado al verificar la whitelist. Por favor, comprueba tu conexión a internet.", "Error de Whitelist", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            PlayButtonText = "JUGAR";
                            PlayButtonColor = "#3C8527";
                            PlayButtonStatusText = "Error: Tiempo de espera agotado al verificar whitelist.";
                            PlayButtonProgress = 0;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error al verificar la whitelist: " + ex.Message, "Error de Whitelist", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        PlayButtonText = "JUGAR";
                        PlayButtonColor = "#3C8527";
                        PlayButtonStatusText = "Error de conexión al verificar whitelist.";
                        PlayButtonProgress = 0;
                        return;
                    }

                    if (!authorized)
                    {
                        System.Windows.MessageBox.Show("No estás registrado en la whitelist para este modpack. Contacta con el administrador.", "Acceso Denegado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        PlayButtonText = "JUGAR";
                        PlayButtonColor = "#3C8527";
                        PlayButtonStatusText = "Acceso Denegado: No estás en la whitelist.";
                        PlayButtonProgress = 0;
                        return;
                    }

                    // 2. Comprobar actualización obligatoria
                    PlayButtonStatusText = "Comprobando actualizaciones del modpack...";
                    PlayButtonProgress = 50;

                    string remoteVersion = WhitelistData.Version;
                    try
                    {
                        var wlTask = MiLauncher.launcher.minecraft.GitHubSyncService.GetWhitelistAsync();
                        var delayTask = Task.Delay(2000);
                        var completedTask = await Task.WhenAny(wlTask, delayTask);
                        if (completedTask == wlTask)
                        {
                            var remoteWhitelist = await wlTask;
                            var match = remoteWhitelist.FirstOrDefault(m => string.Equals(m.Id, WhitelistData.Id, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                remoteVersion = match.Version;
                                // Sincronizar cache local para mantenerla al día
                                string settingsFolder = MiLauncher.launcher.settings.SettingsManager.LoadSettings().InstancesFolder;
                                string cachePath = System.IO.Path.Combine(settingsFolder, "whitelist_cache.json");
                                string json = System.Text.Json.JsonSerializer.Serialize(remoteWhitelist, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                                System.IO.File.WriteAllText(cachePath, json);
                            }
                        }
                    }
                    catch { }

                    string localVersion = "0.0.0";
                    var localConfig = MiLauncher.launcher.settings.InstanceConfig.Load(_instancePath);
                    if (localConfig != null && !string.IsNullOrEmpty(localConfig.Version))
                    {
                        localVersion = localConfig.Version;
                    }

                    if (localVersion != remoteVersion)
                    {
                        System.Windows.MessageBox.Show($"Hay una nueva versión obligatoria disponible ({remoteVersion}). El modpack se actualizará automáticamente.", "Actualización Obligatoria", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                        IsDownloading = true;
                        PlayButtonText = "ACTUALIZANDO...";
                        PlayButtonColor = "#A0A0A0"; // Gris

                        try
                        {
                            // Actualizar la versión en nuestro objeto temporal
                            WhitelistData.Version = remoteVersion;

                            await System.Threading.Tasks.Task.Run(async () =>
                            {
                                await MiLauncher.launcher.minecraft.GitHubSyncService.DownloadModpackAsync(WhitelistData, _instancePath, (status, progress) =>
                                {
                                    SafeDispatcher.Invoke(() =>
                                    {
                                        PlayButtonStatusText = status;
                                        PlayButtonProgress = progress;
                                    });
                                });
                            });

                            IsInstalled = true;
                            IsDownloading = false;
                            PlayButtonText = "JUGAR";
                            PlayButtonColor = "#3C8527"; // Verde
                            PlayButtonStatusText = "¡Actualización completada! Listo para jugar.";
                            PlayButtonProgress = 0;

                            RefreshLocalAssets();
                            LoadMetadata();

                            // Recargar la lista principal en segundo plano
                            SafeDispatcher.Invoke(() =>
                            {
                                if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                                {
                                    mainVm.LoadInstances();
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            IsDownloading = false;
                            PlayButtonText = "JUGAR";
                            PlayButtonColor = "#3C8527";
                            PlayButtonStatusText = "Error en actualización: " + ex.Message;
                            PlayButtonProgress = 0;
                            System.Windows.MessageBox.Show($"Ocurrió un error al actualizar el modpack: {ex.Message}", "Error de Actualización", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
                        return;
                    }
                }

                _isGameRunning = true;
                PlayButtonProgress = 0;
                PlayButtonStatusText = "";
                PlayButtonText = "INICIANDO...";
                PlayButtonColor = "#A0A0A0"; // Gris

                var settings = MiLauncher.launcher.settings.SettingsManager.LoadSettings();
                MiLauncher.launcher.ui.ConsoleWindow consoleWindow = null;
                if (settings.ShowConsole)
                {
                    consoleWindow = new MiLauncher.launcher.ui.ConsoleWindow();
                    consoleWindow.Show();
                }

                _currentLauncher = new MiLauncher.launcher.launch.LaunchController();

                _currentLauncher.GameStarted += (s, ev) => 
                {
                    SafeDispatcher.Invoke(() => 
                    {
                        PlayButtonProgress = 0;
                        PlayButtonStatusText = "";
                        PlayButtonText = "FORZAR CIERRE";
                        PlayButtonColor = "#AA3333"; // Rojo

                        if (settings.LauncherLaunchBehavior == 1) // Cerrar launcher -> Ahora solo ocultamos en segundo plano
                        {
                            System.Windows.Application.Current.MainWindow?.Hide();
                        }
                        else if (settings.LauncherLaunchBehavior == 2) // Ocultar launcher
                        {
                            System.Windows.Application.Current.MainWindow?.Hide();
                        }
                    });
                };

                _currentLauncher.FileChanged += (s, e) =>
                {
                    SafeDispatcher.Invoke(() =>
                    {
                        PlayButtonStatusText = string.Format("[{0}/{1}] {2}", e.ProgressedFileCount, e.TotalFileCount, e.FileName);
                    });
                };

                _currentLauncher.ProgressChanged += (s, e) =>
                {
                    SafeDispatcher.Invoke(() =>
                    {
                        PlayButtonProgress = e.ProgressPercentage;
                    });
                };

                _currentLauncher.LogReceived += (s, log) =>
                {
                    MiLauncher.launcher.tools.Logger.LogDirect(log);
                    SafeDispatcher.Invoke(() => 
                    {
                        if (consoleWindow != null)
                        {
                            consoleWindow.AppendLog(log);
                        }
                    });
                };

                _currentLauncher.GameExited += (s, ev) => 
                {
                    SafeDispatcher.Invoke(() => 
                    {
                        if (!_isGameRunning) return;
                        _isGameRunning = false;
                        PlayButtonText = "JUGAR";
                        PlayButtonColor = "#3C8527"; // Volver a Verde

                        bool wasForced = _isForceClosing;
                        _isForceClosing = false;

                        if (wasForced)
                        {
                            System.Windows.Application.Current.MainWindow?.Show();
                            System.Windows.Application.Current.MainWindow?.Activate();
                        }
                        else if (settings.CloseLauncherOnGameExit)
                        {
                            System.Windows.Application.Current.Shutdown();
                            return;
                        }

                        if (consoleWindow != null)
                        {
                            if (settings.CloseConsoleOnGameExit)
                            {
                                consoleWindow.Close();
                            }
                            else
                            {
                                consoleWindow.AppendLog("\n[INFO] El proceso del juego terminó o se cerró.");
                            }
                        }

                        if (!wasForced && (settings.LauncherLaunchBehavior == 2 || settings.LauncherLaunchBehavior == 1)) // Ocultar launcher -> Mostrar de nuevo
                        {
                            System.Windows.Application.Current.MainWindow?.Show();
                            System.Windows.Application.Current.MainWindow?.Activate();
                        }
                    });
                };

                try
                {
                    string targetDir = GetGameDir();
                    await _currentLauncher.LaunchAsync(targetDir);
                }
                catch (System.Exception ex)
                {
                    SafeDispatcher.Invoke(() =>
                    {
                        _isGameRunning = false;
                        PlayButtonText = "JUGAR";
                        PlayButtonColor = "#3C8527"; // Volver a Verde
                        PlayButtonStatusText = "Error al iniciar: " + ex.Message;
                        System.Windows.MessageBox.Show("Error al iniciar el juego: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        if (consoleWindow != null)
                        {
                            try { consoleWindow.Close(); } catch { }
                        }
                    });
                }
            });

            LoadMetadata();
            StartMonitoring();
        }

        // Metadata customizations
        private ModpackMetadata _metadata = new ModpackMetadata();

        public void LoadMetadata()
        {
            try
            {
                if (!string.IsNullOrEmpty(_instancePath))
                {
                    string path = System.IO.Path.Combine(_instancePath, "metadata.json");
                    if (System.IO.File.Exists(path))
                    {
                        string json = System.IO.File.ReadAllText(path);
                        _metadata = System.Text.Json.JsonSerializer.Deserialize<ModpackMetadata>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ModpackMetadata();
                    }
                    else if (WhitelistData != null)
                    {
                        _metadata = new ModpackMetadata
                        {
                            HideIcon = false,
                            CustomIconPath = "custom_icon.png",
                            HideVersionList = false,
                            VersionListText = $"{WhitelistData.ModLoader} {WhitelistData.GameVersion} [Official]",
                            HideDescription = false,
                            DescriptionText = WhitelistData.Description,
                            HideTitle = false,
                            TitleText = WhitelistData.Name,
                            TitleColor = WhitelistData.TitleColor,
                            TitleFontSize = WhitelistData.TitleFontSize,
                            TitleFontFamily = WhitelistData.TitleFontFamily,
                            HideLoader = false,
                            LoaderText = $"{WhitelistData.ModLoader} {WhitelistData.LoaderVersion}"
                        };
                    }
                    else
                    {
                        _metadata = new ModpackMetadata();
                    }
                }
                else if (WhitelistData != null)
                {
                    _metadata = new ModpackMetadata
                    {
                        HideIcon = false,
                        CustomIconPath = "custom_icon.png",
                        HideVersionList = false,
                        VersionListText = $"{WhitelistData.ModLoader} {WhitelistData.GameVersion} [Official]",
                        HideDescription = false,
                        DescriptionText = WhitelistData.Description,
                        HideTitle = false,
                        TitleText = WhitelistData.Name,
                        TitleColor = WhitelistData.TitleColor,
                        TitleFontSize = WhitelistData.TitleFontSize,
                        TitleFontFamily = WhitelistData.TitleFontFamily,
                        HideLoader = false,
                        LoaderText = $"{WhitelistData.ModLoader} {WhitelistData.LoaderVersion}"
                    };
                }
                else
                {
                    _metadata = new ModpackMetadata();
                }
            }
            catch
            {
                _metadata = new ModpackMetadata();
            }

            OnPropertyChanged(nameof(CustomTitle));
            OnPropertyChanged(nameof(TitleVisibility));
            OnPropertyChanged(nameof(TitleColorBrush));
            OnPropertyChanged(nameof(TitleFontSize));
            OnPropertyChanged(nameof(TitleFontFamily));
            OnPropertyChanged(nameof(CustomVersion));
            OnPropertyChanged(nameof(VersionListVisibility));
            OnPropertyChanged(nameof(CustomDescription));
            OnPropertyChanged(nameof(DescriptionVisibility));
            OnPropertyChanged(nameof(LoaderText));
            OnPropertyChanged(nameof(LoaderVisibility));
            OnPropertyChanged(nameof(IconVisibility));
            OnPropertyChanged(nameof(CustomIconVisibility));
            OnPropertyChanged(nameof(DefaultIconVisibility));
            OnPropertyChanged(nameof(CustomIconSource));
        }

        public string CustomTitle => !_metadata.HideTitle && !string.IsNullOrEmpty(_metadata.TitleText) ? _metadata.TitleText : Title;
        public System.Windows.Visibility TitleVisibility => _metadata.HideTitle ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public System.Windows.Media.Brush TitleColorBrush
        {
            get
            {
                if (!string.IsNullOrEmpty(_metadata.TitleColor))
                {
                    try
                    {
                        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_metadata.TitleColor);
                        return new System.Windows.Media.SolidColorBrush(color);
                    }
                    catch { }
                }
                return System.Windows.Media.Brushes.White;
            }
        }
        public double TitleFontSize => _metadata.TitleFontSize > 0 ? _metadata.TitleFontSize : 54;
        public System.Windows.Media.FontFamily TitleFontFamily
        {
            get
            {
                if (!string.IsNullOrEmpty(_metadata.TitleFontFamily))
                {
                    try
                    {
                        return new System.Windows.Media.FontFamily(_metadata.TitleFontFamily);
                    }
                    catch { }
                }
                return new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "./Resources/Fonts/#Minecraft");
            }
        }
        public string CustomVersion => !string.IsNullOrEmpty(_metadata.VersionListText) ? _metadata.VersionListText : Version;
        public System.Windows.Visibility VersionListVisibility => _metadata.HideVersionList ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public string CustomDescription => !string.IsNullOrEmpty(_metadata.DescriptionText) ? _metadata.DescriptionText : Description;
        public System.Windows.Visibility DescriptionVisibility => _metadata.HideDescription ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public string LoaderText => !string.IsNullOrEmpty(_metadata.LoaderText) ? _metadata.LoaderText : Version;
        public System.Windows.Visibility LoaderVisibility => _metadata.HideLoader ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public System.Windows.Visibility IconVisibility => _metadata.HideIcon ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public System.Windows.Visibility CustomIconVisibility =>
            !_metadata.HideIcon && CustomIconSource != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Visibility DefaultIconVisibility =>
            !_metadata.HideIcon && CustomIconSource == null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public System.Windows.Media.ImageSource CustomIconSource
        {
            get
            {
                if (string.IsNullOrEmpty(_metadata.CustomIconPath) || string.IsNullOrEmpty(_instancePath)) return null;
                string fullPath = _metadata.CustomIconPath;
                if (!System.IO.Path.IsPathRooted(fullPath))
                {
                    fullPath = System.IO.Path.Combine(_instancePath, fullPath);
                }
                if (System.IO.File.Exists(fullPath))
                {
                    return LoadImageWithoutLock(fullPath);
                }
                return null;
            }
        }

        public ICommand OpenSettingsCommand { get; }
        public ICommand PlayCommand { get; }

        private int _detectedPid = 0;
        private System.Windows.Threading.DispatcherTimer? _monitorTimer;

        private string GetGameDir()
        {
            return string.IsNullOrEmpty(_instancePath) ? "C:\\Users\\VERONICA\\Documents\\universidad1\\launcher minecraft\\TestInstance" : _instancePath;
        }

        private void StartMonitoring()
        {
            _monitorTimer = new System.Windows.Threading.DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromSeconds(1.5);
            _monitorTimer.Tick += MonitorTimer_Tick;
            _monitorTimer.Start();
        }

        private void MonitorTimer_Tick(object? sender, System.EventArgs e)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string targetDir = GetGameDir();
                    int foundPid = 0;

                    using (var searcher = new System.Management.ManagementObjectSearcher(
                        "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'java.exe' OR Name = 'javaw.exe'"))
                    {
                        foreach (System.Management.ManagementObject obj in searcher.Get())
                        {
                            var pidObj = obj["ProcessId"];
                            var cmdLineObj = obj["CommandLine"];
                            if (pidObj != null && cmdLineObj != null)
                            {
                                int pid = Convert.ToInt32(pidObj);
                                string cmdLine = cmdLineObj.ToString() ?? "";
                                if (cmdLine.Contains(targetDir, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    foundPid = pid;
                                    break;
                                }
                            }
                        }
                    }

                    int currentPid = foundPid;
                    SafeDispatcher.Invoke(() =>
                    {
                        if (currentPid != 0)
                        {
                            _detectedPid = currentPid;
                            if (!_isGameRunning || PlayButtonText != "FORZAR CIERRE")
                            {
                                _isGameRunning = true;
                                OnPropertyChanged(nameof(IsGameRunning));
                                
                                PlayButtonText = "FORZAR CIERRE";
                                PlayButtonColor = "#AA3333"; // Rojo
                                PlayButtonProgress = 0;
                                PlayButtonStatusText = "";
                            }
                        }
                        else
                        {
                            // No matching process found
                            if (_isGameRunning && PlayButtonText == "FORZAR CIERRE")
                            {
                                _isGameRunning = false;
                                OnPropertyChanged(nameof(IsGameRunning));
                                _detectedPid = 0;
                                PlayButtonText = "JUGAR";
                                PlayButtonColor = "#3C8527"; // Verde
                                PlayButtonProgress = 0;
                                PlayButtonStatusText = "";

                                bool wasForced = _isForceClosing;
                                _isForceClosing = false;

                                if (wasForced)
                                {
                                    System.Windows.Application.Current.MainWindow?.Show();
                                    System.Windows.Application.Current.MainWindow?.Activate();
                                }
                                else
                                {
                                    // Si el comportamiento del launcher es ocultarse, al cerrarse el juego lo volvemos a mostrar
                                    var settings = MiLauncher.launcher.settings.SettingsManager.LoadSettings();
                                    if (settings.CloseLauncherOnGameExit)
                                    {
                                        System.Windows.Application.Current.Shutdown();
                                        return;
                                    }
                                    if (settings.LauncherLaunchBehavior == 2 || settings.LauncherLaunchBehavior == 1)
                                    {
                                        System.Windows.Application.Current.MainWindow?.Show();
                                        System.Windows.Application.Current.MainWindow?.Activate();
                                    }
                                }
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error in WMI monitor: " + ex.Message);
                }
            });
        }

        public string InstancePath => _instancePath;

        public void ClearAssetsBeforeDelete()
        {
            BackgroundImagePath = null;
            _metadata.CustomIconPath = "";
            OnPropertyChanged(nameof(CustomIconSource));
            OnPropertyChanged(nameof(CustomIconVisibility));
            OnPropertyChanged(nameof(DefaultIconVisibility));
        }

        public static System.Windows.Media.ImageSource LoadImageWithoutLock(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return null;
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }
    }

    public static class SafeDispatcher
    {
        public static void Invoke(System.Action action)
        {
            try
            {
                if (System.Windows.Application.Current == null) return;
                var dispatcher = System.Windows.Application.Current.Dispatcher;
                if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;
                dispatcher.Invoke(action);
            }
            catch
            {
                // Ignore dispatch errors during shutdown/app cancellation
            }
        }
    }

    public class ModpackMetadata
    {
        public bool HideIcon { get; set; } = false;
        public string CustomIconPath { get; set; } = "";
        public bool HideVersionList { get; set; } = false;
        public string VersionListText { get; set; } = "";
        public bool HideDescription { get; set; } = false;
        public string DescriptionText { get; set; } = "";
        public bool HideTitle { get; set; } = false;
        public string TitleText { get; set; } = "";
        public string TitleColor { get; set; } = "";
        public double TitleFontSize { get; set; } = 0;
        public string TitleFontFamily { get; set; } = "";
        public bool HideLoader { get; set; } = false;
        public string LoaderText { get; set; } = "";
    }
}
