using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NDDownload
{
    /// <summary>
    /// InstallHelp.xaml 的交互逻辑
    /// </summary>
    public partial class InstallHelp : Window
    {
        public InstallHelp()
        {
            InitializeComponent();
            string about = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\About.txt";
            Encoding encod = Encoding.UTF8;
            if (File.Exists(about))
            {
                string about_text = File.ReadAllText(about, encod);
                textBox.Text = about_text;
            }
            else {
                textBox.Text = "日志下载失败";
            }
        }
    }
}
