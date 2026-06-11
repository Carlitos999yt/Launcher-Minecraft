using CmlLib.Core;
using CmlLib.Core.Auth;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MiLauncher.launcher.tools;

using MiLauncher.launcher.net;
using MiLauncher.launcher.settings;

namespace MiLauncher.launcher.launch
{
    public class LaunchController
    {
        private Process? _gameProcess;
        public bool IsGameRunning => _gameProcess != null && !_gameProcess.HasExited;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_MAXIMIZE = 3;

        public event EventHandler? GameStarted;
        public event EventHandler? GameExited;
        public event EventHandler<string>? LogReceived;
        public event EventHandler<CmlLib.Core.Downloader.DownloadFileChangedEventArgs>? FileChanged;
        public event EventHandler<System.ComponentModel.ProgressChangedEventArgs>? ProgressChanged;

        public async Task LaunchAsync(string instancePath)
        {
            if (IsGameRunning) return;

            Directory.CreateDirectory(instancePath);
            var instanceConfig = InstanceConfig.Load(instancePath);
            var globalSettings = SettingsManager.LoadSettings();

            var path = new MinecraftPath(instancePath);
            if (instanceConfig.UseGlobalSettings)
            {
                var globalPath = new MinecraftPath(); // Apunta a %appdata%/.minecraft
                path.Versions = globalPath.Versions;
                path.Assets = globalPath.Assets;
                path.Library = globalPath.Library;
                path.Runtime = globalPath.Runtime;
            }
            
            string targetVersion = instanceConfig.GameVersion;
            if (string.IsNullOrWhiteSpace(targetVersion)) targetVersion = "1.20.1";

            // Determinar qué configuración usar
            int maxMem = instanceConfig.UseGlobalSettings ? globalSettings.MaxMem : instanceConfig.MaxMem;
            int minMem = instanceConfig.UseGlobalSettings ? globalSettings.MinMem : instanceConfig.MinMem;
            
            // Determinar qué Java usar de forma inteligente
            string javaPath = "";
            
            int reqVersion = JavaDetector.GetJavaMajorVersionForMinecraft(targetVersion);

            if (!instanceConfig.UseGlobalSettings && !string.IsNullOrWhiteSpace(instanceConfig.JavaPath) && File.Exists(instanceConfig.JavaPath))
            {
                // El jugador eligió un Java específico local para esta instancia. Validamos compatibilidad.
                int selectedVersion = JavaDetector.GetJavaMajorVersion(instanceConfig.JavaPath);
                if (selectedVersion >= reqVersion)
                {
                    javaPath = instanceConfig.JavaPath;
                }
            }
            
            if (string.IsNullOrWhiteSpace(javaPath))
            {
                // De lo contrario, intentamos usar el global si existe y es compatible con esta versión de Minecraft
                if (!string.IsNullOrWhiteSpace(globalSettings.JavaPath) && File.Exists(globalSettings.JavaPath))
                {
                    int globalVersion = JavaDetector.GetJavaMajorVersion(globalSettings.JavaPath);
                    if (globalVersion >= reqVersion)
                    {
                        javaPath = globalSettings.JavaPath;
                    }
                }
                
                // Si el global no es compatible o no existe, auto-detectamos la mejor versión para esta instancia
                if (string.IsNullOrWhiteSpace(javaPath))
                {
                    javaPath = JavaDetector.GetBestJavaPathForVersion(targetVersion) ?? "";
                }
            }

            // Si por alguna razón sigue vacío o no existe en el disco, usamos el fallback absoluto
            if (string.IsNullOrWhiteSpace(javaPath) || !File.Exists(javaPath))
            {
                javaPath = JavaDetector.GetBestJavaPath() ?? "java";
            }

            // Para poder capturar los logs, necesitamos java.exe en lugar de javaw.exe
            if (javaPath.EndsWith("javaw.exe", StringComparison.OrdinalIgnoreCase))
            {
                javaPath = javaPath.Substring(0, javaPath.Length - 5) + ".exe";
            }

            // Asegurarnos de que las versiones modernas usen Java 21 automáticamente
            if (targetVersion.StartsWith("1.20.5") || targetVersion.StartsWith("1.20.6") || targetVersion.StartsWith("1.21") || targetVersion.StartsWith("1.22"))
            {
                if (!javaPath.Contains("21") && !javaPath.Contains("22"))
                {
                    var j21Path = await MiLauncher.launcher.tools.JavaDownloader.GetOrDownloadJava21Async((status) => 
                    {
                        ProgressChanged?.Invoke(this, new System.ComponentModel.ProgressChangedEventArgs(10, status));
                    });
                    
                    if (!string.IsNullOrEmpty(j21Path))
                    {
                        javaPath = j21Path;
                    }
                }
            }

            // Si es un modpack de Fabric, intentaremos lanzar la versión de Fabric
            if (instanceConfig.ModLoader == "Fabric" && !string.IsNullOrEmpty(instanceConfig.LoaderVersion))
            {
                // Busca una versión ya instalada de Fabric en la carpeta versions
                string fabricVerName = $"fabric-loader-{instanceConfig.LoaderVersion}-{targetVersion}";
                string fabricVerPath = Path.Combine(path.Versions, fabricVerName);
                string fabricJsonPath = Path.Combine(fabricVerPath, fabricVerName + ".json");
                
                // Si el archivo JSON del perfil no existe, lo descargamos directamente de la API de Fabric
                if (!File.Exists(fabricJsonPath))
                {
                    try
                    {
                        Directory.CreateDirectory(fabricVerPath);
                        using var httpClient = new System.Net.Http.HttpClient();
                        string metaUrl = $"https://meta.fabricmc.net/v2/versions/loader/{targetVersion}/{instanceConfig.LoaderVersion}/profile/json";
                        string profileJson = await httpClient.GetStringAsync(metaUrl);
                        File.WriteAllText(fabricJsonPath, profileJson);
                        Debug.WriteLine($"Perfil de Fabric descargado en {fabricJsonPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error descargando el perfil de Fabric: {ex.Message}");
                    }
                }
                
                targetVersion = fabricVerName;
            }

            // AHORA inicializamos CmlLib para que lea la carpeta 'versions' actualizada con el nuevo json de Fabric
            var launcher = new CMLauncher(path);
            launcher.FileChanged += (e) => FileChanged?.Invoke(this, e);
            launcher.ProgressChanged += (s, e) => ProgressChanged?.Invoke(this, e);

            int resWidth = instanceConfig.UseGlobalSettings ? globalSettings.ResolutionWidth : instanceConfig.ResolutionWidth;
            int resHeight = instanceConfig.UseGlobalSettings ? globalSettings.ResolutionHeight : instanceConfig.ResolutionHeight;
            bool isFullscreen = instanceConfig.UseGlobalSettings ? false : instanceConfig.Fullscreen;

            // Construir argumentos de la JVM combinando memoria y parámetros personalizados
            string jvmArgsStr = instanceConfig.UseGlobalSettings ? globalSettings.JvmArgs : instanceConfig.JvmArgs;
            var jvmArgsList = new System.Collections.Generic.List<string> { $"-Xms{minMem}m", $"-Xmx{maxMem}m" };
            if (!string.IsNullOrWhiteSpace(jvmArgsStr))
            {
                var split = jvmArgsStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                jvmArgsList.AddRange(split);
            }
            int permGen = instanceConfig.UseGlobalSettings ? globalSettings.PermGen : instanceConfig.PermGen;
            if (permGen > 0 && JavaDetector.GetJavaMajorVersion(javaPath) < 8)
            {
                jvmArgsList.Add($"-XX:PermSize={permGen}m");
                jvmArgsList.Add($"-XX:MaxPermSize={permGen}m");
            }

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = maxMem,
                JavaPath = javaPath,
                ScreenHeight = resHeight,
                ScreenWidth = resWidth,
                FullScreen = isFullscreen,
                Session = MSession.CreateOfflineSession(globalSettings.SelectedAccount ?? "Player"),
                VersionType = "MiLauncher",
                JVMArguments = jvmArgsList.ToArray()
            };

            if (globalSettings.AutoConnect && !string.IsNullOrWhiteSpace(globalSettings.ServerAddress))
            {
                launchOption.ServerIp = globalSettings.ServerAddress;
                launchOption.ServerPort = globalSettings.ServerPort;
            }

            // INYECCIÓN DE CONFIGURACIÓN DE MOD DE SKINS
            try
            {
                string modsDir = Path.Combine(instancePath, "mods");
                bool hasCsl = false;
                if (Directory.Exists(modsDir))
                {
                    foreach (var file in Directory.GetFiles(modsDir, "*CustomSkinLoader*.jar"))
                    {
                        hasCsl = true;
                        break;
                    }
                }

                if (hasCsl)
                {
                    string cslConfigDir = Path.Combine(instancePath, "CustomSkinLoader");
                    Directory.CreateDirectory(cslConfigDir);
                    string cslConfigPath = Path.Combine(cslConfigDir, "CustomSkinLoader.json");
                    
                    string jsonConfig = @"{
  ""version"": ""14.28"",
  ""loadlist"": [
    {
      ""name"": ""LocalSkin"",
      ""type"": ""Legacy"",
      ""root"": ""LocalSkin/""
    },
    {
      ""name"": ""Mojang"",
      ""type"": ""MojangAPI""
    }
  ],
  ""enableDynamicSkull"": true,
  ""enableTransparentSkin"": true,
  ""ignoreHttpsCertificate"": false
}";
                    File.WriteAllText(cslConfigPath, jsonConfig);

                    // Cargar la skin actual desde SkinManager
                    var skinData = MiLauncher.launcher.net.SkinManager.LoadSkins();
                    var currentSkin = skinData.Skins.Find(s => s.Id == skinData.CurrentSkinId);
                    
                    if (currentSkin != null)
                    {
                        string username = launchOption.Session?.Username ?? "Player";
                        string localSkinDir = Path.Combine(cslConfigDir, "LocalSkin", "skins");
                        Directory.CreateDirectory(localSkinDir);
                        
                        if (!string.IsNullOrEmpty(currentSkin.SkinPath) && File.Exists(currentSkin.SkinPath))
                        {
                            string destSkinPath = Path.Combine(localSkinDir, username + ".png");
                            File.Copy(currentSkin.SkinPath, destSkinPath, true);
                        }

                        string localCapeDir = Path.Combine(cslConfigDir, "LocalSkin", "capes");
                        Directory.CreateDirectory(localCapeDir);
                        
                        string capeSourcePath = null;
                        if (!string.IsNullOrEmpty(currentSkin.CapePath))
                        {
                            if (currentSkin.CapePath == "Sin capa")
                            {
                                string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Capes");
                                Directory.CreateDirectory(cacheFolder);
                                string emptyCapePath = Path.Combine(cacheFolder, "Sin_capa.png");
                                if (!File.Exists(emptyCapePath))
                                {
                                    string b64 = "iVBORw0KGgoAAAANSUhEUgAAAEAAAAAgCAYAAACinVr6AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAA6SURBVGhD7cExAQAAAMIg+6deCj9gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwQW0AAAFjWkIAAAAASUVORK5CYII=";
                                    File.WriteAllBytes(emptyCapePath, Convert.FromBase64String(b64));
                                }
                                capeSourcePath = emptyCapePath;
                            }
                            else
                            {
                                string capesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Capes");
                                string capePhysicalPath = Path.Combine(capesDir, currentSkin.CapePath + ".png");

                                if (File.Exists(capePhysicalPath))
                                {
                                    capeSourcePath = capePhysicalPath;
                                }
                                else if (File.Exists(currentSkin.CapePath))
                                {
                                    capeSourcePath = currentSkin.CapePath;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(capeSourcePath))
                        {
                            string destCapePath = Path.Combine(localCapeDir, username + ".png");
                            if (File.Exists(capeSourcePath))
                            {
                                File.Copy(capeSourcePath, destCapePath, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error inyectando CustomSkinLoader: " + ex.Message);
            }

            _gameProcess = await launcher.CreateProcessAsync(targetVersion, launchOption);

            if (_gameProcess != null)
            {
                // Parche para el bug de Fabric con CmlLib: Duplicado de librerías ASM (9.3 vs 9.8)
                string args = _gameProcess.StartInfo.Arguments;
                if (args.Contains("asm-9.8") && args.Contains("asm-9.3"))
                {
                    // Limpiar todas las referencias a asm 9.3 (asm, asm-analysis, asm-commons, asm-tree, asm-util)
                    args = System.Text.RegularExpressions.Regex.Replace(args, @"[;:]?[^;:]*?asm[a-zA-Z0-9\-]*?\\9\.3\\[^;:]*?\.jar", "");
                    _gameProcess.StartInfo.Arguments = args;
                    Debug.WriteLine("Parche aplicado: ASM 9.3 eliminado del classpath para evitar conflicto con ASM 9.8");
                }

                // Aplicar Wrapper Command (estilo MiLauncher)
                string wrapper = instanceConfig.UseGlobalSettings ? globalSettings.WrapperCommand : instanceConfig.WrapperCommand;
                if (!string.IsNullOrWhiteSpace(wrapper))
                {
                    string originalFileName = _gameProcess.StartInfo.FileName;
                    string originalArgs = _gameProcess.StartInfo.Arguments;
                    _gameProcess.StartInfo.FileName = wrapper;
                    _gameProcess.StartInfo.Arguments = $"\"{originalFileName}\" {originalArgs}";
                }

                _gameProcess.EnableRaisingEvents = true;
                
                // Si la configuración del launcher es cerrarse tras abrir el juego (LauncherLaunchBehavior == 1),
                // iniciamos el proceso de Minecraft sin redirecciones de flujo de entrada/salida para evitar 
                // que Windows lo cierre cuando el launcher se apague.
                bool isCloseBehavior = globalSettings.LauncherLaunchBehavior == 1;
                
                _gameProcess.StartInfo.UseShellExecute = false;
                _gameProcess.StartInfo.CreateNoWindow = true;

                _gameProcess.StartInfo.RedirectStandardOutput = true;
                _gameProcess.StartInfo.RedirectStandardError = true;
                _gameProcess.StartInfo.RedirectStandardInput = true;

                string logFilePath = Path.Combine(instancePath, "launcher_log.txt");
                File.WriteAllText(logFilePath, $"--- Nuevo Lanzamiento: {DateTime.Now} ---\n");

                _gameProcess.Exited += (s, e) =>
                {
                    _gameProcess = null;
                    GameExited?.Invoke(this, EventArgs.Empty);

                    // Ejecutar Comando Post-Cierre (estilo MiLauncher)
                    string postExitCmd = instanceConfig.UseGlobalSettings ? globalSettings.PostExitCommand : instanceConfig.PostExitCommand;
                    if (!string.IsNullOrWhiteSpace(postExitCmd))
                    {
                        try
                        {
                            LogReceived?.Invoke(this, $"Ejecutando comando post-cierre: {postExitCmd}");
                            var psi = new ProcessStartInfo("cmd.exe", $"/c {postExitCmd}")
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            LogReceived?.Invoke(this, $"[ERROR] Error en comando post-cierre: {ex.Message}");
                        }
                    }
                };

                _gameProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        LogReceived?.Invoke(this, e.Data);
                        try { File.AppendAllText(logFilePath, e.Data + "\n"); } catch { }
                    }
                };
                
                _gameProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        LogReceived?.Invoke(this, "[ERROR] " + e.Data);
                        try { File.AppendAllText(logFilePath, "[ERROR] " + e.Data + "\n"); } catch { }
                    }
                };

                try
                {
                    // Ejecutar Comando Pre-Lanzamiento (estilo MiLauncher)
                    string preLaunchCmd = instanceConfig.UseGlobalSettings ? globalSettings.PreLaunchCommand : instanceConfig.PreLaunchCommand;
                    if (!string.IsNullOrWhiteSpace(preLaunchCmd))
                    {
                        try
                        {
                            LogReceived?.Invoke(this, $"Ejecutando comando pre-lanzamiento: {preLaunchCmd}");
                            var psi = new ProcessStartInfo("cmd.exe", $"/c {preLaunchCmd}")
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            var p = Process.Start(psi);
                            p?.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            LogReceived?.Invoke(this, $"[ERROR] Error en comando pre-lanzamiento: {ex.Message}");
                        }
                    }

                    string initMsg1 = "Iniciando: " + _gameProcess.StartInfo.FileName;
                    string initMsg2 = "Argumentos: " + _gameProcess.StartInfo.Arguments;
                    LogReceived?.Invoke(this, initMsg1);
                    LogReceived?.Invoke(this, initMsg2);
                    try { File.AppendAllText(logFilePath, initMsg1 + "\n" + initMsg2 + "\n"); } catch { }
                    
                    bool started = _gameProcess.Start();
                    if (started)
                    {
                        if (_gameProcess.StartInfo.RedirectStandardOutput)
                        {
                            _gameProcess.BeginOutputReadLine();
                        }
                        if (_gameProcess.StartInfo.RedirectStandardError)
                        {
                            _gameProcess.BeginErrorReadLine();
                        }
                        GameStarted?.Invoke(this, EventArgs.Empty);

                        // Maximizar ventana (estilo MiLauncher)
                        bool maxWindow = instanceConfig.UseGlobalSettings ? globalSettings.MaximizeWindow : instanceConfig.MaximizeWindow;
                        if (maxWindow)
                        {
                            var proc = _gameProcess;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    for (int i = 0; i < 100; i++)
                                    {
                                        if (proc == null || proc.HasExited) break;
                                        proc.Refresh();
                                        IntPtr hWnd = proc.MainWindowHandle;
                                        if (hWnd != IntPtr.Zero)
                                        {
                                            ShowWindow(hWnd, SW_MAXIMIZE);
                                            break;
                                        }
                                        await Task.Delay(300);
                                    }
                                }
                                catch { }
                            });
                        }
                    }
                    else
                    {
                        LogReceived?.Invoke(this, "[ERROR] El proceso de Java no pudo iniciar.");
                        try { File.AppendAllText(logFilePath, "[ERROR] El proceso de Java no pudo iniciar.\n"); } catch { }
                        GameExited?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    LogReceived?.Invoke(this, "[FATAL ERROR] " + ex.Message);
                    GameExited?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void KillGame()
        {
            if (IsGameRunning && _gameProcess != null)
            {
                try
                {
                    _gameProcess.Kill();
                }
                catch (Exception) { }
            }
        }
    }
}
