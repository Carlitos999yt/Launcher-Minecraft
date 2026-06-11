using System;
using System.IO;

namespace MiLauncher.launcher.tools
{
    public static class Logger
    {
        private static string LogPath = "";

        public static void Initialize(string logDirectory)
        {
            Directory.CreateDirectory(logDirectory);
            LogPath = Path.Combine(logDirectory, "latest.log");
            if (File.Exists(LogPath))
                File.Delete(LogPath); // Limpiamos el viejo en cada arranque
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message, Exception? ex = null)
        {
            Write("ERROR", $"{message} | Excepción: {ex?.Message}");
            if (ex?.StackTrace != null)
                Write("ERROR", ex.StackTrace);
        }

        public static event Action<string>? LogAdded;

        public static void LogDirect(string message)
        {
            if (string.IsNullOrEmpty(LogPath)) return;
            try
            {
                File.AppendAllText(LogPath, message + Environment.NewLine);
            }
            catch { /* Ignorar si hay lock del sistema de archivos */ }
            LogAdded?.Invoke(message);
        }

        private static void Write(string level, string message)
        {
            if (string.IsNullOrEmpty(LogPath)) return;
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            try
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch { /* Ignorar si hay lock del sistema de archivos */ }
            LogAdded?.Invoke(line);
        }
    }
}
