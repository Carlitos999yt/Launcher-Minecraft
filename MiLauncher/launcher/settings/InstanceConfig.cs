using System;
using System.IO;
using System.Text.Json;

namespace MiLauncher.launcher.settings
{
    public class InstanceConfig
    {
        // General
        public bool UseGlobalSettings { get; set; } = true;
        public string Name { get; set; } = "";
        public string GameVersion { get; set; } = "1.20.1";
        public string ModLoader { get; set; } = "Vanilla"; // Vanilla, Fabric, Forge
        public string LoaderVersion { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        
        // Java & RAM
        public string JavaPath { get; set; } = "";
        public int MinMem { get; set; } = 1024;
        public int MaxMem { get; set; } = 4096;
        public int PermGen { get; set; } = 256;
        public string JvmArgs { get; set; } = "";
        public string WrapperCommand { get; set; } = "";

        public bool Fullscreen { get; set; } = false;
        public int ResolutionWidth { get; set; } = 854;
        public int ResolutionHeight { get; set; } = 480;
        public bool MaximizeWindow { get; set; } = false;
        public string PreLaunchCommand { get; set; } = "";
        public string PostExitCommand { get; set; } = "";

        public static InstanceConfig Load(string instancePath)
        {
            string configPath = Path.Combine(instancePath, "instance_config.json");
            if (!File.Exists(configPath))
            {
                return new InstanceConfig(); // Default values (UseGlobalSettings = true)
            }
            try
            {
                string json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<InstanceConfig>(json) ?? new InstanceConfig();
            }
            catch (Exception)
            {
                return new InstanceConfig();
            }
        }

        public void Save(string instancePath)
        {
            Directory.CreateDirectory(instancePath);
            string configPath = Path.Combine(instancePath, "instance_config.json");
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
    }
}
