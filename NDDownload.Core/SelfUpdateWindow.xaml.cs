using System.Windows;

namespace NDDownload
{
    public partial class SelfUpdateWindow : Window
    {
        public SelfUpdateWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetStatus(text));
                return;
            }
            StatusText.Text = text ?? string.Empty;
        }

        public void SetProgress(double percent, bool indeterminate = false)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetProgress(percent, indeterminate));
                return;
            }
            Progress.IsIndeterminate = indeterminate;
            if (!indeterminate)
            {
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                Progress.Value = percent;
                PercentText.Text = $"{percent:0}%";
            }
            else
            {
                PercentText.Text = string.Empty;
            }
        }
    }
}
