using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using NDDownload.ViewModel;

namespace NDDownload.Download
{
    public class OfflineInstallation
    {
        private StringBuilder Message;
        private InstallViewModel installview;
        private static string[] MaxPaths;

        public List<DownloadItem> DownloadList;
        private int count;

        public OfflineInstallation(string[] maxPaths, InstallViewModel _installview)
        {
            Message = new StringBuilder();
            MaxPaths = maxPaths;
            installview = _installview;
        }

        public void DownloadMain()
        {
            DownloadList = installview.GetDownloadItemList();
            count = DownloadList.Count;
            this.Message.AppendLine($"下载 {count}");

            this.ExtractZip(DownloadList);

            installview.SetlocalVersion();
            //Clear(DownloadList);

            //this.package = new PackageContents();


            installview.ButtonText = "安装完成";
            //installview.CanControl = true;
            installview.Save();
            installview.isClose = true;
        }
        public void FindDownloadItem(string path)
        {
            foreach (var i in this.installview.FileDownloadSelect)
            {
                if (i.FileNameContains(System.IO.Path.GetFileName(path)))
                {
                    i.isDone = true;
                    i.oldVersion = i.Version;
                }
            }
        }
        public void ExtractZip(List<DownloadItem> downloadItems)
        {
            string unistall_dir = $"{ResourcesUrl.ApplicationPlugins}Unistall";
            Directory.CreateDirectory(unistall_dir);

            foreach (DownloadItem item in downloadItems)
            {
                //设置选中的安装MAX版本
                try
                {
                    item.SetWillPath(this.installview.MaxInstallSelect, true);
                }
                catch
                {
                    Console.WriteLine($"downloadItem.SetUse -> {item.FileName}");
                }
                //替换 下载目录为用户设置的资源目录
                item.FolderPath = this.installview.UserResourceDirectory;
                item.SetTempGUIDPath();



                if (File.Exists(item.FileName))
                {
                    
                    this.FindDownloadItem(item.FileName);

                    //缓存目录
                    Directory.CreateDirectory(item.tempPath);
                    try
                    {
                        //解压到缓存目录
                        //ZipFile.ExtractToDirectory(item.FileName, item.tempPath, Encoding.UTF8);
                        ZipFile.ExtractToDirectory(item.FileName, item.tempPath);
                    }
                    catch
                    {
                        this.Message.AppendLine( $"文件损坏 {item.FileName}\n");
                        continue;
                    }

                    //Console.WriteLine($"解压 {item.FileName} ------> {item.tempPath}");

                    this.Message.AppendLine($"解压 {item.FileName} -->\n");
                    
                    this.Message.AppendLine($"  {item.tempPath}\n");

                    DirectoryInfo di = new DirectoryInfo(item.tempPath);
                    FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);


                    for (int i = 0; i < item.ExtractPath.Count; i++)
                    {
                        string ext_path = item.ExtractPath[i];

                        List<string> history_file = new List<string>();

                        foreach (var file in files)
                        {
                            string temp = file.Directory.FullName.Replace(item.tempPath, "");

                            //这里只处理了 被勾选的MAX版本中的文件，
                            string newpath = System.IO.Path.Combine(temp, file.Name);

                            newpath = System.IO.Path.Combine(ext_path, newpath.TrimStart('\\'));

                            history_file.Add(newpath);

                            try
                            {
                                if (File.Exists(newpath))
                                {
                                    File.Delete(newpath);
                                }
                                else
                                {
                                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newpath));
                                }
                                file.CopyTo(newpath);
                            }
                            catch { }
                        }

                        string history_file_pack = System.IO.Path.Combine(unistall_dir, item.MaxRoots[i], System.IO.Path.GetFileNameWithoutExtension(item.FileName));
                        history_file_pack = System.IO.Path.ChangeExtension(history_file_pack, ".txt");

                        if (File.Exists(history_file_pack))
                        {
                            File.Delete(history_file_pack);
                        }
                        else
                        {
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(history_file_pack));
                        }
                        File.WriteAllLines(history_file_pack, history_file, Encoding.UTF8);
                        this.Message.AppendLine($"安装记录 {history_file_pack}\n");
                    }

                    if (item.parent != null)
                    {
                        installview.SetInstallFileConfig(item.parent);
                    }

                    //保存当前安装的文件记录
                    this.installview.SetInstallFileConfig(item);

                }
            }
            this.installview.Message = Message.ToString();

        }
    }
}
