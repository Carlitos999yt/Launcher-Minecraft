using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MiLauncher.Models;
using MiLauncher.launcher.tools;

namespace MiLauncher.launcher.net
{
    public static class SkinManager
    {
        private static readonly string ConfigDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        private static readonly string SkinsFile = Path.Combine(ConfigDir, "skins.json");

        public static void SaveSkins(IEnumerable<PlayerSkin> skins, PlayerSkin currentSkin)
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                    Directory.CreateDirectory(ConfigDir);

                var data = new SkinData 
                { 
                    Skins = new List<PlayerSkin>(skins),
                    CurrentSkinId = currentSkin?.Id
                };

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SkinsFile, json);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error guardando skins.json: {ex.Message}");
            }
        }

        public static SkinData LoadSkins()
        {
            try
            {
                if (!File.Exists(SkinsFile)) return new SkinData { Skins = new List<PlayerSkin>() };

                string json = File.ReadAllText(SkinsFile);
                var data = JsonSerializer.Deserialize<SkinData>(json);
                return data ?? new SkinData { Skins = new List<PlayerSkin>() };
            }
            catch (Exception ex)
            {
                Logger.Error($"Error leyendo skins.json: {ex.Message}");
                return new SkinData { Skins = new List<PlayerSkin>() };
            }
        }
    }

    public class SkinData
    {
        public List<PlayerSkin> Skins { get; set; } = new List<PlayerSkin>();
        public string CurrentSkinId { get; set; }
    }
}
