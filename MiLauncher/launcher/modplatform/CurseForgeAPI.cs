using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace MiLauncher.launcher.modplatform
{
    /// <summary>
    /// Client for interacting with the CurseForge Core API.
    /// </summary>
    public class CurseForgeAPI
    {
        private static readonly string BaseUrl = "https://api.curseforge.com/v1";
        private static readonly HttpClient client = new HttpClient();
        
        // This requires an API key in production, usually passed via headers.
        private static string _apiKey = "";

        public static void SetApiKey(string key)
        {
            _apiKey = key;
            if (client.DefaultRequestHeaders.Contains("x-api-key"))
                client.DefaultRequestHeaders.Remove("x-api-key");
            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public class Mod
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Summary { get; set; }
            public int DownloadCount { get; set; }
            public ModLinks Links { get; set; }
            public ModAsset Logo { get; set; }
        }

        public class ModLinks
        {
            public string WebsiteUrl { get; set; }
            public string WikiUrl { get; set; }
            public string IssuesUrl { get; set; }
            public string SourceUrl { get; set; }
        }

        public class ModAsset
        {
            public int Id { get; set; }
            public string Url { get; set; }
            public string ThumbnailUrl { get; set; }
        }

        public static async Task<Mod> GetModAsync(int modId)
        {
            if (string.IsNullOrEmpty(_apiKey)) return null;

            var response = await client.GetAsync($"{BaseUrl}/mods/{modId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<JsonElement>(content);
                var data = root.GetProperty("data").GetRawText();
                return JsonSerializer.Deserialize<Mod>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
    }
}
