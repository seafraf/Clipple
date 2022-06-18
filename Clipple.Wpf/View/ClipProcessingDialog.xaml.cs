using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for ClipProcessingDialog.xaml
    /// </summary>
    public partial class ClipProcessingDialog : BaseMetroDialog
    {
        public ClipProcessingDialog(ViewModel.ClipProcessingDialogViewModel clipProcessingDialogViewModel)
        {
            InitializeComponent();
            DataContext = clipProcessingDialogViewModel;
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = true,
                Arguments = $"/select,\"{e.Uri.AbsoluteUri}\""
            });
            e.Handled = true;
        }
    }
}
