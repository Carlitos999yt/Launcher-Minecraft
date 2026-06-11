using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace MiLauncher.launcher.java
{
    /// <summary>
    /// Equivalent to MiLauncher's JavaCommon
    /// Java auto-detection and validation utilities.
    /// </summary>
    public static class JavaCommon
    {
        public static string FindDefaultJava()
        {
            var envPath = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(envPath))
            {
                var exePath = System.IO.Path.Combine(envPath, "bin", "java.exe");
                if (System.IO.File.Exists(exePath)) return exePath;
            }
            return null;
        }

        public static bool CheckJVMArgs(string jvmargs)
        {
            if (string.IsNullOrEmpty(jvmargs)) return true;

            var memRegex = new Regex("-Xm[sx]");
            var versionRegex = new Regex("-version:.*");

            if (jvmargs.Contains("-XX:PermSize=") || memRegex.IsMatch(jvmargs) || 
                jvmargs.Contains("-XX-MaxHeapSize") || jvmargs.Contains("-XX:InitialHeapSize"))
            {
                var warnStr = "You tried to manually set a JVM memory option (using \"-XX:PermSize\", \"-XX-MaxHeapSize\", \"-XX:InitialHeapSize\", \"-Xmx\" " +
                              "or \"-Xms\").\n" +
                              "There are dedicated boxes for these in the settings (Java tab, in the Memory group at the top).\n" +
                              "This message will be displayed until you remove them from the JVM arguments.";
                
                Application.Current.Dispatcher.Invoke(() => {
                    MessageBox.Show(warnStr, "JVM arguments warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return false;
            }

            // block lunacy with passing required version to the JVM
            if (versionRegex.IsMatch(jvmargs))
            {
                var warnStr = "You tried to pass required Java version argument to the JVM (using \"-version:xxx\"). This is not safe and will not be allowed.\n" +
                              "This message will be displayed until you remove this from the JVM arguments.";
                
                Application.Current.Dispatcher.Invoke(() => {
                    MessageBox.Show(warnStr, "JVM arguments warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return false;
            }

            return true;
        }

        public static void JavaWasOk(string platform, string version, string vendor, string errorLog)
        {
            string text = $"Java test succeeded!\nPlatform reported: {platform}\nJava version reported: {version}\nJava vendor reported: {vendor}\n";
            
            if (!string.IsNullOrEmpty(errorLog))
            {
                text += $"\nWarnings:\n{errorLog}";
            }
            
            Application.Current.Dispatcher.Invoke(() => {
                MessageBox.Show(text, "Java test success", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        public static void JavaArgsWereBad(string errorLog)
        {
            string text = "The specified Java binary didn't work with the arguments you provided:\n\n" + errorLog;
            Application.Current.Dispatcher.Invoke(() => {
                MessageBox.Show(text, "Java test failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        public static void JavaBinaryWasBad()
        {
            string text = "The specified Java binary didn't work.\nYou should press 'Detect', or set the path to the Java executable.\n";
            Application.Current.Dispatcher.Invoke(() => {
                MessageBox.Show(text, "Java test failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        public static void JavaCheckNotFound()
        {
            string text = "Java checker library could not be found. Please check your installation.";
            Application.Current.Dispatcher.Invoke(() => {
                MessageBox.Show(text, "Java test failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
    }
}
