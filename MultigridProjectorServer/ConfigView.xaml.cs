using System.Windows.Controls;

namespace MultigridProjectorServer
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once RedundantExtendsListEntry
    public partial class ConfigView : UserControl
    {
        public ConfigView()
        {
            InitializeComponent();
            DataContext = MultigridProjectorConfig.Instance;
        }
    }
}