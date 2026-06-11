using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MiLauncher.launcher.tools;

namespace MiLauncher.launcher.minecraft
{
    public class WhitelistedModpack
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = "";
        public string Branch { get; set; } = "main";
        public string GameVersion { get; set; } = "1.20.1";
        public string ModLoader { get; set; } = "Fabric"; // Vanilla, Fabric, Forge
        public string LoaderVersion { get; set; } = "";
        public string TitleColor { get; set; } = "#FFFFFF";
        public double TitleFontSize { get; set; } = 28.0;
        public string TitleFontFamily { get; set; } = "Segoe UI";
    }

    public class GitHubSyncService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(10); // Límite de 10 descargas paralelas

        // Ajustes por defecto de los repositorios
        public static string Username { get; set; } = "Carlitos999yt";
        public static string ModpacksRepo { get; set; } = "Modpack-Servers";
        public static string ReleasesRepo { get; set; } = "Launcher-Minecraft";
        
        // Token de acceso personal si la whitelist/repositorio es privado
        public static string PersonalAccessToken { get; set; } = "";

        private static HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("User-Agent", "MiLauncher-Client/1.0");
            if (!string.IsNullOrEmpty(PersonalAccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("token", PersonalAccessToken);
            }
            return request;
        }

        public static async Task<(string newVersion, string changelog, string downloadUrl)> CheckForUpdatesAsync(string currentVersion)
        {
            string url = $"https://api.github.com/repos/{Username}/{ReleasesRepo}/releases/latest";
            try
            {
                using var request = CreateRequest(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    string tag = root.GetProperty("tag_name").GetString() ?? "";
                    string body = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
                    string downloadUrl = $"https://github.com/{Username}/{ReleasesRepo}/releases";

                    if (root.TryGetProperty("assets", out var assetsEl) && assetsEl.ValueKind == JsonValueKind.Array && assetsEl.GetArrayLength() > 0)
                    {
                        downloadUrl = assetsEl[0].GetProperty("browser_download_url").GetString() ?? downloadUrl;
                    }

                    string cleanNew = tag.TrimStart('v', 'V', ' ');
                    string cleanCur = currentVersion.TrimStart('v', 'V', ' ');

                    return (cleanNew, body, downloadUrl);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobando actualizaciones en GitHub: {ex.Message}");
            }
            return ("", "", "");
        }

        public static async Task<List<WhitelistedModpack>> GetWhitelistAsync()
        {
            // Intentar leer whitelist.json de la rama whitelist del repositorio de modpacks
            string url = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/whitelist/whitelist.json";
            
            // Si el repositorio es privado, raw.githubusercontent.com requiere token en la URL, 
            // por lo que usamos la API de contenidos de GitHub que soporta headers de autorización.
            if (!string.IsNullOrEmpty(PersonalAccessToken))
            {
                url = $"https://api.github.com/repos/{Username}/{ModpacksRepo}/contents/whitelist.json?ref=whitelist";
            }

            try
            {
                using var request = CreateRequest(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json;
                    if (!string.IsNullOrEmpty(PersonalAccessToken))
                    {
                        // La API de contenidos de GitHub devuelve un objeto base64
                        string apiJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(apiJson);
                        string base64Content = doc.RootElement.GetProperty("content").GetString() ?? "";
                        // Limpiar saltos de línea del base64
                        base64Content = base64Content.Replace("\n", "").Replace("\r", "");
                        byte[] data = Convert.FromBase64String(base64Content);
                        json = System.Text.Encoding.UTF8.GetString(data);
                    }
                    else
                    {
                        json = await response.Content.ReadAsStringAsync();
                    }

                    var list = JsonSerializer.Deserialize<List<WhitelistedModpack>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return list ?? new List<WhitelistedModpack>();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error cargando la Whitelist de GitHub: {ex.Message}");
            }
            return new List<WhitelistedModpack>();
        }

        public static async Task DownloadModpackAsync(WhitelistedModpack modpack, string targetDir, Action<string, double> progressCallback)
        {
            try
            {
                Directory.CreateDirectory(targetDir);

                // 1. Descargar config-modpack.json y guardarlo como instance_config.json
                progressCallback("Descargando archivo de configuración de la instancia...", 5);
                string configUrl = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/{modpack.Branch}/config-modpack.json";
                if (!string.IsNullOrEmpty(PersonalAccessToken))
                {
                    configUrl = $"https://api.github.com/repos/{Username}/{ModpacksRepo}/contents/config-modpack.json?ref={modpack.Branch}";
                }

                string configJson = await DownloadStringContentAsync(configUrl);
                if (!string.IsNullOrEmpty(configJson))
                {
                    // Guardar como instance_config.json local
                    // Mapeamos config-modpack.json a la estructura local de InstanceConfig
                    using var doc = JsonDocument.Parse(configJson);
                    var root = doc.RootElement;
                    
                    var localConfig = new settings.InstanceConfig
                    {
                        Name = modpack.Name,
                        GameVersion = root.TryGetProperty("gameVersion", out var gv) ? gv.GetString() ?? modpack.GameVersion : modpack.GameVersion,
                        ModLoader = root.TryGetProperty("modLoader", out var ml) ? ml.GetString() ?? modpack.ModLoader : modpack.ModLoader,
                        LoaderVersion = root.TryGetProperty("loaderVersion", out var lv) ? lv.GetString() ?? modpack.LoaderVersion : modpack.LoaderVersion,
                        MinMem = root.TryGetProperty("minMem", out var minm) ? minm.GetInt32() : 1024,
                        MaxMem = root.TryGetProperty("maxMem", out var maxm) ? maxm.GetInt32() : 4096,
                        JvmArgs = root.TryGetProperty("jvmArgs", out var ja) ? ja.GetString() ?? "" : "",
                        WrapperCommand = root.TryGetProperty("wrapperCommand", out var wc) ? wc.GetString() ?? "" : "",
                        PreLaunchCommand = root.TryGetProperty("preLaunchCommand", out var plc) ? plc.GetString() ?? "" : "",
                        PostExitCommand = root.TryGetProperty("postExitCommand", out var pec) ? pec.GetString() ?? "" : ""
                    };
                    localConfig.Save(targetDir);
                }
                else
                {
                    // Crear configuración básica si no existe
                    var localConfig = new settings.InstanceConfig
                    {
                        Name = modpack.Name,
                        GameVersion = modpack.GameVersion,
                        ModLoader = modpack.ModLoader,
                        LoaderVersion = modpack.LoaderVersion
                    };
                    localConfig.Save(targetDir);
                }

                // Escribir metadata.json local para las personalizaciones visuales de la whitelist
                string metadataPath = Path.Combine(targetDir, "metadata.json");
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
                string metaJson = JsonSerializer.Serialize(wlMetadata, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(metadataPath, metaJson);

                // 2. Descargar background-modpack.png / background-modpack.jpg
                progressCallback("Descargando imágenes promocionales y fondos...", 15);
                
                string[] bgExtensions = { "png", "jpg", "jpeg" };
                bool bgDownloaded = false;
                foreach (var ext in bgExtensions)
                {
                    string bgUrl = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/{modpack.Branch}/background-modpack.{ext}";
                    string localBg = Path.Combine(targetDir, $"background.{ext}");
                    
                    if (await DownloadBinaryFileAsync(bgUrl, localBg))
                    {
                        bgDownloaded = true;
                        break;
                    }
                }

                // Descargar icono: custom_icon.png
                string iconUrl = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/{modpack.Branch}/custom_icon.png";
                string localIcon = Path.Combine(targetDir, "custom_icon.png");
                await DownloadBinaryFileAsync(iconUrl, localIcon);

                // Descargar galería de hasta 10 imágenes: image-1-modpack.png a image-10-modpack.png
                for (int i = 1; i <= 10; i++)
                {
                    string imgUrl = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/{modpack.Branch}/image-{i}-modpack.png";
                    string localImg = Path.Combine(targetDir, $"image-{i}-modpack.png");
                    
                    // Si falla una descarga (404), detenemos la búsqueda secuencial para no sobrecargar de peticiones inválidas
                    if (!await DownloadBinaryFileAsync(imgUrl, localImg))
                    {
                        break;
                    }
                }

                // 3. Descargar mods
                // Primero intentamos descargar un archivo único consolidado: mods.zip
                progressCallback("Comprobando existencia de paquete consolidado de mods (mods.zip)...", 25);
                string modsZipUrl = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/{modpack.Branch}/mods.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"mods_{Guid.NewGuid()}.zip");
                
                bool zipDownloaded = await DownloadBinaryFileAsync(modsZipUrl, tempZipPath);
                
                if (zipDownloaded)
                {
                    progressCallback("Descomprimiendo archivos de mods...", 60);
                    string modsFolder = Path.Combine(targetDir, "mods");
                    if (Directory.Exists(modsFolder)) Directory.Delete(modsFolder, true);
                    Directory.CreateDirectory(modsFolder);
                    
                    await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, modsFolder, overwriteFiles: true));
                    try { File.Delete(tempZipPath); } catch { }
                    progressCallback("Instalación de mods completada correctamente.", 100);
                }
                else
                {
                    // Si no hay mods.zip, listamos y descargamos mods individuales usando la API de contenidos de GitHub
                    progressCallback("Paquete mods.zip no encontrado. Obteniendo lista de mods individuales...", 30);
                    string contentsUrl = $"https://api.github.com/repos/{Username}/{ModpacksRepo}/contents/mods?ref={modpack.Branch}";
                    
                    List<string> modUrls = new List<string>();
                    List<string> modFilenames = new List<string>();

                    try
                    {
                        using var req = CreateRequest(HttpMethod.Get, contentsUrl);
                        using var res = await _httpClient.SendAsync(req);
                        if (res.IsSuccessStatusCode)
                        {
                            string apiJson = await res.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(apiJson);
                            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var fileEl in doc.RootElement.EnumerateArray())
                                {
                                    string name = fileEl.GetProperty("name").GetString() ?? "";
                                    string downloadUrl = fileEl.GetProperty("download_url").GetString() ?? "";
                                    if (name.EndsWith(".jar") && !string.IsNullOrEmpty(downloadUrl))
                                    {
                                        modUrls.Add(downloadUrl);
                                        modFilenames.Add(name);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error al listar mods individuales desde la API de GitHub: {ex.Message}");
                    }

                    if (modUrls.Count > 0)
                    {
                        string modsFolder = Path.Combine(targetDir, "mods");
                        Directory.CreateDirectory(modsFolder);

                        int totalMods = modUrls.Count;
                        int downloaded = 0;

                        var downloadTasks = new List<Task>();
                        for (int i = 0; i < totalMods; i++)
                        {
                            string mUrl = modUrls[i];
                            string mFile = Path.Combine(modsFolder, modFilenames[i]);

                            downloadTasks.Add(Task.Run(async () =>
                            {
                                await _downloadSemaphore.WaitAsync();
                                try
                                {
                                    await DownloadBinaryFileAsync(mUrl, mFile);
                                }
                                finally
                                {
                                    _downloadSemaphore.Release();
                                    lock (modUrls)
                                    {
                                        downloaded++;
                                        double perc = 30.0 + ((double)downloaded / totalMods) * 70.0;
                                        progressCallback($"Descargando mods ({downloaded}/{totalMods})...", perc);
                                    }
                                }
                            }));
                        }

                        await Task.WhenAll(downloadTasks);
                        progressCallback("Mods individuales descargados con éxito.", 100);
                    }
                    else
                    {
                        progressCallback("No se encontraron mods en esta rama del modpack.", 100);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error instalando modpack '{modpack.Name}': {ex.Message}");
                throw;
            }
        }

        private static async Task<string> DownloadStringContentAsync(string url)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    if (url.Contains("/contents/"))
                    {
                        string apiJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(apiJson);
                        string base64Content = doc.RootElement.GetProperty("content").GetString() ?? "";
                        base64Content = base64Content.Replace("\n", "").Replace("\r", "");
                        byte[] data = Convert.FromBase64String(base64Content);
                        return System.Text.Encoding.UTF8.GetString(data);
                    }
                    else
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch { }
            return "";
        }

        private static async Task<bool> DownloadBinaryFileAsync(string url, string destinationPath)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    string dir = Path.GetDirectoryName(destinationPath) ?? "";
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
