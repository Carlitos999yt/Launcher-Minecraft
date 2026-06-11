using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace MiLauncher.launcher.modplatform
{
    /// <summary>
    /// Client for interacting with the Modrinth API v2.
    /// </summary>
    public class ModrinthAPI
    {
        private static readonly string BaseUrl = "https://api.modrinth.com/v2";
        private static readonly HttpClient client = new HttpClient();

        static ModrinthAPI()
        {
            // Set User-Agent as required by Modrinth terms of service
            client.DefaultRequestHeaders.Add("User-Agent", "MiLauncher/1.0");
        }

        public class Project
        {
            public string Id { get; set; }
            public string Slug { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public List<string> Categories { get; set; }
            public string ClientSide { get; set; }
            public string ServerSide { get; set; }
            public int Downloads { get; set; }
        }

        public class Version
        {
            public string Id { get; set; }
            public string ProjectId { get; set; }
            public string VersionNumber { get; set; }
            public List<string> GameVersions { get; set; }
            public List<string> Loaders { get; set; }
            public List<File> Files { get; set; }
        }

        public class File
        {
            public string Url { get; set; }
            public string Filename { get; set; }
            public bool Primary { get; set; }
            public int Size { get; set; }
        }

        public static async Task<Project> GetProjectAsync(string idOrSlug)
        {
            var response = await client.GetAsync($"{BaseUrl}/project/{idOrSlug}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Project>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }

        public static async Task<List<Version>> GetProjectVersionsAsync(string idOrSlug)
        {
            var response = await client.GetAsync($"{BaseUrl}/project/{idOrSlug}/version");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Version>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return new List<Version>();
        }

        public static async Task<Version> GetVersionAsync(string versionId)
        {
            var response = await client.GetAsync($"{BaseUrl}/version/{versionId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Version>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
    }
}
