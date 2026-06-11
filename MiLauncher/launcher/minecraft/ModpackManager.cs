using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MiLauncher.launcher.tools;

namespace MiLauncher.launcher.minecraft
{
    public static class ModpackManager
    {
        private static readonly string ModpacksDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modpacks");

        public static async Task InstallModpackAsync(string zipFilePath, string targetName)
        {
            try
            {
                if (!Directory.Exists(ModpacksDir))
                    Directory.CreateDirectory(ModpacksDir);

                string targetDir = Path.Combine(ModpacksDir, targetName);
                if (Directory.Exists(targetDir))
                    Directory.Delete(targetDir, true);

                Directory.CreateDirectory(targetDir);

                await Task.Run(() => 
                {
                    ZipFile.ExtractToDirectory(zipFilePath, targetDir, overwriteFiles: true);
                });

                Logger.Info($"Modpack '{targetName}' extraído correctamente en {targetDir}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error extrayendo modpack: {ex.Message}");
                throw;
            }
        }
    }
}
