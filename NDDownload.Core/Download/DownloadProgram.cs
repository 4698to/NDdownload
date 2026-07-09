using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using Downloader;
using System.Collections.Concurrent;
using System.Threading;
using System.Reflection;
using System.Windows.Controls;
using System.ComponentModel;
using NDDownload.ViewModel;
using System.IO.Compression;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace NDDownload.Download
{
    public class DownloadProgram
    {
        //版本信息
        public string RemoteUrl;//服务器地址
        //需要下载的文件
        public List<DownloadItem> DownloadList;
        private int count;
        //public PackageContents package;

        private static DownloadService CurrentDownloadService;
        private static DownloadConfiguration CurrentDownloadConfiguration;
        private static CancellationTokenSource CancelAllTokenSource;

        private InstallViewModel installview;
        //private static float _value;
        private static bool IsDownloadOk;
        private static string[] MaxPaths;
        public DownloadProgram(string[] maxPaths, InstallViewModel _installview) 
        { 
            CancelAllTokenSource = new CancellationTokenSource();
            MaxPaths = maxPaths;
            installview = _installview;
        }
        
        public bool CanControl
        {
            get { return installview.CanControl; }
            set {
                installview.CanControl = value;
            }
        }
        public float ProgressValue
        {
            get { return installview.ProgressValue; }
            set { installview.ProgressValue = value; }
        }
        public string Message
        {
            get { return installview.Message; }
            set { installview.Message = value; }
        }
        public bool DownloadOk
        {
            get { return IsDownloadOk; }
        }
        public List<DownloadItem> GetDownList()
        {
            return installview.GetDownloadItemList();
        }
        public void AppendLine(string str)
        {
            this.Message += str;
        }
        public async Task<bool> DownloadMain()
        {
            try
            {
                DownloadList = GetDownList();
                count = DownloadList.Count;
                AppendLine($"下载 {count}\n");

                if (count == 0)
                {
                    AppendLine("没有可下载的项目。\n");
                    CanControl = true;
                    return false;
                }

                await DownloadAll(DownloadList, CancelAllTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppendLine($"下载过程异常: {ex.Message}\n");
                CanControl = true;
                installview.ButtonText = "安装";
            }
            return false;
        }
        private async Task DownloadAll(IEnumerable<DownloadItem> downloadList, CancellationToken cancelToken)
        {
            foreach (DownloadItem downloadItem in downloadList)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                //设置选中的安装MAX版本
                try
                {
                    //downloadItem.SetUse(installview.MaxInstallSelect);
                    downloadItem.SetWillPath(installview.MaxInstallSelect,true);

                }
                catch (Exception ex)
                {
                    AppendLine($"路径设置失败 {downloadItem.FileName}: {ex.Message}\n");
                }
                try
                {
                    await DownloadFile(downloadItem).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    AppendLine($"下载失败 {downloadItem.FileName}: {ex.Message}\n");
                }
            }
        }
        /*private List<DownloadItem> GetDownloadItems(string remote)
        {
            float v = 0;
            List<DownloadItem> downloadList = GetInstallItem.ReadXml(WebAddress.contentLocal, remote ,out v );
            
            this.installview.GetFileItemOldVersion(downloadList);

            //this.installview.FileDownloadSelect;

            return downloadList;
        }*/
        private async Task<DownloadService> DownloadFile(DownloadItem downloadItem)
        {
            CurrentDownloadConfiguration = GetDownloadConfiguration();
            CurrentDownloadService = this.CreateDownloadService(CurrentDownloadConfiguration);
            
            Console.WriteLine($"{downloadItem.Url} ->{ downloadItem.FileName}");

            if (string.IsNullOrWhiteSpace(downloadItem.FileName))
            {
                await CurrentDownloadService.DownloadFileTaskAsync(downloadItem.Url, new DirectoryInfo(downloadItem.FolderPath)).ConfigureAwait(false);
            }
            else
            {
                if (File.Exists(downloadItem.FileName))
                {
                    File.Delete(downloadItem.FileName);
                }

                await CurrentDownloadService.DownloadFileTaskAsync(downloadItem.Url, downloadItem.FileName).ConfigureAwait(false);
            }

            return CurrentDownloadService;
        }
        private static DownloadConfiguration GetDownloadConfiguration()
        {
            var cookies = new CookieContainer();
            cookies.Add(new Cookie("download-type", "test") { Domain = "domain.com" });

            return new DownloadConfiguration
            {
                BufferBlockSize = 10240,    // usually, hosts support max to 8000 bytes, default values is 8000
                ChunkCount = 1,             // file parts to download, default value is 1
                MaximumBytesPerSecond = 0, // download speed limited to 10MB/s, default values is zero or unlimited
                MaxTryAgainOnFailover = 25,  // the maximum number of times to fail
                MaximumMemoryBufferBytes = 1024 * 1024 * 50, // release memory buffer after each 50 MB
                ParallelDownload = true,    // download parts of file as parallel or not. Default value is false
                ParallelCount = 4,          // number of parallel downloads. The default value is the same as the chunk count
                Timeout = 3000,             // timeout (millisecond) per stream block reader, default value is 1000
                RangeDownload = false,      // set true if you want to download just a specific range of bytes of a large file
                RangeLow = 0,               // floor offset of download range of a large file
                RangeHigh = 0,              // ceiling offset of download range of a large file
                ClearPackageOnCompletionWithFailure = true, // Clear package and downloaded data when download completed with failure, default value is false
                MinimumSizeOfChunking = 1024, // minimum size of chunking to download a file in multiple parts, default value is 512                                              
                ReserveStorageSpaceBeforeStartingDownload = false, // Before starting the download, reserve the storage space of the file as file size, default value is false
                RequestConfiguration =
                {
                    // config and customize request headers
                    Accept = "*/*",
                    CookieContainer = cookies,
                    Headers = new WebHeaderCollection(),     // { your custom headers }
                    KeepAlive = true,                        // default value is false
                    ProtocolVersion = HttpVersion.Version11, // default value is HTTP 1.1
                    UseDefaultCredentials = false,
                    // your custom user agent or your_app_name/app_version.
                    UserAgent = $"DownloaderSample/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}"
                    // Proxy = new WebProxy() {
                    //    Address = new Uri("http://YourProxyServer/proxy.pac"),
                    //    UseDefaultCredentials = false,
                    //    Credentials = System.Net.CredentialCache.DefaultNetworkCredentials,
                    //    BypassProxyOnLocal = true
                    // }
                }
            };
        }
        
        private DownloadService CreateDownloadService(DownloadConfiguration config)
        {
            var downloadService = new DownloadService(config);

            // Provide `FileName` and `TotalBytesToReceive` at the start of each downloads
            downloadService.DownloadStarted += this.OnDownloadStarted;

            // Provide any information about chunker downloads, 
            // like progress percentage per chunk, speed, 
            // total received bytes and received bytes array to live streaming.
            downloadService.ChunkDownloadProgressChanged += this.OnChunkDownloadProgressChanged;

            // Provide any information about download progress, 
            // like progress percentage of sum of chunks, total speed, 
            // average speed, total received bytes and received bytes array 
            // to live streaming.
            downloadService.DownloadProgressChanged += this.OnDownloadProgressChanged;

            // Download completed event that can include occurred errors or 
            // cancelled or download completed successfully.
            downloadService.DownloadFileCompleted +=this.OnDownloadFileCompleted;

            return downloadService;
        }
        private void OnDownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            this.ProgressValue = 0;
            this.CanControl = false;
            //ConsoleProgress = new ProgressBar(10000, $"Downloading {e.FileName}   ", ProcessBarOption);
        }
        private void OnChunkDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            //ChildProgressBar progress = ChildConsoleProgresses.GetOrAdd(e.ProgressId,
            //    id => ConsoleProgress?.Spawn(10000, $"chunk {id}", ChildOption));
            //progress.Tick((int)(e.ProgressPercentage * 100));
            //var activeChunksCount = e.ActiveChunks; // Running chunks count
        }
        private void OnDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            this.ProgressValue = (float)(e.ProgressPercentage );
            //ConsoleProgress.Tick((int)(* 100));
            //if (sender is DownloadService ds)
            //{
            //    e.UpdateTitleInfo(ds.IsPaused);
            //}
        }
        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var pack = (DownloadService)sender;

            if (e.Cancelled)
            {
                IsDownloadOk = false;
            }
            else if (e.Error != null)
            {
                
            }
            else
            {
                
                IsDownloadOk = true;
                //查找当前完成的 下载任务
                this.FindDownloadItem(pack.Package.FileName);

            }
            AppendLine($"{System.IO.Path.GetFileName(pack.Package.FileName)}\n");
            AppendLine(string.Concat(" -> ", CurrentDownloadService.Status.ToString() , "\r\n"));
            
            this.count -= 1;

            if (this.count == 0)
            {
                Console.WriteLine("开始解压任务");
                AppendLine("开始解压任务\n");

                ExtractZip(DownloadList);
                
                installview.SetlocalVersion();
                //Clear(DownloadList);

                //this.package = new PackageContents();


                installview.ButtonText = "安装完成";
                this.CanControl = true;
                installview.Save();

                installview.isClose = true;
            }
            
        }
        public void FindDownloadItem(string path)
        {
            foreach (var i in this.installview.FileDownloadSelect)
            {
                if (i.FileNameContains(System.IO.Path.GetFileName(path))) 
                { 
                    i.isDone = true;
                    i.oldVersion = i.Version;

                    //保存当前安装的文件记录
                    //this.installview.SetInstallFileConfig(i.Item);
                }
            }
        }
        
        public void ExtractZip(List<DownloadItem> downloadItems)
        {
            string unistall_dir = $"{ResourcesUrl.ApplicationPlugins}Unistall";
            Directory.CreateDirectory(unistall_dir);

            foreach (DownloadItem item in downloadItems)
            {
                if (File.Exists(item.FileName))
                {

                    //缓存目录
                    Directory.CreateDirectory(item.tempPath);
                    try
                    {
                        //解压到缓存目录
                        //ZipFile.ExtractToDirectory(item.FileName, item.tempPath, Encoding.UTF8);
                        ZipFile.ExtractToDirectory(item.FileName, item.tempPath);
                    }
                    catch (Exception ex)
                    {
                        AppendLine($"解压失败 {item.FileName}: {ex.Message}\n");
                        continue;
                    }

                        Console.WriteLine($"解压 {item.FileName} ------> {item.tempPath}");
                        AppendLine($"解压 {item.FileName} -->\n");
                        AppendLine($"  {item.tempPath}\n");

                        DirectoryInfo di = new DirectoryInfo(item.tempPath);
                        FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
                        

                        for (int i =0;i< item.ExtractPath.Count;i++)
                        {
                            string ext_path = item.ExtractPath[i];
                                
                            Directory.CreateDirectory(ext_path);

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
                                    else { 
                                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newpath));
                                    }
                                    file.CopyTo(newpath);
                                }
                                catch (Exception ex)
                                {
                                    AppendLine($"安装文件失败 {newpath}: {ex.Message}\n");
                                }
                            }
                            
                            string history_file_pack = System.IO.Path.Combine(unistall_dir,item.MaxRoots[i], System.IO.Path.GetFileNameWithoutExtension(item.FileName));
                            history_file_pack = System.IO.Path.ChangeExtension(history_file_pack, ".txt");

                            if (File.Exists(history_file_pack))
                            {
                                File.Delete(history_file_pack);
                            }
                            else {
                                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(history_file_pack));
                            }
                            File.WriteAllLines(history_file_pack, history_file, Encoding.UTF8);
                            AppendLine($"安装记录 {history_file_pack}\n");

                    }


                    /*if (!string.IsNullOrEmpty(item.sha))
                    {
                        uninstallfile = $"{unistall_dir}\\{item.sha}";
                    }*/
                    //File.WriteAllText(uninstallfile, copyfilelist.ToString(), Encoding.UTF8);

                    //}
                    //catch { }




                    if (item.parent != null)
                    {
                        installview.SetInstallFileConfig(item.parent);
                    }
                    
                        //保存当前安装的文件记录
                    installview.SetInstallFileConfig(item);
                    
                }
            }



        }
        public static void Clear(List<DownloadItem> downloadItems)
        {
            foreach (DownloadItem item in downloadItems)
            { 
                Directory.Delete(item.tempPath,true);
                File.Delete(item.FileName);
            }
        }

        
    }
}
