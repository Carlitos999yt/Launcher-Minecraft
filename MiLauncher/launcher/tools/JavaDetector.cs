using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiLauncher.launcher.tools
{
    public static class JavaDetector
    {
        public static string? GetBestJavaPath()
        {
            var javaPaths = GetInstalledJavas();
            
            // Tratamos de buscar la versión más moderna de Java (ej. Java 17 o 21 para Minecraft moderno)
            // Ordenamos heurísticamente por "17", "21", "16", "11", "8" si aparece en la ruta
            var preferred = javaPaths.OrderByDescending(p => 
            {
                if (p.Contains("21")) return 21;
                if (p.Contains("17")) return 17;
                if (p.Contains("16")) return 16;
                if (p.Contains("11")) return 11;
                if (p.Contains("8") || p.Contains("1.8")) return 8;
                return 0;
            }).FirstOrDefault();

            return preferred;
        }

        public static List<string> GetInstalledJavas()
        {
            var paths = new List<string>();

            // Lugares comunes en Windows
            var commonDirs = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            var javaFolders = new List<string>
            {
                "Java",
                "Eclipse Adoptium",
                "Amazon Corretto",
                "BellSoft",
                "Microsoft"
            };

            foreach (var baseDir in commonDirs)
            {
                if (string.IsNullOrEmpty(baseDir)) continue;

                foreach (var folder in javaFolders)
                {
                    string dir = Path.Combine(baseDir, folder);
                    if (Directory.Exists(dir))
                    {
                        try
                        {
                            var javas = Directory.GetFiles(dir, "java.exe", SearchOption.AllDirectories);
                            paths.AddRange(javas);
                        }
                        catch
                        {
                            // Ignorar errores de acceso
                        }
                    }
                }
            }

            // Eliminar duplicados
            return paths.Distinct().ToList();
        }

        public static int GetJavaMajorVersion(string javaPath)
        {
            try
            {
                if (string.IsNullOrEmpty(javaPath)) return 8;
                
                string lowerPath = javaPath.ToLowerInvariant();
                if (lowerPath.Contains("java-21") || lowerPath.Contains("jdk-21") || lowerPath.Contains("jdk21")) return 21;
                if (lowerPath.Contains("java-17") || lowerPath.Contains("jdk-17") || lowerPath.Contains("jdk17")) return 17;
                if (lowerPath.Contains("java-16") || lowerPath.Contains("jdk-16") || lowerPath.Contains("jdk16")) return 16;
                if (lowerPath.Contains("java-11") || lowerPath.Contains("jdk-11") || lowerPath.Contains("jdk11")) return 11;
                if (lowerPath.Contains("jre7") || lowerPath.Contains("jdk7") || lowerPath.Contains("1.7")) return 7;
                if (lowerPath.Contains("jre6") || lowerPath.Contains("jdk6") || lowerPath.Contains("1.6")) return 6;
                if (lowerPath.Contains("jre1.8") || lowerPath.Contains("jdk1.8") || lowerPath.Contains("1.8") || lowerPath.Contains("jre8")) return 8;

                // Fallback: Run process to check version
                var psi = new System.Diagnostics.ProcessStartInfo(javaPath, "-version")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process != null)
                    {
                        string output = process.StandardError.ReadToEnd();
                        process.WaitForExit(2000);
                        
                        var match = System.Text.RegularExpressions.Regex.Match(output, @"version\s+""(\d+)(\.(\d+))?");
                        if (match.Success)
                        {
                            int mainVer = int.Parse(match.Groups[1].Value);
                            if (mainVer == 1)
                            {
                                if (match.Groups[3].Success)
                                    return int.Parse(match.Groups[3].Value);
                            }
                            return mainVer;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
            return 8;
        }

        public static int GetJavaMajorVersionForMinecraft(string mcVersion)
        {
            if (string.IsNullOrEmpty(mcVersion)) return 8;
            if (mcVersion.StartsWith("1.20.5") || mcVersion.StartsWith("1.20.6") || mcVersion.StartsWith("1.21") || mcVersion.StartsWith("1.22"))
            {
                return 21;
            }
            if (mcVersion.StartsWith("1.17") || mcVersion.StartsWith("1.18") || mcVersion.StartsWith("1.19") || mcVersion.StartsWith("1.20"))
            {
                return 17;
            }
            if (mcVersion.StartsWith("1.16"))
            {
                return 16;
            }
            return 8;
        }

        public static string? GetBestJavaPathForVersion(string mcVersion)
        {
            var javaPaths = GetInstalledJavas();
            int reqVersion = GetJavaMajorVersionForMinecraft(mcVersion);

            // Find all paths matching the exact required major version
            var matches = javaPaths.Where(p => GetJavaMajorVersion(p) == reqVersion).ToList();
            if (matches.Any()) return matches.First();

            // Fallback: If no exact match, try to find a version that is compatible (>= reqVersion)
            var compatible = javaPaths.Where(p => GetJavaMajorVersion(p) >= reqVersion)
                                     .OrderBy(GetJavaMajorVersion)
                                     .FirstOrDefault();
            if (compatible != null) return compatible;

            // Ultimate fallback: return the best available java on system
            return GetBestJavaPath();
        }
    }
}
