using NDDownload.Download;
using System.Windows;

namespace NDDownloadIn
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            InstallChannel.ConfigureFromEntryAssembly();
            base.OnStartup(e);
        }
    }
}
