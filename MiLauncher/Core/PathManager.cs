using System;
using System.IO;

namespace MiLauncher.Core
{
    public static class PathManager
    {
        public static string GlobalLauncherPath { get; private set; }
        private const string LauncherFolderName = ".mylauncher"; // Nombre genérico que cambiaremos

        static PathManager()
        {
            // Ruta global por defecto: AppData/Roaming/.mylauncher
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            GlobalLauncherPath = Path.Combine(appData, LauncherFolderName);
            Directory.CreateDirectory(GlobalLauncherPath);
        }

        public static void SetGlobalPath(string newPath)
        {
            GlobalLauncherPath = newPath;
            Directory.CreateDirectory(GlobalLauncherPath);
        }

        public static string GetInstancePath(string modpackName, string? customPath = null)
        {
            // Si el usuario eligió una ruta manual solo para este modpack (ej. D:\Zombies)
            if (!string.IsNullOrEmpty(customPath))
            {
                Directory.CreateDirectory(customPath);
                return customPath;
            }

            // Ruta aislada por defecto
            string path = Path.Combine(GlobalLauncherPath, "Instancias", modpackName);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
