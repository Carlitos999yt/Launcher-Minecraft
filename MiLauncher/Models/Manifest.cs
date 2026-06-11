using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiLauncher.Models
{
    public class CurseForgeManifest
    {
        [JsonPropertyName("minecraft")]
        public MinecraftInfo? Minecraft { get; set; }

        [JsonPropertyName("manifestType")]
        public string? ManifestType { get; set; }

        [JsonPropertyName("manifestVersion")]
        public int ManifestVersion { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("files")]
        public List<ModFile>? Files { get; set; }
    }

    public class MinecraftInfo
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }
        
        [JsonPropertyName("modLoaders")]
        public List<ModLoader>? ModLoaders { get; set; }
    }

    public class ModLoader
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class ModFile
    {
        [JsonPropertyName("projectID")]
        public int ProjectId { get; set; }

        [JsonPropertyName("fileID")]
        public int FileId { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }
    }
}
