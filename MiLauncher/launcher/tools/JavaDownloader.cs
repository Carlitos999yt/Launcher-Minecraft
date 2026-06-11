using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MiLauncher.launcher.tools
{
    public static class JavaDownloader
    {
        public static async Task<string> GetOrDownloadJava21Async(Action<string> onProgress)
        {
            string runtimeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "runtime", "java-21");
            string javaExePath = Path.Combine(runtimeFolder, "bin", "java.exe");

            if (File.Exists(javaExePath))
            {
                return javaExePath; // Ya está descargado
            }

            try
            {
                onProgress?.Invoke("Descargando Java 21 (Requerido para versiones modernas)...");
                Directory.CreateDirectory(runtimeFolder);
                string zipPath = Path.Combine(runtimeFolder, "java21.zip");

                using (var client = new HttpClient())
                {
                    // API de Adoptium para obtener el JRE de Java 21 en Windows x64
                    string url = "https://api.adoptium.net/v3/binary/latest/21/ga/windows/x64/jre/hotspot/normal/eclipse";
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                onProgress?.Invoke("Extrayendo Java 21...");
                
                // Extraer a una carpeta temporal para encontrar la subcarpeta real
                string tempExtract = Path.Combine(runtimeFolder, "temp_extract");
                if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
                Directory.CreateDirectory(tempExtract);
                
                ZipFile.ExtractToDirectory(zipPath, tempExtract);
                
                // Mover el contenido de la subcarpeta (ej. jdk-21.0.2+13-jre) al directorio root
                var subDirs = Directory.GetDirectories(tempExtract);
                if (subDirs.Length > 0)
                {
                    string rootSubDir = subDirs[0];
                    foreach (var file in Directory.GetFiles(rootSubDir, "*", SearchOption.AllDirectories))
                    {
                        string relPath = file.Substring(rootSubDir.Length + 1);
                        string destPath = Path.Combine(runtimeFolder, relPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        File.Copy(file, destPath, true);
                    }
                }

                // Limpieza
                Directory.Delete(tempExtract, true);
                File.Delete(zipPath);

                onProgress?.Invoke("Java 21 instalado correctamente.");
                
                if (File.Exists(javaExePath))
                    return javaExePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error descargando Java 21: {ex.Message}");
            }

            return null; // Falló la descarga
        }
    }
}
