using System;
using System.IO;
using System.Xml.Serialization;
using NLog;
using Torch;
using Torch.Views;

namespace MultigridProjectorServer
{
    [Serializable]
    public class MultigridProjectorConfig : ViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string ConfigFileName = "MultigridProjector.cfg";
        private static bool _loading;

        private bool _enablePlugin = true;
        private bool _blockLimit;
        private bool _pcuLimit;

        private static MultigridProjectorConfig _instance;
        public static MultigridProjectorConfig Instance => _instance ?? (_instance = new MultigridProjectorConfig());
        private static XmlSerializer ConfigSerializer => new XmlSerializer(typeof(MultigridProjectorConfig));
        private static string ConfigFilePath => Path.Combine(MultigridProjectorPlugin.Instance.StoragePath, ConfigFileName);

        [Display(Description = "Enables/disables the plugin", Name = "Enable Plugin", Order = 1)]
        public bool EnablePlugin
        {
            get => _enablePlugin;
            set
            {
                _enablePlugin = value;
                OnPropertyChanged(nameof(EnablePlugin));
            }
        }

        [Display(Description = "Enforce player block limit", Name = "Block limit", Order = 2)]
        public bool BlockLimit
        {
            get => _blockLimit;
            set
            {
                _blockLimit = value;
                OnPropertyChanged(nameof(BlockLimit));
            }
        }

        [Display(Description = "Enforce player PCU limit", Name = "PCU limit", Order = 2)]
        public bool PcuLimit
        {
            get => _pcuLimit;
            set
            {
                _pcuLimit = value;
                OnPropertyChanged(nameof(PcuLimit));
            }
        }

        protected override void OnPropertyChanged(string propName = "")
        {
            // FIXME: Frequent saving causes exception due to the file still being open. What?!
            //Save();
        }

        private void UnsafeSave()
        {
            using (var streamWriter = new StreamWriter(ConfigFilePath))
            {
                ConfigSerializer.Serialize(streamWriter, _instance);
            }
        }

        private void UnsafeLoad(string path)
        {
            using (var streamReader = new StreamReader(path))
            {
                if (!(ConfigSerializer.Deserialize(streamReader) is MultigridProjectorConfig config))
                {
                    Log.Error($"Failed to deserialize configuration file: {path}");
                    return;
                }

                _instance = config;
            }
        }

        public void Save()
        {
            if (_loading)
                return;

            lock (this)
            {
                try
                {
                    UnsafeSave();
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Failed to save configuration file: {ConfigFilePath}");
                }
            }
        }

        public void Load()
        {
            _loading = true;
            lock (this)
            {
                var path = ConfigFilePath;
                try
                {
                    if (!File.Exists(path))
                    {
                        Log.Warn($"Missing configuration file. Saving default one: {path}");
                        UnsafeSave();
                        return;
                    }

                    UnsafeLoad(path);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Failed to load configuration file: {path}");
                }
                finally
                {
                    _loading = false;
                }
            }
        }
    }
}