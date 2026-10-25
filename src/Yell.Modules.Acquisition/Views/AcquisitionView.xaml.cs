using System.Windows.Controls;
using Yell.Modules.Acquisition.ViewModels;

namespace Yell.Modules.Acquisition.Views
{
    public partial class AcquisitionView : UserControl
    {
        public AcquisitionView(AcquisitionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
