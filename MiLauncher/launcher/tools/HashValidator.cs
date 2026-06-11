using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MiLauncher.launcher.tools
{
    public static class HashValidator
    {
        public static async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash)
        {
            if (!File.Exists(filePath)) return false;
            
            using var stream = File.OpenRead(filePath);
            using var sha1 = SHA1.Create();
            
            byte[] hashBytes = await sha1.ComputeHashAsync(stream);
            string fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return fileHash == expectedHash.ToLowerInvariant();
        }
    }
}
