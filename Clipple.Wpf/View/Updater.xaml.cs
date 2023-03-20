using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Clipple.View;

public partial class Updater
{
    public Updater()
    {
        InitializeComponent();
    }

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true
        });
        e.Handled = true;
    }
}