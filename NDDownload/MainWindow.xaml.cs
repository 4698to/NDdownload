using Microsoft.WindowsAPICodePack.Dialogs;
using NDDownload.Download;
using NDDownload.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
using IWshRuntimeLibrary;
namespace NDDownload
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public InstallViewModel _installtree;
        public MainWindow()
        {
            InitializeComponent();
            
            _installtree = new InstallViewModel(GetInstallItem.GetMax());
            InstallChannel.ApplyFixedServerToViewModel(_installtree);
            _installtree.ShowGif = ResourcesUrl.GetShowGif();
            this.DataContext = _installtree;
            this.Title = _installtree.WindowTitle;

            _installtree.Start();//下载服务器上内容清单，

            _installtree.GetScriptPath();//获取用户配置的自定义脚本文件夹
            _installtree.GetToolsList();//获取用户设置的工具列表配置文件

            //启动下载器就去掉 max2015 的盒子UI配置
            ResetMaxCUI.Reset();


            //显示的版本号
            show_version.Text = $"Version : {ResourcesUrl.version} , {ResourcesUrl.buildtime}";

            // 获取桌面路径
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 快捷方式的目标路径（你要创建快捷方式的应用程序路径）
            string targetPath = InstallChannel.GetInstalledExePath();
            if (System.IO.File.Exists(targetPath))
            {
                string short_path = System.IO.Path.Combine(desktopPath, "天晴盒子.lnk");
                if (!System.IO.File.Exists(short_path))
                {
                    // 创建快捷方式
                    CreateShortcut(short_path, targetPath);
                }
            }

        }

        static void CreateShortcut(string shortcutPath, string targetPath)
        {
            // 创建WshShell对象
            WshShell shell = new WshShell();

            // 创建快捷方式对象
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            // 设置快捷方式的目标路径
            shortcut.TargetPath = targetPath;

            // 设置快捷方式的工作目录（通常是目标路径的目录）
            shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(targetPath);

            // 设置快捷方式的描述
            shortcut.Description = "天晴盒子";

            // 设置快捷方式的图标（可选）
            shortcut.IconLocation = targetPath ;//"notepad.exe, 0"; // 这里可以指定图标路径和索引

            // 保存快捷方式
            shortcut.Save();
        }
        private void checkbox_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        
        private void btn_do_Click(object sender, RoutedEventArgs e)
        {
            if (_installtree.MaxInstallSelectCount > 0)
            {
                if (!_installtree.isClose)
                {
                    _installtree.GetDownloadItemList();

                    string process_name = "";
                    if (_installtree.quick)
                    {
                        _installtree.StartDownload();
                    }
                    else
                    {

                        if (WebAddress.checkMaxProcess(ref process_name))
                        {
                            _installtree.ShowMSG(process_name, "结束以下全部进程后重试");
                            
                        }
                        else
                        {
                            _installtree.StartDownload();
                        }
                    }
                }
                else { 
                    //安装完成之后再点按钮就会关闭窗口
                    _installtree.Close();
                }
            }
            else {
                _installtree.ShowMSG("请勾选需要安装的 3ds Max ", "没勾选啊");

            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _installtree.Save();
        }

        private void btn_uninstall_Click(object sender, RoutedEventArgs e)
        {
            /*if (_installtree.maxinstallselectcount > 0)
            {
                _installtree.uninstall();
            }*/
            _installtree.Uninstall();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine(sender);
            Hyperlink help = (Hyperlink)sender;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(help.NavigateUri.ToString()));
            //e.Handled = true;
        }

        private void select_all_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox check = (CheckBox)sender;
            _installtree.SelectAllMaxViewModel((bool)check.IsChecked);
        }
        private void select_all_file_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox check = (CheckBox)sender;
            _installtree.SelectAllFile((bool)check.IsChecked);
        }

        private void addnewpath_Click(object sender, RoutedEventArgs e)
        {
            string save_path = string.Empty;
            CommonOpenFileDialog commonsavefile = new CommonOpenFileDialog();
            commonsavefile.Title = "选择工具库路径";
            commonsavefile.IsFolderPicker = true;

            if (commonsavefile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                save_path = commonsavefile.FileName;
                if (!string.IsNullOrEmpty(save_path))
                {
                    _installtree.AddNewScriptPath(save_path);
                }
            }
        }

        private void remove_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                FrameworkElement parent = btn.Parent as FrameworkElement;
                if (parent != null)
                {
                    Grid panel = parent as Grid;
                    var scriptPath = panel?.DataContext as ScriptCollectPathsViewModel;
                    if (scriptPath != null)
                    {
                        _installtree.ScriptPaths.Remove(scriptPath);
                    }
                }
            }
        }

        private void Button_select_path_Click(object sender, RoutedEventArgs e)
        {
            string save_path = string.Empty;
            CommonOpenFileDialog commonsavefile = new CommonOpenFileDialog();
            commonsavefile.Title = "选择离线资源目录";
            commonsavefile.IsFolderPicker = true;

            if (commonsavefile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                save_path = commonsavefile.FileName;

                _installtree.UserResourceDirectory = save_path;
                _installtree.CanControl = false;

                _installtree.offline = true;
                _installtree.DownTitle = "";
                _installtree.CallOfflineMethod();

            }
            else { 
                _installtree.CanControl = false;
                _installtree.offline = true;
                _installtree.DownTitle = "";
            }
        }

        private void replace_resources_Click(object sender, RoutedEventArgs e)
        {
            
            if (_installtree.MaxInstallSelectCount > 0)
            {

                if (!_installtree.isClose)
                {
                    _installtree.GetDownloadItemList();

                    string process_name = "";
                    if (_installtree.quick)
                    {
                        //_installtree.StartDownload();
                        _installtree.OfflineMain();

                    }
                    else
                    {

                        if (WebAddress.checkMaxProcess(ref process_name))
                        {
                            _installtree.ShowMSG(process_name, "结束以下全部进程后重试");

                        }
                        else
                        {
                            _installtree.OfflineMain();

                        }
                    }
                }
                else
                {
                    //安装完成之后再点按钮就会关闭窗口
                    _installtree.Close();
                }
            }
            else
            {
                _installtree.ShowMSG("请勾选需要安装的 3ds Max ", "没勾选啊");

            }
        }

        private void addNewXml_Click(object sender, RoutedEventArgs e)
        {
            string open_path = string.Empty;
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog();
            commonOpenFileDialog.Title = "选择 XML 工具库 文件 ";
            commonOpenFileDialog.Filters.Add(new CommonFileDialogFilter("xml 工具库配置",".xml"));

            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                open_path = commonOpenFileDialog.FileName;
                if (!string.IsNullOrEmpty(open_path) && System.IO.File.Exists(open_path))
                {
                    //_installtree.AddNewScriptPath(save_path);
                    _installtree.AddNewXMLToolsList(open_path);
                }
            }
        }

        private void removeXMLPath_Click_1(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                FrameworkElement parent = btn.Parent as FrameworkElement;
                if (parent != null)
                {
                    Grid panel = parent as Grid;
                    var xmlPath = panel?.DataContext as ScriptCollectPathsViewModel;
                    if (xmlPath != null)
                    {
                        _installtree.XMLPaths.Remove(xmlPath);
                    }
                }
            }
        }

        private void open_temp_path_Click(object sender, RoutedEventArgs e)
        {
            ExplorerHelper.OpenFolder(ResourcesUrl.TempDownPath);
        }
    }
}
