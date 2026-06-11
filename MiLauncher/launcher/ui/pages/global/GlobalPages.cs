using MaterialDesignThemes.Wpf;
using MiLauncher.launcher.settings;
using System.Linq;
using System.Windows.Media;
using MiLauncher.launcher.ui;
using Color = System.Windows.Media.Color;

namespace MiLauncher.launcher.ui.pages.global
{
    public class LauncherPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Launcher";
        public override PackIconKind Icon => PackIconKind.RocketLaunchOutline;
        public override string Description => "Ajustes de comportamiento del launcher y carpetas principales.";
        
        public string InstancesFolder 
        { 
            get => SettingsManager.LoadSettings().InstancesFolder; 
            set { var c = SettingsManager.LoadSettings(); c.InstancesFolder = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }
        public string IconsFolder 
        { 
            get => SettingsManager.LoadSettings().IconsFolder; 
            set { var c = SettingsManager.LoadSettings(); c.IconsFolder = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }
        public bool CloseAfterLaunch 
        { 
            get => SettingsManager.LoadSettings().CloseAfterLaunch; 
            set { var c = SettingsManager.LoadSettings(); c.CloseAfterLaunch = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        public int ResolutionWidth
        {
            get => SettingsManager.LoadSettings().ResolutionWidth;
            set
            {
                var c = SettingsManager.LoadSettings();
                c.ResolutionWidth = value;
                SettingsManager.SaveSettings(c);
                OnPropertyChanged();
            }
        }

        public int ResolutionHeight
        {
            get => SettingsManager.LoadSettings().ResolutionHeight;
            set
            {
                var c = SettingsManager.LoadSettings();
                c.ResolutionHeight = value;
                SettingsManager.SaveSettings(c);
                OnPropertyChanged();
            }
        }

        public bool ShowConsole 
        { 
            get => SettingsManager.LoadSettings().ShowConsole; 
            set { var c = SettingsManager.LoadSettings(); c.ShowConsole = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        public bool MaximizeWindow 
        { 
            get => SettingsManager.LoadSettings().MaximizeWindow; 
            set { var c = SettingsManager.LoadSettings(); c.MaximizeWindow = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        public string PreLaunchCommand 
        { 
            get => SettingsManager.LoadSettings().PreLaunchCommand; 
            set { var c = SettingsManager.LoadSettings(); c.PreLaunchCommand = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        public string PostExitCommand 
        { 
            get => SettingsManager.LoadSettings().PostExitCommand; 
            set { var c = SettingsManager.LoadSettings(); c.PostExitCommand = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        public bool Fullscreen 
        { 
            get => SettingsManager.LoadSettings().Fullscreen; 
            set { var c = SettingsManager.LoadSettings(); c.Fullscreen = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        public int LauncherLaunchBehavior
        {
            get => SettingsManager.LoadSettings().LauncherLaunchBehavior;
            set { var c = SettingsManager.LoadSettings(); c.LauncherLaunchBehavior = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); }
        }



        public bool CloseConsoleOnGameExit
        {
            get => SettingsManager.LoadSettings().CloseConsoleOnGameExit;
            set { var c = SettingsManager.LoadSettings(); c.CloseConsoleOnGameExit = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); }
        }

        public bool CloseLauncherOnGameExit
        {
            get => SettingsManager.LoadSettings().CloseLauncherOnGameExit;
            set { var c = SettingsManager.LoadSettings(); c.CloseLauncherOnGameExit = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); }
        }

        public bool AutoConnect
        {
            get => SettingsManager.LoadSettings().AutoConnect;
            set { var c = SettingsManager.LoadSettings(); c.AutoConnect = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); }
        }

        public string ServerAddress
        {
            get => SettingsManager.LoadSettings().ServerAddress;
            set { var c = SettingsManager.LoadSettings(); c.ServerAddress = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); }
        }

        public int ServerPort
        {
            get => SettingsManager.LoadSettings().ServerPort;
            set { var c = SettingsManager.LoadSettings(); c.ServerPort = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); }
        }

        public System.Windows.Input.ICommand BrowseInstancesCommand { get; }
        public System.Windows.Input.ICommand OpenInstancesCommand { get; }
        public System.Windows.Input.ICommand BrowseIconsCommand { get; }
        public System.Windows.Input.ICommand OpenIconsCommand { get; }
        public System.Windows.Input.ICommand ResetDefaultsCommand { get; }

        public LauncherPageViewModel()
        {
            BrowseInstancesCommand = new RelayCommand(_ =>
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog();
                dialog.Title = "Seleccionar carpeta de Instancias";
                if (dialog.ShowDialog() == true)
                {
                    InstancesFolder = dialog.FolderName;
                }
            });

            OpenInstancesCommand = new RelayCommand(_ =>
            {
                OpenFolderInExplorer(InstancesFolder);
            });

            BrowseIconsCommand = new RelayCommand(_ =>
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog();
                dialog.Title = "Seleccionar carpeta de Íconos";
                if (dialog.ShowDialog() == true)
                {
                    IconsFolder = dialog.FolderName;
                }
            });

            OpenIconsCommand = new RelayCommand(_ =>
            {
                OpenFolderInExplorer(IconsFolder);
            });

            ResetDefaultsCommand = new RelayCommand(_ =>
            {
                var result = System.Windows.MessageBox.Show(
                    "¿Estás seguro de que deseas restablecer todos los ajustes globales a sus valores por defecto? Se perderán las rutas y configuraciones personalizadas.",
                    "Restablecer Ajustes",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var defaultConfig = new AppConfig();
                    
                    var current = SettingsManager.LoadSettings();
                    defaultConfig.OfflineAccounts = current.OfflineAccounts;
                    defaultConfig.SelectedAccount = current.SelectedAccount;
                    
                    SettingsManager.SaveSettings(defaultConfig);

                    OnPropertyChanged(nameof(InstancesFolder));
                    OnPropertyChanged(nameof(IconsFolder));
                    OnPropertyChanged(nameof(CloseAfterLaunch));
                    OnPropertyChanged(nameof(ShowConsole));
                    OnPropertyChanged(nameof(MaximizeWindow));
                    OnPropertyChanged(nameof(PreLaunchCommand));
                    OnPropertyChanged(nameof(PostExitCommand));
                    OnPropertyChanged(nameof(ResolutionWidth));
                    OnPropertyChanged(nameof(ResolutionHeight));
                    OnPropertyChanged(nameof(Fullscreen));
                    OnPropertyChanged(nameof(LauncherLaunchBehavior));
                    OnPropertyChanged(nameof(CloseConsoleOnGameExit));
                    OnPropertyChanged(nameof(CloseLauncherOnGameExit));
                    OnPropertyChanged(nameof(AutoConnect));
                    OnPropertyChanged(nameof(ServerAddress));
                    OnPropertyChanged(nameof(ServerPort));

                    MiLauncher.launcher.tools.ThemeManager.ApplyColors(defaultConfig.PrimaryColor, defaultConfig.SecondaryColor, defaultConfig.BorderColor, defaultConfig.TextColor);

                    System.Windows.MessageBox.Show("Ajustes restablecidos correctamente.", "Ajustes", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            });
        }

        private void OpenFolderInExplorer(string path)
        {
            try
            {
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    public class JavaPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Java";
        public override PackIconKind Icon => PackIconKind.CoffeeOutline;
        public override string Description => "Configuración de memoria y ruta del ejecutable Java.";

        public string JavaPath 
        { 
            get => SettingsManager.LoadSettings().JavaPath; 
            set { var c = SettingsManager.LoadSettings(); c.JavaPath = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }
        public int MinMem 
        { 
            get => SettingsManager.LoadSettings().MinMem; 
            set { var c = SettingsManager.LoadSettings(); c.MinMem = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }
        public int MaxMem 
        { 
            get => SettingsManager.LoadSettings().MaxMem; 
            set { var c = SettingsManager.LoadSettings(); c.MaxMem = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }
        public int PermGen 
        { 
            get => SettingsManager.LoadSettings().PermGen; 
            set { var c = SettingsManager.LoadSettings(); c.PermGen = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }
        public string JvmArgs 
        { 
            get => SettingsManager.LoadSettings().JvmArgs; 
            set { var c = SettingsManager.LoadSettings(); c.JvmArgs = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }
        public string WrapperCommand 
        { 
            get => SettingsManager.LoadSettings().WrapperCommand; 
            set { var c = SettingsManager.LoadSettings(); c.WrapperCommand = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        public System.Windows.Input.ICommand AutoDetectCommand { get; }
        public System.Windows.Input.ICommand BrowseJavaCommand { get; }
        public System.Windows.Input.ICommand OpenJavaFolderCommand { get; }

        public JavaPageViewModel()
        {
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
        }
    }

    public class AccountListPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Cuentas";
        public override PackIconKind Icon => PackIconKind.AccountOutline;
        public override string Description => "Gestiona tus cuentas de Microsoft y offline.";

        public System.Collections.ObjectModel.ObservableCollection<string> OfflineAccounts { get; }
        
        public string NewAccountName { get; set; } = "";
        
        public string SecureID => SettingsManager.GetOrCreateOfflineID();
        
        public string SelectedAccount
        {
            get => SettingsManager.LoadSettings().SelectedAccount;
            set
            {
                var config = SettingsManager.LoadSettings();
                config.SelectedAccount = value;
                SettingsManager.SaveSettings(config);
                OnPropertyChanged();
            }
        }
        
        public System.Windows.Input.ICommand AddOfflineAccountCommand { get; }
        public System.Windows.Input.ICommand RemoveAccountCommand { get; }
        public System.Windows.Input.ICommand SelectAccountCommand { get; }
        public System.Windows.Input.ICommand CopySecureIDCommand { get; }
        public System.Windows.Input.ICommand ResetSecureIDCommand { get; }

        public AccountListPageViewModel()
        {
            var config = SettingsManager.LoadSettings();
            OfflineAccounts = new System.Collections.ObjectModel.ObservableCollection<string>(config.OfflineAccounts);
            
            AddOfflineAccountCommand = new RelayCommand(_ => 
            {
                if (!string.IsNullOrWhiteSpace(NewAccountName) && !OfflineAccounts.Contains(NewAccountName))
                {
                    OfflineAccounts.Add(NewAccountName);
                    if (OfflineAccounts.Count == 1) 
                    {
                        SelectedAccount = NewAccountName;
                        if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                        {
                            mainVm.CurrentAccountName = NewAccountName;
                        }
                    }
                    SaveAccounts();
                    NewAccountName = "";
                    OnPropertyChanged(nameof(NewAccountName));
                }
            });

            RemoveAccountCommand = new RelayCommand(acc => 
            {
                if (acc is string accountName)
                {
                    OfflineAccounts.Remove(accountName);
                    if (SelectedAccount == accountName)
                    {
                        SelectedAccount = OfflineAccounts.FirstOrDefault() ?? "Sin Cuenta";
                    }
                    SaveAccounts();
                }
            });

            SelectAccountCommand = new RelayCommand(acc => 
            {
                if (acc is string accountName)
                {
                    SelectedAccount = accountName;
                    
                    // Actualizar en vivo la barra lateral
                    if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                    {
                        mainVm.CurrentAccountName = accountName;
                    }
                }
            });

            CopySecureIDCommand = new RelayCommand(_ => 
            {
                try
                {
                    System.Windows.Clipboard.SetText(SecureID);
                    System.Windows.MessageBox.Show("¡ID Seguro copiado al portapapeles!", "Copiado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("No se pudo copiar al portapapeles: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });

            ResetSecureIDCommand = new RelayCommand(_ => 
            {
                var result = System.Windows.MessageBox.Show(
                    "¿Estás seguro de que deseas reiniciar tu ID Seguro? Tendrás que volver a ser agregado a la White List.",
                    "Confirmación de Reinicio (Paso 1 de 2)",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var doubleCheck = System.Windows.MessageBox.Show(
                        "¿Estás COMPLETAMENTE SEGURO de hacer esto? Esta acción cambiará tu ID permanentemente y no podrás entrar al servidor hasta que seas agregado a la White List de nuevo con el nuevo ID.",
                        "Confirmación de Reinicio (Paso 2 de 2)",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);

                    if (doubleCheck == System.Windows.MessageBoxResult.Yes)
                    {
                        string newID = SettingsManager.GetOrCreateOfflineID(forceReset: true);
                        OnPropertyChanged(nameof(SecureID));
                        System.Windows.MessageBox.Show("¡ID Seguro reiniciado exitosamente!\n\nNuevo ID: " + newID, "Reinicio Completado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            });
        }

        private void SaveAccounts()
        {
            var config = SettingsManager.LoadSettings();
            config.OfflineAccounts = OfflineAccounts.ToList();
            SettingsManager.SaveSettings(config);
        }
    }

    public class AppearancePageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Apariencia";
        public override PackIconKind Icon => PackIconKind.PaletteOutline;
        public override string Description => "Personaliza los colores y el estilo del launcher.";
        
        public bool UseDarkMode 
        { 
            get => SettingsManager.LoadSettings().UseDarkMode; 
            set { var c = SettingsManager.LoadSettings(); c.UseDarkMode = value; SettingsManager.SaveSettings(c); OnPropertyChanged(); } 
        }

        private string _primaryColorHex = "";
        private string _secondaryColorHex = "";
        private string _borderColorHex = "";
        private string _textColorHex = "";
        private bool _isUpdatingFromRgb = false;

        private byte _primaryR;
        private byte _primaryG;
        private byte _primaryB;

        private byte _secondaryR;
        private byte _secondaryG;
        private byte _secondaryB;

        private byte _borderR;
        private byte _borderG;
        private byte _borderB;

        private byte _textR;
        private byte _textG;
        private byte _textB;

        public byte PrimaryR
        {
            get => _primaryR;
            set
            {
                if (_primaryR != value)
                {
                    _primaryR = value;
                    OnPropertyChanged();
                    UpdatePrimaryHexFromRgb();
                }
            }
        }

        public byte PrimaryG
        {
            get => _primaryG;
            set
            {
                if (_primaryG != value)
                {
                    _primaryG = value;
                    OnPropertyChanged();
                    UpdatePrimaryHexFromRgb();
                }
            }
        }

        public byte PrimaryB
        {
            get => _primaryB;
            set
            {
                if (_primaryB != value)
                {
                    _primaryB = value;
                    OnPropertyChanged();
                    UpdatePrimaryHexFromRgb();
                }
            }
        }

        public byte SecondaryR
        {
            get => _secondaryR;
            set
            {
                if (_secondaryR != value)
                {
                    _secondaryR = value;
                    OnPropertyChanged();
                    UpdateSecondaryHexFromRgb();
                }
            }
        }

        public byte SecondaryG
        {
            get => _secondaryG;
            set
            {
                if (_secondaryG != value)
                {
                    _secondaryG = value;
                    OnPropertyChanged();
                    UpdateSecondaryHexFromRgb();
                }
            }
        }

        public byte SecondaryB
        {
            get => _secondaryB;
            set
            {
                if (_secondaryB != value)
                {
                    _secondaryB = value;
                    OnPropertyChanged();
                    UpdateSecondaryHexFromRgb();
                }
            }
        }

        public byte BorderR
        {
            get => _borderR;
            set
            {
                if (_borderR != value)
                {
                    _borderR = value;
                    OnPropertyChanged();
                    UpdateBorderHexFromRgb();
                }
            }
        }

        public byte BorderG
        {
            get => _borderG;
            set
            {
                if (_borderG != value)
                {
                    _borderG = value;
                    OnPropertyChanged();
                    UpdateBorderHexFromRgb();
                }
            }
        }

        public byte BorderB
        {
            get => _borderB;
            set
            {
                if (_borderB != value)
                {
                    _borderB = value;
                    OnPropertyChanged();
                    UpdateBorderHexFromRgb();
                }
            }
        }

        public byte TextR
        {
            get => _textR;
            set
            {
                if (_textR != value)
                {
                    _textR = value;
                    OnPropertyChanged();
                    UpdateTextHexFromRgb();
                }
            }
        }

        public byte TextG
        {
            get => _textG;
            set
            {
                if (_textG != value)
                {
                    _textG = value;
                    OnPropertyChanged();
                    UpdateTextHexFromRgb();
                }
            }
        }

        public byte TextB
        {
            get => _textB;
            set
            {
                if (_textB != value)
                {
                    _textB = value;
                    OnPropertyChanged();
                    UpdateTextHexFromRgb();
                }
            }
        }

        public string PrimaryColorHex
        {
            get => _primaryColorHex;
            set
            {
                if (_primaryColorHex != value)
                {
                    _primaryColorHex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PrimaryColor));
                    if (!_isUpdatingFromRgb)
                    {
                        UpdateRgbFromPrimaryHex();
                    }
                }
            }
        }

        public string SecondaryColorHex
        {
            get => _secondaryColorHex;
            set
            {
                if (_secondaryColorHex != value)
                {
                    _secondaryColorHex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SecondaryColor));
                    if (!_isUpdatingFromRgb)
                    {
                        UpdateRgbFromSecondaryHex();
                    }
                }
            }
        }

        public string BorderColorHex
        {
            get => _borderColorHex;
            set
            {
                if (_borderColorHex != value)
                {
                    _borderColorHex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BorderColor));
                    if (!_isUpdatingFromRgb)
                    {
                        UpdateRgbFromBorderHex();
                    }
                }
            }
        }

        public string TextColorHex
        {
            get => _textColorHex;
            set
            {
                if (_textColorHex != value)
                {
                    _textColorHex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextColor));
                    if (!_isUpdatingFromRgb)
                    {
                        UpdateRgbFromTextHex();
                    }
                }
            }
        }

        private void UpdateBorderHexFromRgb()
        {
            _isUpdatingFromRgb = true;
            try
            {
                BorderColorHex = $"#{BorderR:X2}{BorderG:X2}{BorderB:X2}";
                ApplyColorsInternal();
            }
            finally
            {
                _isUpdatingFromRgb = false;
            }
        }

        private void UpdateRgbFromBorderHex()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(BorderColorHex);
                _borderR = color.R;
                _borderG = color.G;
                _borderB = color.B;
                OnPropertyChanged(nameof(BorderR));
                OnPropertyChanged(nameof(BorderG));
                OnPropertyChanged(nameof(BorderB));
                ApplyColorsInternal();
            }
            catch
            {
                // Ignorar
            }
        }

        private void UpdateTextHexFromRgb()
        {
            _isUpdatingFromRgb = true;
            try
            {
                TextColorHex = $"#{TextR:X2}{TextG:X2}{TextB:X2}";
                ApplyColorsInternal();
            }
            finally
            {
                _isUpdatingFromRgb = false;
            }
        }

        private void UpdateRgbFromTextHex()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(TextColorHex);
                _textR = color.R;
                _textG = color.G;
                _textB = color.B;
                OnPropertyChanged(nameof(TextR));
                OnPropertyChanged(nameof(TextG));
                OnPropertyChanged(nameof(TextB));
                ApplyColorsInternal();
            }
            catch
            {
                // Ignorar
            }
        }

        private void UpdatePrimaryHexFromRgb()
        {
            _isUpdatingFromRgb = true;
            try
            {
                PrimaryColorHex = $"#{PrimaryR:X2}{PrimaryG:X2}{PrimaryB:X2}";
                ApplyColorsInternal();
            }
            finally
            {
                _isUpdatingFromRgb = false;
            }
        }

        private void UpdateRgbFromPrimaryHex()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(PrimaryColorHex);
                _primaryR = color.R;
                _primaryG = color.G;
                _primaryB = color.B;
                OnPropertyChanged(nameof(PrimaryR));
                OnPropertyChanged(nameof(PrimaryG));
                OnPropertyChanged(nameof(PrimaryB));
                ApplyColorsInternal();
            }
            catch
            {
                // Ignorar formato hex inválido
            }
        }

        private void UpdateSecondaryHexFromRgb()
        {
            _isUpdatingFromRgb = true;
            try
            {
                SecondaryColorHex = $"#{SecondaryR:X2}{SecondaryG:X2}{SecondaryB:X2}";
                ApplyColorsInternal();
            }
            finally
            {
                _isUpdatingFromRgb = false;
            }
        }

        private void UpdateRgbFromSecondaryHex()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(SecondaryColorHex);
                _secondaryR = color.R;
                _secondaryG = color.G;
                _secondaryB = color.B;
                OnPropertyChanged(nameof(SecondaryR));
                OnPropertyChanged(nameof(SecondaryG));
                OnPropertyChanged(nameof(SecondaryB));
                ApplyColorsInternal();
            }
            catch
            {
                // Ignorar formato hex inválido
            }
        }

        private void ApplyColorsInternal()
        {
            var c = SettingsManager.LoadSettings();
            c.PrimaryColor = PrimaryColorHex;
            c.SecondaryColor = SecondaryColorHex;
            c.BorderColor = BorderColorHex;
            c.TextColor = TextColorHex;
            SettingsManager.SaveSettings(c);
            
            MiLauncher.launcher.tools.ThemeManager.ApplyColors(PrimaryColorHex, SecondaryColorHex, BorderColorHex, TextColorHex);
        }

        public Color PrimaryColor
        {
            get
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(PrimaryColorHex);
                }
                catch
                {
                    return Colors.Transparent;
                }
            }
        }

        public Color SecondaryColor
        {
            get
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(SecondaryColorHex);
                }
                catch
                {
                    return Colors.Transparent;
                }
            }
        }

        public Color BorderColor
        {
            get
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(BorderColorHex);
                }
                catch
                {
                    return Colors.Transparent;
                }
            }
        }

        public Color TextColor
        {
            get
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(TextColorHex);
                }
                catch
                {
                    return Colors.Transparent;
                }
            }
        }

        public System.Windows.Input.ICommand ApplyColorsCommand { get; }
        public System.Windows.Input.ICommand PresetCommand { get; }

        public AppearancePageViewModel()
        {
            var config = SettingsManager.LoadSettings();
            _primaryColorHex = config.PrimaryColor;
            _secondaryColorHex = config.SecondaryColor;
            _borderColorHex = config.BorderColor;
            if (string.IsNullOrEmpty(_borderColorHex)) _borderColorHex = "#33FFFFFF";
            _textColorHex = config.TextColor;
            if (string.IsNullOrEmpty(_textColorHex)) _textColorHex = "#FFFFFF";

            // Inicializar valores de RGB desde la configuración cargada
            try
            {
                var primary = (Color)ColorConverter.ConvertFromString(_primaryColorHex);
                _primaryR = primary.R;
                _primaryG = primary.G;
                _primaryB = primary.B;
            }
            catch
            {
                _primaryR = 23;
                _primaryG = 22;
                _primaryB = 21;
            }

            try
            {
                var secondary = (Color)ColorConverter.ConvertFromString(_secondaryColorHex);
                _secondaryR = secondary.R;
                _secondaryG = secondary.G;
                _secondaryB = secondary.B;
            }
            catch
            {
                _secondaryR = 37;
                _secondaryG = 37;
                _secondaryB = 38;
            }

            try
            {
                var border = (Color)ColorConverter.ConvertFromString(_borderColorHex);
                _borderR = border.R;
                _borderG = border.G;
                _borderB = border.B;
            }
            catch
            {
                _borderR = 51;
                _borderG = 255;
                _borderB = 255;
            }

            try
            {
                var text = (Color)ColorConverter.ConvertFromString(_textColorHex);
                _textR = text.R;
                _textG = text.G;
                _textB = text.B;
            }
            catch
            {
                _textR = 255;
                _textG = 255;
                _textB = 255;
            }

            ApplyColorsCommand = new RelayCommand(_ => 
            {
                ApplyColorsInternal();
                
                // Forzar refresco
                OnPropertyChanged(nameof(PrimaryColor));
                OnPropertyChanged(nameof(SecondaryColor));
                OnPropertyChanged(nameof(BorderColor));
                OnPropertyChanged(nameof(TextColor));
            });

            PresetCommand = new RelayCommand(presetName => 
            {
                if (presetName is string preset)
                {
                    string primary = "#171615";
                    string secondary = "#252526";
                    string border = "#33FFFFFF";
                    string text = "#FFFFFF";

                    switch (preset.ToLower())
                    {
                        case "dark":
                            primary = "#171615";
                            secondary = "#252526";
                            border = "#33FFFFFF";
                            text = "#FFFFFF";
                            break;
                        case "midnight":
                            primary = "#0a0e1a";
                            secondary = "#151b30";
                            border = "#1f2a47";
                            text = "#FFFFFF";
                            break;
                        case "amoled":
                            primary = "#000000";
                            secondary = "#111111";
                            border = "#222222";
                            text = "#FFFFFF";
                            break;
                        case "green":
                            primary = "#0d1a0f";
                            secondary = "#1b3020";
                            border = "#2d5035";
                            text = "#FFFFFF";
                            break;
                    }

                    PrimaryColorHex = primary;
                    SecondaryColorHex = secondary;
                    BorderColorHex = border;
                    TextColorHex = text;

                    ApplyColorsInternal();
                }
            });
        }
    }

    public class SystemPageViewModel : BasePageViewModel
    {
        public override string DisplayName => "Sistema";
        public override PackIconKind Icon => PackIconKind.Update;
        public override string Description => "Buscar actualizaciones del launcher y recargar la whitelist de modpacks.";

        private string _statusText = "Listo.";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private double _progressValue = 0;
        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public System.Windows.Input.ICommand CheckUpdatesCommand { get; }
        public System.Windows.Input.ICommand ReloadWhitelistCommand { get; }

        public SystemPageViewModel()
        {
            CheckUpdatesCommand = new RelayCommand(async _ =>
            {
                if (IsBusy) return;
                IsBusy = true;
                StatusText = "Buscando actualizaciones en GitHub...";
                ProgressValue = 30;
                
                var updateInfo = await MiLauncher.launcher.minecraft.GitHubSyncService.CheckForUpdatesAsync("1.0.0");
                ProgressValue = 100;
                
                if (!string.IsNullOrEmpty(updateInfo.newVersion))
                {
                    StatusText = $"Nueva actualización encontrada: {updateInfo.newVersion}";
                    IsBusy = false;

                    SafeDispatcher.Invoke(() =>
                    {
                        var updateWin = new UpdateWindow("1.0.0", updateInfo.newVersion, updateInfo.changelog, updateInfo.downloadUrl);
                        updateWin.Owner = System.Windows.Application.Current.MainWindow;
                        updateWin.ShowDialog();
                    });
                }
                else
                {
                    StatusText = "Tu launcher está actualizado (Versión v1.0.0).";
                    IsBusy = false;
                    System.Windows.MessageBox.Show("El launcher se encuentra en la versión más reciente.", "Actualizaciones", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            });

            ReloadWhitelistCommand = new RelayCommand(async _ =>
            {
                if (IsBusy) return;
                IsBusy = true;
                ProgressValue = 0;

                try
                {
                    StatusText = "Conectando con el repositorio de GitHub...";
                    ProgressValue = 20;

                    var whitelist = await MiLauncher.launcher.minecraft.GitHubSyncService.GetWhitelistAsync();
                    ProgressValue = 60;

                    if (whitelist.Count > 0)
                    {
                        StatusText = "Sincronizando whitelist con la caché local...";
                        var settings = SettingsManager.LoadSettings();
                        System.IO.Directory.CreateDirectory(settings.InstancesFolder);
                        string cachePath = System.IO.Path.Combine(settings.InstancesFolder, "whitelist_cache.json");
                        
                        string json = System.Text.Json.JsonSerializer.Serialize(whitelist, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        System.IO.File.WriteAllText(cachePath, json);
                        
                        // Si hay instancias locales asociadas, les actualizamos su metadata.json
                        foreach (var modpack in whitelist)
                        {
                            string targetDir = System.IO.Path.Combine(settings.InstancesFolder, modpack.Id);
                            if (System.IO.Directory.Exists(targetDir))
                            {
                                string metadataPath = System.IO.Path.Combine(targetDir, "metadata.json");
                                var wlMetadata = new
                                {
                                    hideIcon = false,
                                    customIconPath = "custom_icon.png",
                                    hideVersionList = false,
                                    versionListText = $"{modpack.ModLoader} {modpack.GameVersion} [Official]",
                                    hideDescription = false,
                                    descriptionText = modpack.Description,
                                    hideTitle = false,
                                    titleText = modpack.Name,
                                    titleColor = modpack.TitleColor,
                                    titleFontSize = modpack.TitleFontSize,
                                    titleFontFamily = modpack.TitleFontFamily,
                                    hideLoader = false,
                                    loaderText = $"{modpack.ModLoader} {modpack.LoaderVersion}"
                                };
                                string metaJson = System.Text.Json.JsonSerializer.Serialize(wlMetadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                                System.IO.File.WriteAllText(metadataPath, metaJson);
                            }
                        }
                        
                        ProgressValue = 80;
                        StatusText = "Recargando modpacks en el Launcher...";
                        
                        SafeDispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                            {
                                mainVm.LoadInstances();
                            }
                        });

                        ProgressValue = 100;
                        StatusText = "Whitelist sincronizada con éxito.";
                        System.Windows.MessageBox.Show("Sincronización completada. Se han obtenido las instancias oficiales autorizadas.", "Whitelist", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        ProgressValue = 100;
                        StatusText = "No se pudieron obtener modpacks de la Whitelist remota.";
                        System.Windows.MessageBox.Show("No se encontraron registros en la whitelist remota de GitHub. Revisa la conexión o configuración del repositorio.", "Whitelist", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
                catch (System.Exception ex)
                {
                    StatusText = "Error al sincronizar: " + ex.Message;
                    System.Windows.MessageBox.Show("Error durante la sincronización: " + ex.Message, "Error Whitelist", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    IsBusy = false;
                }
            });
        }
    }
}
