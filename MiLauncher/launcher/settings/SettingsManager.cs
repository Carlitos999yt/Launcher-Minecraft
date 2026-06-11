using System;
using System.IO;
using System.Text.Json;
using MiLauncher.launcher.tools;

namespace MiLauncher.launcher.settings
{
    public class AppConfig
    {
        // Global
        public bool UseDarkMode { get; set; } = true;
        public string PrimaryColor { get; set; } = "#171615";
        public string SecondaryColor { get; set; } = "#252526";
        public string BorderColor { get; set; } = "#33FFFFFF";
        public string TextColor { get; set; } = "#FFFFFF";
        
        // Java
        public string JavaPath { get; set; } = MiLauncher.launcher.java.JavaCommon.FindDefaultJava() ?? "javaw.exe";
        public int MinMem { get; set; } = 1024;
        public int MaxMem { get; set; } = MiLauncher.launcher.tools.SysInfo.DefaultMaxJvmMem();
        public int PermGen { get; set; } = 256;
        public string JvmArgs { get; set; } = "-XX:+UseG1GC -Dsun.rmi.dgc.server.gcInterval=2147483646 -XX:+UnlockExperimentalVMOptions -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M";
        public string WrapperCommand { get; set; } = "";
        
        // Launcher
        public string InstancesFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Instances");
        public string IconsFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Icons");
        public bool CloseAfterLaunch { get; set; } = true;
        public bool ShowConsole { get; set; } = true;
        public bool MaximizeWindow { get; set; } = false;
        public string PreLaunchCommand { get; set; } = "";
        public string PostExitCommand { get; set; } = "";
        
        // Comportamiento de Lanzamiento Avanzado
        public bool IsDeveloperMode { get; set; } = false;
        public int LauncherLaunchBehavior { get; set; } = 1; // 0 = Keep Open, 1 = Close, 2 = Hide
        public bool CloseConsoleOnGameExit { get; set; } = false;
        public bool CloseLauncherOnGameExit { get; set; } = false;
        public bool AutoConnect { get; set; } = false;
        public string ServerAddress { get; set; } = "";
        public int ServerPort { get; set; } = 25565;
        
        // Window
        public int ResolutionWidth { get; set; } = 854;
        public int ResolutionHeight { get; set; } = 480;
        public bool Fullscreen { get; set; } = false;

        // Accounts
        public System.Collections.Generic.List<string> OfflineAccounts { get; set; } = new System.Collections.Generic.List<string> { "TuNombreAqui" };
        public string SelectedAccount { get; set; } = "TuNombreAqui";
    }

    public static class SettingsManager
    {
        private static readonly string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher");
        private static readonly string SettingsFile = Path.Combine(ConfigDir, "settings.json");

        public static void SaveSettings(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                    Directory.CreateDirectory(ConfigDir);

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                MiLauncher.launcher.tools.Logger.Error($"Error guardando settings.json: {ex.Message}");
            }
        }

        public static string GetHardwareHash()
        {
            string raw = Environment.MachineName + Environment.UserName + Environment.ProcessorCount.ToString();
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
                string hex = BitConverter.ToString(bytes).Replace("-", "").ToUpper();
                return hex.Substring(0, 12);
            }
        }

        private static string GenerateRandomSuffix()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new char[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        public static string GetOrCreateOfflineID(bool forceReset = false)
        {
            string hwHash = GetHardwareHash();
            
            // Paths
            string localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiLauncher", ".sys_id");
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", ".sys_id");
            string userPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sys_id");

            string? localVal = ReadSecureFile(localPath);
            string? appDataVal = ReadSecureFile(appDataPath);
            string? userVal = ReadSecureFile(userPath);

            string? resolvedID = null;

            if (!forceReset)
            {
                // Vote check
                var votes = new System.Collections.Generic.Dictionary<string, int>();
                if (IsValidID(localVal, hwHash)) votes[localVal!] = (votes.ContainsKey(localVal!) ? votes[localVal!] : 0) + 1;
                if (IsValidID(appDataVal, hwHash)) votes[appDataVal!] = (votes.ContainsKey(appDataVal!) ? votes[appDataVal!] : 0) + 1;
                if (IsValidID(userVal, hwHash)) votes[userVal!] = (votes.ContainsKey(userVal!) ? votes[userVal!] : 0) + 1;

                string? bestVal = null;
                int maxVotes = 0;
                foreach (var kvp in votes)
                {
                    if (kvp.Value > maxVotes)
                    {
                        maxVotes = kvp.Value;
                        bestVal = kvp.Key;
                    }
                }

                if (maxVotes >= 2)
                {
                    resolvedID = bestVal;
                }
                else if (maxVotes == 1)
                {
                    resolvedID = bestVal;
                }
            }

            if (string.IsNullOrEmpty(resolvedID))
            {
                // Generate new ID
                resolvedID = hwHash + GenerateRandomSuffix();
            }

            // Sync/Write to all files
            WriteSecureFile(localPath, resolvedID);
            WriteSecureFile(appDataPath, resolvedID);
            WriteSecureFile(userPath, resolvedID);

            return resolvedID;
        }

        private static bool IsValidID(string? id, string hwHash)
        {
            if (string.IsNullOrEmpty(id) || id.Length != 20) return false;
            return id.StartsWith(hwHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string? ReadSecureFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path).Trim();
                }
            }
            catch { }
            return null;
        }

        private static void WriteSecureFile(string path, string id)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(path))
                {
                    // Remove hidden attribute to write
                    var fileInfo = new FileInfo(path);
                    if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        fileInfo.Attributes &= ~FileAttributes.Hidden;
                    }
                }

                File.WriteAllText(path, id);
                
                // Make file hidden
                var info = new FileInfo(path);
                info.Attributes |= FileAttributes.Hidden;
            }
            catch { }
        }

        public static AppConfig LoadSettings()
        {
            try
            {
                AppConfig config;
                if (!File.Exists(SettingsFile)) 
                    config = new AppConfig();
                else
                {
                    string json = File.ReadAllText(SettingsFile);
                    config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }

                // Generar/inicializar el ID Seguro pero no forzarlo como cuenta única del usuario
                GetOrCreateOfflineID();
                
                if (config.OfflineAccounts == null || config.OfflineAccounts.Count == 0)
                {
                    config.OfflineAccounts = new System.Collections.Generic.List<string> { "TuNombreAqui" };
                    config.SelectedAccount = "TuNombreAqui";
                }

                return config;
            }
            catch (Exception ex)
            {
                MiLauncher.launcher.tools.Logger.Error($"Error leyendo settings.json: {ex.Message}");
                var config = new AppConfig();
                config.OfflineAccounts = new System.Collections.Generic.List<string> { "TuNombreAqui" };
                config.SelectedAccount = "TuNombreAqui";
                GetOrCreateOfflineID();
                return config;
            }
        }
    }
}
