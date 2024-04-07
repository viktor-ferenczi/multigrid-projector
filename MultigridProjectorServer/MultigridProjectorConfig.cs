using System;
using System.IO;
using Torch;
using Torch.Views;

namespace MultigridProjectorServer
{
    [Serializable]
    public class MultigridProjectorConfig : ViewModel
    {
        private const string ConfigFileName = "MultigridProjector.cfg";
        public static string ConfigFilePath => Path.Combine(MultigridProjectorPlugin.Instance.StoragePath, ConfigFileName);

        private bool setPreviewBlockVisuals;

        [Display(Order = 1, GroupName = "Compatibility", Name = "Set preview block visuals", Description = "Compatibility with mods depending on preview block transparency.")]
        public bool SetPreviewBlockVisuals
        {
            get => setPreviewBlockVisuals;
            set
            {
                setPreviewBlockVisuals = value;
                OnPropertyChanged();
            }
        }

#if INCOMPLETE_UNTESTED
        private bool _blockLimit;
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

        private bool _pcuLimit;
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
#endif
    }
}