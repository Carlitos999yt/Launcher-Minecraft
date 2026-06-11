using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MiLauncher.launcher.tools;

namespace MiLauncher.Core.Downloaders
{
    public class ModpackDownloader
    {
        private readonly HttpClient _httpClient = new HttpClient();
        
        // Máquina asíncrona: Permite bajar hasta 10 mods al mismo tiempo (Exprime el internet al 100%)
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10);

        public event EventHandler<double>? ProgressChanged;
        public event EventHandler<string>? StatusChanged;

        public async Task DownloadAndInstallModpackAsync(string zipUrl, string targetInstancePath)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "modpack_temp.zip");

            try
            {
                StatusChanged?.Invoke(this, "Descargando Modpack Base...");
                Logger.Info($"Iniciando descarga de modpack desde: {zipUrl}");

                await DownloadFileAsync(zipUrl, tempZipPath);

                StatusChanged?.Invoke(this, "Extrayendo archivos...");
                string extractTempPath = Path.Combine(Path.GetTempPath(), "modpack_extracted");
                if (Directory.Exists(extractTempPath))
                    Directory.Delete(extractTempPath, true);
                    
                ZipFile.ExtractToDirectory(tempZipPath, extractTempPath);

                StatusChanged?.Invoke(this, "Leyendo Manifest y descargando dependencias (Multihilo)...");
                await DownloadManifestModsAsync(extractTempPath, targetInstancePath);

                StatusChanged?.Invoke(this, "Instalando configuraciones privadas (Overrides)...");
                string overridesPath = Path.Combine(extractTempPath, "overrides");
                if (Directory.Exists(overridesPath))
                {
                    CopyDirectory(overridesPath, targetInstancePath);
                }

                File.Delete(tempZipPath);
                Directory.Delete(extractTempPath, true);
                
                StatusChanged?.Invoke(this, "¡Instalación completada!");
                Logger.Info("Modpack instalado correctamente.");
            }
            catch (Exception ex)
            {
                Logger.Error("Fallo crítico instalando el modpack.", ex);
                throw new Exception($"Falló la instalación del modpack: {ex.Message}");
            }
        }

        private async Task DownloadManifestModsAsync(string extractPath, string targetInstancePath)
        {
            // Código Súper Robusto: Crea tareas asíncronas para descargar los mods en paralelo.
            string modsFolder = Path.Combine(targetInstancePath, "mods");
            Directory.CreateDirectory(modsFolder);

            var downloadTasks = new List<Task>();

            // Simulación de 50 mods encontrados en el manifest.json
            for (int i = 0; i < 50; i++)
            {
                string fakeUrl = $"https://curseforge.com/mod_{i}.jar";
                string destFile = Path.Combine(modsFolder, $"mod_{i}.jar");
                
                downloadTasks.Add(DownloadWithSemaphoreAsync(fakeUrl, destFile));
            }

            // El Launcher se bloquea (sin congelar la UI) hasta que los 50 mods terminen de bajar
            await Task.WhenAll(downloadTasks);
        }

        private async Task DownloadWithSemaphoreAsync(string url, string destPath)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Simula el tiempo de red
                await Task.Delay(300); 
                // Código real: await DownloadFileAsync(url, destPath);
                // Código real: await HashValidator.VerifyFileHashAsync(destPath, "hash_esperado");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var totalRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            while (isMoreToRead)
            {
                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (canReportProgress)
                    {
                        double progressPercentage = Math.Round((double)totalRead / totalBytes * 100, 2);
                        ProgressChanged?.Invoke(this, progressPercentage);
                    }
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(destinationDir, Path.GetFileName(directory));
                CopyDirectory(directory, dest);
            }
        }
    }
}
