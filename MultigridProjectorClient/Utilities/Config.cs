using MultigridProjector.Utilities;
using System;
using System.IO;
using System.Xml.Serialization;
using VRage.FileSystem;

namespace MultigridProjectorClient.Utilities
{
    public class ConfigObject
    {
        public bool ShowDialogs = true;

        public bool ClientWelding = true;
        public bool ShipWelding = true;
        public bool ConnectSubgrids = true;

        public bool RepairProjection = true;
        public bool ProjectorAligner = true;
        public bool BlockHighlight = true;
        public bool CraftProjection = true;
    }

    internal static class Config
    {
        public static ConfigObject CurrentConfig { get; private set; } = new ConfigObject();

        // Config file location: %AppData%/SpaceEngineers/Storage/MultigridProjector.cfg
        private static readonly string configPath = Path.Combine(MyFileSystem.UserDataPath, "Storage", "MultigridProjector.cfg");

        private static readonly XmlSerializer configSerializer = new XmlSerializer(typeof(ConfigObject));

        public static bool SaveConfig()
        {
            ConfigObject config = CurrentConfig;
            try
            {
                using (TextWriter writer = File.CreateText(configPath))
                {
                    configSerializer.Serialize(writer, config);
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Could not save config");
                return false;
            }

            return true;
        }

        public static void ResetConfig()
        {
            CurrentConfig = new ConfigObject();
            SaveConfig();
        }

        public static bool LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                PluginLog.Warn("Could not find config file");
                return false;
            }

            try
            {
                using (StreamReader reader = new StreamReader(configPath))
                {
                    CurrentConfig = (ConfigObject)configSerializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Could not load config");
                return false;
            }

            return true;
        }
    }
}