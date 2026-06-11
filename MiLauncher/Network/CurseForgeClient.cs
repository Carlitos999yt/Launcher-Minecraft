using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MiLauncher.launcher.tools;

namespace MiLauncher.Network
{
    public class CurseForgeClient
    {
        private readonly HttpClient _client;
        
        // Proxy público simulado para no quemar tokens privados en el código abierto.
        private const string ProxyUrl = "https://curse.proxy.example/v1";

        public CurseForgeClient()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<string?> GetModFileUrlAsync(int projectId, int fileId)
        {
            try
            {
                string url = $"{ProxyUrl}/mods/{projectId}/files/{fileId}";
                var response = await _client.GetStringAsync(url);
                
                using var document = JsonDocument.Parse(response);
                if (document.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("downloadUrl", out var downloadUrl))
                {
                    return downloadUrl.GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Falló la lectura de CurseForge API (Proyecto: {projectId})", ex);
                return null;
            }
        }
    }
}
