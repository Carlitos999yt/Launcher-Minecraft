using System;

namespace MiLauncher.launcher.minecraft
{
    /// <summary>
    /// Equivalent to MiLauncher's BaseInstance
    /// Represents a Minecraft instance loaded in the launcher.
    /// </summary>
    public abstract class BaseInstance
    {
        public string Name { get; set; }
        public string InstancePath { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }

        public BaseInstance(string name, string instancePath)
        {
            Name = name;
            InstancePath = instancePath;
        }

        public abstract void Init();
        public abstract void Load();
    }
}
