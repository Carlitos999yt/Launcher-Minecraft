using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.UI.Wpf;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MiLauncher.launcher.tools;

namespace MiLauncher.launcher.net
{
    public static class AuthManager
    {
        private static readonly string ConfigDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        private static readonly string AuthFile = Path.Combine(ConfigDir, "auth.json");
        
        public static MSession? CurrentSession { get; private set; }

        public static async Task<MSession?> LoginAsync()
        {
            try
            {
                var loginWindow = new MicrosoftLoginWindow();
                MSession session = await loginWindow.ShowLoginDialog();

                if (session != null)
                {
                    CurrentSession = session;
                    return session;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error en login Microsoft: {ex.Message}");
            }

            return null;
        }

        public static Task<MSession?> AutoLoginAsync()
        {
            // AutoLogin no disponible tan fácil en v2 sin manejar tokens manualmente.
            return Task.FromResult<MSession?>(null);
        }

        public static void SetOfflineSession(string username)
        {
            CurrentSession = MSession.CreateOfflineSession(username);
        }
    }
}
