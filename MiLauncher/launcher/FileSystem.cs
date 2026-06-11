using System;
using System.IO;
using System.Linq;

namespace MiLauncher.launcher
{
    /// <summary>
    /// Equivalent to MiLauncher's FileSystem
    /// Encapsulates all disk IO operations for the launcher.
    /// </summary>
    public static class FileSystem
    {
        public static void EnsureExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception ex)
                {
                    throw new IOException($"Unable to create folder {new DirectoryInfo(dir).Name} ({dir})", ex);
                }
            }
        }

        public static void Write(string filename, byte[] data)
        {
            EnsureExists(Path.GetDirectoryName(filename));
            try
            {
                File.WriteAllBytes(filename, data);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error writing data to {filename}: {ex.Message}", ex);
            }
        }

        public static void AppendSafe(string filename, byte[] data)
        {
            EnsureExists(Path.GetDirectoryName(filename));
            byte[] buffer;
            try
            {
                buffer = Read(filename);
            }
            catch (IOException)
            {
                buffer = Array.Empty<byte>();
            }

            var newBuffer = new byte[buffer.Length + data.Length];
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
            Buffer.BlockCopy(data, 0, newBuffer, buffer.Length, data.Length);

            try
            {
                File.WriteAllBytes(filename, newBuffer);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error writing data to {filename}: {ex.Message}", ex);
            }
        }

        public static void Append(string filename, byte[] data)
        {
            EnsureExists(Path.GetDirectoryName(filename));
            try
            {
                using var stream = new FileStream(filename, FileMode.Append, FileAccess.Write);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error writing data to {filename}: {ex.Message}", ex);
            }
        }

        public static byte[] Read(string filename)
        {
            try
            {
                return File.ReadAllBytes(filename);
            }
            catch (Exception ex)
            {
                throw new IOException($"Unable to open {filename} for reading: {ex.Message}", ex);
            }
        }

        public static bool UpdateTimestamp(string filename)
        {
            try
            {
                File.SetLastWriteTime(filename, DateTime.Now);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool EnsureFilePathExists(string filenamepath)
        {
            var dir = Path.GetDirectoryName(filenamepath);
            if (string.IsNullOrEmpty(dir)) return false;
            
            try
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool EnsureFolderPathExists(string folderPathName)
        {
            if (Directory.Exists(folderPathName))
                return true;

            try
            {
                Directory.CreateDirectory(folderPathName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CopyFileAttributes(string src, string dst)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    File.SetAttributes(dst, File.GetAttributes(src));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void CopyFolderAttributes(string src, string dst, string relative)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            
            var path = PathCombine(src, relative);
            var srcDir = new DirectoryInfo(src);
            
            while (path.Length >= src.Length && !string.IsNullOrEmpty(Path.GetDirectoryName(path)))
            {
                path = Path.GetDirectoryName(path);
                if (path == null) break;
                
                var relativePath = Path.GetRelativePath(src, path);
                var dstPath = PathCombine(dst, relativePath);
                CopyFileAttributes(path, dstPath);
            }
        }

        public static bool MoveByCopy(string source, string dest)
        {
            try
            {
                File.Copy(source, dest, true);
                if (!DeletePath(source))
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Move(string source, string dest)
        {
            EnsureFilePathExists(dest);
            try
            {
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }
                File.Move(source, dest);
                return true;
            }
            catch
            {
                return MoveByCopy(source, dest);
            }
        }

        public static bool DeletePath(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteContents(string path)
        {
            if (!Directory.Exists(path))
            {
                return true;
            }

            bool ret = true;
            var dirInfo = new DirectoryInfo(path);

            foreach (var file in dirInfo.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    ret = false;
                }
            }

            foreach (var dir in dirInfo.GetDirectories())
            {
                try
                {
                    dir.Delete(true);
                }
                catch
                {
                    ret = false;
                }
            }

            return ret;
        }

        public static string PathCombine(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1)) return path2;
            if (string.IsNullOrEmpty(path2)) return path1;
            return Path.GetFullPath(Path.Combine(path1, path2));
        }

        public static string PathCombine(string path1, string path2, string path3)
        {
            return PathCombine(PathCombine(path1, path2), path3);
        }

        public static string PathCombine(string path1, string path2, string path3, string path4)
        {
            return PathCombine(PathCombine(path1, path2, path3), path4);
        }

        public static string AbsolutePath(string path)
        {
            return Path.GetFullPath(path);
        }

        public static int PathDepth(string path)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            var info = new DirectoryInfo(path);
            var parts = info.FullName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            int numParts = parts.Length;
            numParts -= parts.Count(p => p == ".");
            numParts -= parts.Count(p => p == "..") * 2;
            return numParts;
        }
        
        public static string ResolveExecutable(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            if (!path.Contains('/') && !path.Contains('\\'))
            {
                var values = Environment.GetEnvironmentVariable("PATH");
                if (values != null)
                {
                    foreach (var p in values.Split(Path.PathSeparator))
                    {
                        var fullPath = Path.Combine(p, path);
                        if (File.Exists(fullPath))
                            return fullPath;
                        
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            if (File.Exists(fullPath + ".exe"))
                                return fullPath + ".exe";
                        }
                    }
                }
                return string.Empty;
            }

            return File.Exists(path) ? path : string.Empty;
        }
    }
}
