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
using System.Linq;
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
                        Version = root.TryGetProperty("version", out var v) ? v.GetString() ?? modpack.Version : modpack.Version,
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
                        Version = modpack.Version,
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
                }

                // 4. Descargar mods externos (> 40MB) especificados en external_mods.json
                progressCallback("Comprobando existencia de mods externos (>40MB)...", 90);
                string extModsUrl = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/{modpack.Branch}/external_mods.json";
                if (!string.IsNullOrEmpty(PersonalAccessToken))
                {
                    extModsUrl = $"https://api.github.com/repos/{Username}/{ModpacksRepo}/contents/external_mods.json?ref={modpack.Branch}";
                }

                string extModsJson = await DownloadStringContentAsync(extModsUrl);
                if (!string.IsNullOrEmpty(extModsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(extModsJson);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            string modsFolder = Path.Combine(targetDir, "mods");
                            Directory.CreateDirectory(modsFolder);

                            var array = doc.RootElement;
                            int totalExt = array.GetArrayLength();
                            int extDone = 0;
                            Logger.Info($"Descargando {totalExt} mods externos > 40MB...");

                            var extTasks = new List<Task>();
                            foreach (var item in array.EnumerateArray())
                            {
                                string filename = item.TryGetProperty("filename", out var fn) ? fn.GetString() ?? "" : "";
                                string url = item.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";

                                if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(url))
                                {
                                    string destPath = Path.Combine(modsFolder, filename);
                                    extTasks.Add(Task.Run(async () =>
                                    {
                                        await _downloadSemaphore.WaitAsync();
                                        try
                                        {
                                            Logger.Info($"Descargando mod externo: {filename}");
                                            await DownloadBinaryFileAsync(url, destPath);
                                        }
                                        finally
                                        {
                                            _downloadSemaphore.Release();
                                            lock (extModsUrl)
                                            {
                                                extDone++;
                                                double perc = 90.0 + ((double)extDone / totalExt) * 8.0;
                                                progressCallback($"Descargando mods grandes ({extDone}/{totalExt})...", perc);
                                            }
                                        }
                                    }));
                                }
                            }
                            await Task.WhenAll(extTasks);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error al descargar mods externos: {ex.Message}");
                    }
                }

                // 5. Instalar CustomSkinLoader dinámicamente
                progressCallback("Instalando mod de skins CustomSkinLoader...", 98);
                await InstallCustomSkinLoaderAsync(targetDir, modpack.GameVersion, modpack.ModLoader);
                progressCallback("Instalación completada con éxito.", 100);
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

        public static async Task<string> GetPlayersJsonAsync()
        {
            string url = $"https://raw.githubusercontent.com/{Username}/{ModpacksRepo}/whitelist/players.json";
            if (!string.IsNullOrEmpty(PersonalAccessToken))
            {
                url = $"https://api.github.com/repos/{Username}/{ModpacksRepo}/contents/players.json?ref=whitelist";
            }
            return await DownloadStringContentAsync(url);
        }

        public static async Task<bool> IsPlayerAuthorizedAsync(string modpackId)
        {
            try
            {
                string json = await GetPlayersJsonAsync();
                if (string.IsNullOrEmpty(json))
                {
                    Logger.Error("No se pudo descargar la whitelist de jugadores (players.json) o está vacía.");
                    return false;
                }
                var players = JsonSerializer.Deserialize<List<PlayerWhitelistEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (players == null) return false;

                string secureId = settings.SettingsManager.GetOrCreateOfflineID();
                string selectedAccount = settings.SettingsManager.LoadSettings().SelectedAccount;

                foreach (var p in players)
                {
                    bool isOfflineMatch = string.Equals(p.Id, secureId, StringComparison.OrdinalIgnoreCase) && 
                                          string.Equals(p.MinecraftName, selectedAccount, StringComparison.OrdinalIgnoreCase);
                    bool isPremiumMatch = string.Equals(p.Id, selectedAccount, StringComparison.OrdinalIgnoreCase);

                    if (isOfflineMatch || isPremiumMatch)
                    {
                        if (p.Modpacks != null)
                        {
                            foreach (var mp in p.Modpacks)
                            {
                                if (string.Equals(mp, modpackId, StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error al verificar la whitelist del jugador: {ex.Message}");
            }
            return false;
        }

        public static async Task InstallCustomSkinLoaderAsync(string instancePath, string gameVersion, string modLoader)
        {
            string modsDir = Path.Combine(instancePath, "mods");
            if (Directory.Exists(modsDir))
            {
                foreach (var file in Directory.GetFiles(modsDir, "*CustomSkinLoader*.jar"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
            else
            {
                Directory.CreateDirectory(modsDir);
            }

            try
            {
                string url = "https://api.modrinth.com/v2/project/custom-skin-loader/version";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "MiLauncher-Client/1.0");
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Modrinth API returned status {response.StatusCode} for CustomSkinLoader");
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

                string targetLoader = modLoader.ToLowerInvariant();
                if (targetLoader == "vanilla")
                {
                    Logger.Info("Modpack es Vanilla, omitiendo CustomSkinLoader.");
                    return;
                }

                JsonElement? bestVersion = null;
                JsonElement? bestFile = null;

                foreach (var ver in doc.RootElement.EnumerateArray())
                {
                    bool loaderMatch = false;
                    if (ver.TryGetProperty("loaders", out var loadersEl) && loadersEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var l in loadersEl.EnumerateArray())
                        {
                            if (string.Equals(l.GetString(), targetLoader, StringComparison.OrdinalIgnoreCase))
                            {
                                loaderMatch = true;
                                break;
                            }
                        }
                    }

                    if (!loaderMatch) continue;

                    bool gameVersionMatch = false;
                    if (ver.TryGetProperty("game_versions", out var gvsEl) && gvsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var gv in gvsEl.EnumerateArray())
                        {
                            if (string.Equals(gv.GetString(), gameVersion, StringComparison.OrdinalIgnoreCase))
                            {
                                gameVersionMatch = true;
                                break;
                            }
                        }
                    }

                    if (!gameVersionMatch) continue;

                    if (ver.TryGetProperty("files", out var filesEl) && filesEl.ValueKind == JsonValueKind.Array && filesEl.GetArrayLength() > 0)
                    {
                        JsonElement selectedFile = filesEl[0];
                        foreach (var f in filesEl.EnumerateArray())
                        {
                            if (f.TryGetProperty("primary", out var prim) && prim.GetBoolean())
                            {
                                selectedFile = f;
                                break;
                            }
                        }
                        bestVersion = ver;
                        bestFile = selectedFile;
                        break;
                    }
                }

                if (bestFile != null && bestFile.Value.TryGetProperty("url", out var fileUrlEl) && bestFile.Value.TryGetProperty("filename", out var filenameEl))
                {
                    string fileUrl = fileUrlEl.GetString() ?? "";
                    string filename = filenameEl.GetString() ?? "";
                    if (!string.IsNullOrEmpty(fileUrl) && !string.IsNullOrEmpty(filename))
                    {
                        string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "cache", "mods");
                        Directory.CreateDirectory(cacheDir);
                        string cacheFilePath = Path.Combine(cacheDir, filename);

                        if (!File.Exists(cacheFilePath))
                        {
                            Logger.Info($"Descargando CustomSkinLoader desde Modrinth a caché: {filename}");
                            using var req = new HttpRequestMessage(HttpMethod.Get, fileUrl);
                            req.Headers.Add("User-Agent", "MiLauncher-Client/1.0");
                            using var res = await _httpClient.SendAsync(req);
                            if (res.IsSuccessStatusCode)
                            {
                                using var fs = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                                await res.Content.CopyToAsync(fs);
                            }
                        }

                        if (File.Exists(cacheFilePath))
                        {
                            string destPath = Path.Combine(modsDir, filename);
                            File.Copy(cacheFilePath, destPath, true);
                            Logger.Info($"CustomSkinLoader {filename} instalado en la instancia.");
                            return;
                        }
                    }
                }

                ui.SafeDispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("Esta versión no soporta las skins.", "CustomSkinLoader", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error buscando/instalando CustomSkinLoader dinámicamente: {ex.Message}");
            }
        }
    }

    public class PlayerWhitelistEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("minecraft_name")]
        public string MinecraftName { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("discord_name")]
        public string DiscordName { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("modpacks")]
        public List<string> Modpacks { get; set; } = new List<string>();
    }
}
