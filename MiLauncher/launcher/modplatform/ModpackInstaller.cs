using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MiLauncher.launcher.settings;

namespace MiLauncher.launcher.modplatform
{
    public class ModpackInstaller
    {
        public static async Task<string> InstallMrPackAsync(string mrpackPath, string instanceName, Action<string, double>? progressCallback = null)
        {
            try
            {
                var globalSettings = SettingsManager.LoadSettings();
                string targetInstancePath = Path.Combine(globalSettings.InstancesFolder, instanceName);

                if (Directory.Exists(targetInstancePath))
                {
                    throw new Exception("Ya existe una instancia con ese nombre.");
                }

                Directory.CreateDirectory(targetInstancePath);

                // 1. Extraer ZIP a temporal
                progressCallback?.Invoke("Extrayendo archivo .mrpack...", 0);
                string tempFolder = Path.Combine(Path.GetTempPath(), "MiLauncher_MrPack_" + Guid.NewGuid().ToString());
                ZipFile.ExtractToDirectory(mrpackPath, tempFolder, true);

                // 2. Leer modrinth.index.json
                string indexPath = Path.Combine(tempFolder, "modrinth.index.json");
                if (!File.Exists(indexPath))
                {
                    throw new Exception("El archivo no es un .mrpack válido (falta modrinth.index.json)");
                }

                string indexJson = File.ReadAllText(indexPath);
                using var document = JsonDocument.Parse(indexJson);
                var root = document.RootElement;

                string mcVersion = "";
                string fabricVersion = "";
                string forgeVersion = "";

                if (root.TryGetProperty("dependencies", out var deps))
                {
                    if (deps.TryGetProperty("minecraft", out var mc)) mcVersion = mc.GetString() ?? "";
                    if (deps.TryGetProperty("fabric-loader", out var fab)) fabricVersion = fab.GetString() ?? "";
                    if (deps.TryGetProperty("forge", out var frg)) forgeVersion = frg.GetString() ?? "";
                }

                if (string.IsNullOrEmpty(mcVersion)) throw new Exception("No se encontró la versión de Minecraft en el modpack.");

                // 3. Crear instance.json
                progressCallback?.Invoke("Configurando instancia...", 5);
                var config = new InstanceConfig
                {
                    Name = instanceName,
                    GameVersion = mcVersion,
                    LoaderVersion = !string.IsNullOrEmpty(fabricVersion) ? fabricVersion : forgeVersion,
                    ModLoader = !string.IsNullOrEmpty(fabricVersion) ? "Fabric" : (!string.IsNullOrEmpty(forgeVersion) ? "Forge" : "Vanilla"),
                    UseGlobalSettings = true
                };
                config.Save(targetInstancePath);

                // 4. Copiar Overrides
                progressCallback?.Invoke("Copiando configuraciones (overrides)...", 10);
                string overridesDir = Path.Combine(tempFolder, "overrides");
                if (Directory.Exists(overridesDir))
                {
                    CopyDirectory(overridesDir, targetInstancePath);
                }
                
                string clientOverridesDir = Path.Combine(tempFolder, "client-overrides");
                if (Directory.Exists(clientOverridesDir))
                {
                    CopyDirectory(clientOverridesDir, targetInstancePath);
                }

                // 5. Descargar archivos (mods, resourcepacks, etc)
                if (root.TryGetProperty("files", out var filesElement))
                {
                    var files = filesElement.EnumerateArray().ToList();
                    int totalFiles = files.Count;
                    int downloaded = 0;

                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "MiLauncher/1.0");

                    var tasks = new List<Task>();
                    // Control de concurrencia para no saturar
                    var semaphore = new System.Threading.SemaphoreSlim(10); 

                    foreach (var fileEl in files)
                    {
                        await semaphore.WaitAsync();

                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                string relativePath = fileEl.GetProperty("path").GetString() ?? "";
                                if (string.IsNullOrEmpty(relativePath)) return;

                                // Normalize path to Windows format if needed
                                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

                                var downloads = fileEl.GetProperty("downloads").EnumerateArray().ToList();
                                if (downloads.Count == 0) return;

                                string downloadUrl = downloads[0].GetString() ?? "";
                                if (string.IsNullOrEmpty(downloadUrl)) return;

                                string destPath = Path.Combine(targetInstancePath, relativePath);
                                string destDir = Path.GetDirectoryName(destPath) ?? "";
                                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                                var response = await httpClient.GetAsync(downloadUrl);
                                response.EnsureSuccessStatusCode();

                                using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await response.Content.CopyToAsync(fs);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error descargando mod: {ex.Message}");
                            }
                            finally
                            {
                                lock (semaphore)
                                {
                                    downloaded++;
                                    double perc = 10.0 + ((double)downloaded / totalFiles) * 90.0;
                                    progressCallback?.Invoke($"Descargando archivos ({downloaded}/{totalFiles})...", perc);
                                }
                                semaphore.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);
                }

                // Limpieza
                try
                {
                    Directory.Delete(tempFolder, true);
                }
                catch { }

                progressCallback?.Invoke("¡Modpack instalado correctamente!", 100);
                return targetInstancePath;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al instalar modpack: " + ex.Message);
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }
}
