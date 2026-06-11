using System;
using System.Runtime.InteropServices;
using System.Management; // Solo Windows

namespace MiLauncher.launcher.tools
{
    /// <summary>
    /// Equivalent to MiLauncher's SysInfo
    /// Exposes system and architecture information.
    /// </summary>
    public static class SysInfo
    {
        public static string CurrentSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return "freebsd";
            return "unknown";
        }

        public static string UseQTForArch()
        {
            var arch = RuntimeInformation.OSArchitecture;
            if (arch == Architecture.X64) return "x86_64";
            if (arch == Architecture.X86) return "i386";
            if (arch == Architecture.Arm64) return "arm64";
            return arch.ToString().ToLower();
        }

        public static int DefaultMaxJvmMem()
        {
            ulong totalRAMBytes = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            totalRAMBytes = Convert.ToUInt64(obj["TotalVisibleMemorySize"]) * 1024; // It returns KB
                        }
                    }
                }
                catch
                {
                    totalRAMBytes = (ulong)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                }
            }
            else
            {
                totalRAMBytes = (ulong)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            }

            ulong totalRamMiB = totalRAMBytes / (1024 * 1024);

            // Memory allocation logic: If totalRAM < 6GB, use (totalRAM / 1.5), else 4GB
            if (totalRamMiB < (4096 * 1.5))
                return (int)(totalRamMiB / 1.5);
            else
                return 4096;
        }

        public static string GetSupportedJavaArchitecture()
        {
            var sys = CurrentSystem();
            var arch = UseQTForArch();

            if (sys == "windows")
            {
                if (arch == "x86_64") return "windows-x64";
                if (arch == "i386") return "windows-x86";
                return "windows-" + arch;
            }
            
            if (sys == "osx")
            {
                if (arch == "arm64") return "mac-os-arm64";
                if (arch.Contains("64")) return "mac-os-x64";
                if (arch.Contains("86")) return "mac-os-x86";
                return "mac-os-" + arch;
            }
            else if (sys == "linux")
            {
                if (arch == "x86_64") return "linux-x64";
                if (arch == "i386") return "linux-x86";
                return "linux-" + arch;
            }

            return string.Empty;
        }
    }
}
