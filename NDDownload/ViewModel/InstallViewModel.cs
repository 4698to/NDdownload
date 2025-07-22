using NDDownload.Download;
using NDDownload.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Downloader;
using System.Windows.Input;
namespace NDDownload.ViewModel
{
    

    public class body_information
    {
        public float Version;
        
    }
    public class InstallViewModel : INotifyPropertyChanged
    {
        private string RemoteUrl = ResourcesUrl.serverName1;

        private static string iniConfig = ResourcesUrl.iniConfig;
        private Ini config;
        private float _progressValue;
        private string _buttonText;
        private ObservableCollection<MaxViewModel> _maxInstallSelect; //选择安装的 max 版本

        private ObservableCollection<DownloadItemViewModel> _fileDownloadSelect; //选择下载的文件

        private ObservableCollection<ScriptCollectPathsViewModel> _scriptPaths;

        private static string[] _maxPaths;
        public PackageContents package;
        //下载程序
        public static DownloadProgram _fileDownloadProdram;

        //离线安装程序
        public static OfflineInstallation _fileGetProdram;

        private bool _IsDownloadOk;
        private string _message;
        private bool _canControl;
        private string _localVersionMessage;
        private string _remoteVersionMessage;
        private float _remoteVersion;

        public bool _islink; //是否正常链接服务器
        public bool isClose; //完成之后的是不是可以关闭窗口
        public bool quick = false;//是否可以快速安装

        public bool offline = false;//离线模式

        public bool _setectSource;

        public InstallViewModel(List<string> maxpaths) 
        {

            _remoteVersion = 0;
            //配置文件
            config = new Ini(iniConfig);

            _maxPaths = maxpaths.ToArray();



            MaxInstallSelect = new ObservableCollection<MaxViewModel>(
                (from p in maxpaths select new MaxViewModel(p)).ToList()
            );


            _scriptPaths = new ObservableCollection<ScriptCollectPathsViewModel>();
            _scriptPaths.Add(new ScriptCollectPathsViewModel(ResourcesUrl.Resources));

            _xmlPaths = new ObservableCollection<ScriptCollectPathsViewModel>() { new ScriptCollectPathsViewModel(ResourcesUrl.dataModelFileXml) };

            _fileDownloadProdram = new DownloadProgram(_maxPaths, this);
            
            _fileGetProdram = new OfflineInstallation(_maxPaths, this);

            

        }
        public void Start()
        { 
            Task task = new Task(this.CallMethod);
            task.Start();
            task.Wait();

            this.SetlocalVersionMessage();
        }
        public List<DownloadItem> GetDownloadItemList()
        {
            List<DownloadItem> DownloadList = new List<DownloadItem>();
            int index = 0;
            foreach (var i in FileDownloadSelect)
            {
                if (i.isSelected && !i.isDone)
                {
                    if (!i.quick) { index += 1; }
                    if (!i.Item.isParent)
                    {
                        DownloadList.Add(i.Item);
                    }
                    else
                    {
                        //这里直接收集子级，不合适，得做选择版本匹配
                        if (i.Item.child != null)
                        {
                            DownloadList.AddRange(i.Item.TestMaxVersion(MaxInstallSelect));
                        }
                    }
                }
            }
            if (index == 0) { quick = true; } else { quick = false; }

            return DownloadList;
        }
        //离线安装资源
        public void CallOfflineMethod()
        {
            //下载的内容清单
            bool fileDone = false;
            string contentList = Path.Combine(this.UserResourceDirectory, Path.GetFileName(ResourcesUrl.contentList));
            if (File.Exists(contentList))
            {
                fileDone = true;
            }

            string aboutFile = Path.Combine(this.UserResourceDirectory, Path.GetFileName(ResourcesUrl.aboutFile));
            this.open_about(aboutFile);

            if (fileDone)
            {

                FileDownloadSelect = new ObservableCollection<DownloadItemViewModel>();
                //创建用户选择下载内容列表
                List <DownloadItem> Downli = GetDownloadItems(string.Empty, contentList, ref _remoteVersion);
                
                RemoteVers = _remoteVersion;

                foreach (DownloadItem item in Downli)
                {
                    item.FolderPath = this.UserResourceDirectory;
                    item.SetTempGUIDPath();
                    /*if (File.Exists(item.FileName))
                    {
                        FileDownloadSelect.Add(new DownloadItemViewModel(item, MaxInstallSelect));
                    }
                    else {
                        Message += $"未找到 {item.FileName} \n";
                    }*/
                }
                FileDownloadSelect = new ObservableCollection<DownloadItemViewModel>(
                            (from p in Downli select new DownloadItemViewModel(p, MaxInstallSelect)).ToList()
                        );
            }
            else {
                Message += $"未找到 {contentList} \n";
                FileDownloadSelect = new ObservableCollection<DownloadItemViewModel>();
            }
        }
        private async void CallMethod()
        {
            if (this.offline)
            {
                //离线安装资源模式
                this.CallOfflineMethod();
            }
            else
            {
                //设置能否链接服务器
                this._islink = await this.StartWebServer();
                _fileDownloadProdram.RemoteUrl = this.RemoteUrl;

                //Console.WriteLine($"{this.RemoteUrl}");
                if (this._islink)
                {
                    //下载的内容清单
                    bool fileDone = await Task.Run(() => SimpleDownloadListFile(this.RemoteUrl, ResourcesUrl.contentLocal));

                    bool aboutDone = await Task.Run(() => SimpleDownloadListFile(this.RemoteUrl, ResourcesUrl.aboutFile));
                    
                    this.open_about(ResourcesUrl.aboutFile);

                    //清单下载成功 之后 才能继续
                    if (fileDone)
                    {
                        //创建用户选择下载内容列表
                        List<DownloadItem> Downli = GetDownloadItems(this.RemoteUrl, ResourcesUrl.contentLocal, ref _remoteVersion);
                        RemoteVers = _remoteVersion;


                        FileDownloadSelect = new ObservableCollection<DownloadItemViewModel>(
                            (from p in Downli select new DownloadItemViewModel(p, MaxInstallSelect)).ToList()
                        );
                        //检测资源 是不是已经安装过
                        GetInstallConfig();
                    }
                    //Console.WriteLine(_remoteVersion);
                }
            }
        }
        private void open_about(string aboutFile)
        {
            Encoding encod = Encoding.UTF8;
            if (System.IO.File.Exists(aboutFile))
            {
                AboutText = System.IO.File.ReadAllText(aboutFile, encod);
            }
            else
            {
                AboutText = "日志下载失败";
            }

        }
        public void SetFIlePack_Install_Path()
        {

        }
        public async Task<bool> StartWebServer()
        {
            //设置 最合适的服务器
            //this.RemoteUrl = WebAddress.GetBestServer();
            //this.RemoteUrl = await Task.Run(() => WebAddress.GetBestServer());

            this.RemoteUrl = ResourcesUrl.GetServerName(_setectSource);

            bool connect = await Task.Run(() => WebAddress.GetBestServer(_setectSource));

            //if (string.IsNullOrEmpty(this.RemoteUrl))
            if (!connect)
            {
                ButtonText = "无法连接 服务器 "; CanControl = false;

                FileDownloadSelect = new ObservableCollection<DownloadItemViewModel>();

                return false;
            }
            else {
                ButtonText = "安装"; CanControl = true;
                return true;
            }

        }
        //关闭时保存配置文件
        public void Save()
        {
            SetScriptPath();
            SetToolsList();

            config.Save();
        }
        //保存安装的内部包记录
        public void SetInstallFileConfig(DownloadItem downloaditem)
        {
            GetInstallitem.SetInstallFile(ref config, downloaditem);
        }

        public void GetToolsList()
        {
            int count = int.Parse(config.GetValue("Count", "ToolsListWaiting", "0"));
            for (int i = 0; i < count; i++)
            {
                string path = config.GetValue(i.ToString(), "ToolsListWaiting");
                bool is_select = bool.Parse(config.GetValue(i.ToString(), "ToolsListWaitingSelect", "False"));
                if (!string.IsNullOrEmpty(path) )
                {
                    XMLPaths.Add(new ScriptCollectPathsViewModel(path, is_select));
                }
            }
        }
        public void SetToolsList()
        {
            if (XMLPaths.Count > 1)
            {
                List<string> paths = new List<string>();
                config.WriteValue("Count", "ToolsListWaiting", (XMLPaths.Count - 1).ToString());
                for (int i = 1; i < XMLPaths.Count; i++)
                {
                    if (XMLPaths[i].Select)
                    {
                        paths.Add(XMLPaths[i].Path);
                    }
                    config.WriteValue((i - 1).ToString(), "ToolsListWaiting", XMLPaths[i].Path);
                    config.WriteValue((i - 1).ToString(), "ToolsListWaitingSelect", XMLPaths[i].Select.ToString());
                }

                config.WriteValue("Count", "ToolsList", paths.Count.ToString());
                for (int i = 0; i < paths.Count; i++)
                {
                    config.WriteValue(i.ToString(), "ToolsList", paths[i]);
                }
                if (paths.Count == 0)
                {
                    config.RemoveSection("ToolsList");
                }
            }
            
        }
        public async void GetScriptPath()
        {
            int count = int.Parse(config.GetValue("Count", "ResourcesWaiting", "0"));
            for (int i = 0; i < count; i++)
            {
                string path = config.GetValue(i.ToString(), "ResourcesWaiting");
                bool is_select = bool.Parse(config.GetValue(i.ToString(), "ResourcesWaitingSelect","False"));
                if (!string.IsNullOrEmpty(path))
                {
                    ScriptPaths.Add(new ScriptCollectPathsViewModel(path, is_select));
                }
            }

        }
        public void SetScriptPath()
        {
            int count = ScriptPaths.Count - 1;
            if (count > 0)
            {
                List<string> paths = new List<string>();
                config.WriteValue("Count", "ResourcesWaiting", count.ToString());
                for (int i = 1; i < ScriptPaths.Count; i++)
                {
                    if (ScriptPaths[i].Select)
                    {
                        paths.Add(ScriptPaths[i].Path);
                    }
                    config.WriteValue((i - 1).ToString(), "ResourcesWaiting", ScriptPaths[i].Path);
                    config.WriteValue((i - 1).ToString(), "ResourcesWaitingSelect", ScriptPaths[i].Select.ToString());
                }

                config.WriteValue("Count", "Resources", paths.Count.ToString());
                for (int i = 0; i < paths.Count; i++)
                {
                    config.WriteValue(i.ToString(), "Resources", paths[i]);
                }
                if (paths.Count == 0)
                { 
                    config.RemoveSection("Resources");
                }

                paths = null;
            }
            else {
                config.RemoveSection("ResourcesWaiting");
                config.RemoveSection("ResourcesWaitingSelect");
                config.RemoveSection("Resources");
            }
        }
        

        public void GetInstallConfig()
        {
            for (int i = 0; i < FileDownloadSelect.Count; i++)
            {
                //GetInstallitem.GetInstallFile(config, FileDownloadSelect[i].Item);
                GetInstallitem.GetInstallFile(config, FileDownloadSelect[i]);

            }
        }
        //启动时 显示的安装情况
        public void SetlocalVersionMessage()
        {
            float localv = 0;
            GetInstallitem.GetMaxIsInstall(ref config, ref localv);

            if (localv <= 0)
            {
                this.localVersionMessage = "好像是第一次使用天晴盒子安装呢.";
            }
            else {
                this.localVersionMessage = $"上次安装内容版本 : {localv}";
            }
            //float remotev = GetInstallitem.GetRemoteVersion();
            //_remoteVersion = remotev;

            if (_remoteVersion > 0)
            {
                if (localv < _remoteVersion)
                {
                    this.remoteVersionMessage = $"已发布 {_remoteVersion} 版本 ，请安装更新.";
                    if (localv < 0)
                    {
                        this.ButtonText = "安装";
                    }
                    else
                    {
                        this.ButtonText = "更新";
                    }
                }
                if (localv == _remoteVersion)
                {
                    this.remoteVersionMessage = "";

                    this.ButtonText = "更改";
                }
            }
        }
        public void SetlocalVersion()
        {
            //配置文件在C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox 
            //没在MAX安装路径下，不是很准
            GetInstallitem.SetMaxIsInstall(ref config, _remoteVersion);
        }
        public static bool SimpleDownloadListFile(string weburl, string localfilename)
        {

            string url = string.Concat(weburl, Path.GetFileName(localfilename));
            //先创建本地的文件夹，
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(localfilename));

            Console.WriteLine($"{url} -> {localfilename}");

            using (WebClient web = new WebClient())
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3 | (SecurityProtocolType)0x300 | (SecurityProtocolType)0xC00;
                web.Proxy = null;
                web.DownloadFile(url, localfilename);
            }
            if (File.Exists(localfilename))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public List<DownloadItem> GetDownloadItems(string weburl,string xmlfile, ref float _remoteVersion)
        {
            //List<DownloadItem> downloadList = GetInstallitem.ReadXml(WebAddress.contentLocal,  weburl,ref _remoteVersion);
            //string jsonfile = @"G:\Git_NDBox\天晴动作组脚本工具v4.47For2015\20240112\InstallBox_version_full.json";

            List<DownloadItem> downloadList = GetInstallitem.DeserializePack(xmlfile, weburl, ref _remoteVersion);

            this.GetFileItemOldVersion(downloadList);
            return downloadList;
        }
        private string _showgif;
        public string ShowGif
        {
            get { 
                return _showgif;
            }
            set { 
                _showgif = value;
            }
        }
        private string _downtitle;
        public string DownTitle
        {
            get {
                if (this.offline)
                {
                    return "安装 - 离线资源";
                }
                else
                {
                    return "安装 - 线上资源";
                }
            }
            set {
                _downtitle = value;
                this.OnPropertyChanged("DownTitle");

            }
        }
        private string _aboutText;
        public string AboutText
        {
            get { return _aboutText; }
            set {
                _aboutText = value;
                this.OnPropertyChanged("AboutText");
            }

        }
        private string _UserResourceDirectory;
        public string UserResourceDirectory { 
            get { return _UserResourceDirectory; } 
            set { _UserResourceDirectory = value; 
                this.OnPropertyChanged("UserResourceDirectory");
            }
        }
        public bool SetectSource
        {
            get { return _setectSource; }
            set { _setectSource = value;
                this.OnPropertyChanged("SetectSource");
            }
        }
        public bool SetectSourceTencent
        {
            get { return !_setectSource; }
            set
            {
                _setectSource = !value;
                this.OnPropertyChanged("SetectSourceTencent");
            }
        }
        public string localVersionMessage
        {
            get { return _localVersionMessage; }
            set { _localVersionMessage = value;
                this.OnPropertyChanged("localVersionMessage");
            }
        }
        public string remoteVersionMessage
        {
            get { return _remoteVersionMessage; }
            set {
                _remoteVersionMessage = value;
                this.OnPropertyChanged("remoteVersionMessage");
            }
        }
        private async void Down()
        {
            if (!string.IsNullOrEmpty(RemoteUrl))
            {
                this.Message += string.Concat(RemoteUrl, "\n");
                //_fileDownloadProdram.Start();
                await _fileDownloadProdram.DownloadMain();
            }
        }
        public void OfflineMain()
        {
            _fileGetProdram.DownloadMain();
        }
        public void StartDownload()
        {
            if (!isClose)
            {
                if (this.offline)
                {
                    //离线模式，不下载
                    //_fileGetProdram.DownloadMain();
                }
                else
                {
                    //开始下载
                    this.Down();
                }
                
            }
            else {
                this.Close();
            }
        }
        public void Close()
        {
            Save();
            //安装完成之后的退出
            System.Environment.Exit(0);
        }
        public void GetFileItemOldVersion(List<DownloadItem> downloadList)
        {
            foreach (DownloadItem Item in downloadList)
            {
                GetInstallitem.GetInstallFileVersion(ref config, Item);
            }
        }
        public void UnistallZip()
        {
            string unistall_dir = $"{ResourcesUrl.ApplicationPlugins}Unistall";

            List<DownloadItem> downloadItems = GetDownloadItemList();
            foreach (DownloadItem item in downloadItems)
            {
                item.SetWillPath(MaxInstallSelect, true);

                for (int i = 0; i < item.MaxRoots.Count; i++)
                {
                    string history_file_pack = System.IO.Path.Combine(unistall_dir, item.MaxRoots[i], System.IO.Path.GetFileNameWithoutExtension(item.FileName));
                    history_file_pack = System.IO.Path.ChangeExtension(history_file_pack, ".txt");

                    if (File.Exists(history_file_pack))
                    {
                        string[] filelist = File.ReadAllLines(history_file_pack, Encoding.UTF8);

                        foreach (string file in filelist)
                        {
                            if (File.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }
                        filelist = null;
                        try { File.Delete(history_file_pack); } catch { }
                    }
                }
                GetInstallitem.ReMoveInstallFileLog(ref config, item);
            }
            downloadItems = null;

            this.ShowMSG("卸载完成", "OK");
        }


        //卸载
        public void Uninstall()
        {
            //this.ShowMSG("卸载功能还没写", "忍着");
            this.UnistallZip();

        }
        public void ShowMSG(string _msg, string _title)
        {
            MessageBox.Show(
                    _msg,
                    _title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
            );
        }
        public string[] MaxPaths
        {
            get { return _maxPaths; }
            set { _maxPaths = value; }
        }
        public float RemoteVers
        {
            get { return _remoteVersion; }
            set {
                _remoteVersion = value;
                this.OnPropertyChanged("RemoteVers");

            }
        }
        public bool IsDownloadOk
        {
            get { return _IsDownloadOk; }
            set { _IsDownloadOk = value;
                this.OnPropertyChanged("IsDownloadOk");
            }
        }
        public string ButtonText
        {
            get { return _buttonText; }
            set { _buttonText = value;
                this.OnPropertyChanged("ButtonText");
            }

        }
        public bool CanControl
        {
            get { return _canControl; }
            set { _canControl = value;
                this.OnPropertyChanged("CanControl");
            }
        }
        public float ProgressValue
        {
            get { return _progressValue; }
            set {
                _progressValue = value;
                this.OnPropertyChanged("ProgressValue");
            }
        }
        public string WindowTitle
        {
            get
            {
                if (_setectSource)
                {
                    return ResourcesUrl.Windowtitle;
                }
                else {
                    return ResourcesUrl.Windowtitle2;
                }
            }
            set {
                //ResourcesUrl.Windowtitle = value;
                this.OnPropertyChanged("Title");
            }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value;
                this.OnPropertyChanged("Message");
            }
        }
        public int MaxInstallSelectCount
        {
            get {
                int count = 0;
                foreach (var m in MaxInstallSelect)
                {
                    if (m.isSelected) {
                        count += 1;
                    }
                }
                return count;
            }
        }
        public void RemoveSelectInstall()
        {
            //this.package.RemoveMaxList(MaxInstallSelect);
        }
        public bool AddNewXMLToolsList(string path)
        {
            bool isfind = _xmlPaths.Any<ScriptCollectPathsViewModel>(p => p.Path.Equals(path));
            {
                _xmlPaths.Add(new ScriptCollectPathsViewModel(path, true));
            }
            return isfind;
        }
        public bool AddNewScriptPath(string path)
        {
            bool isfind = _scriptPaths.Any<ScriptCollectPathsViewModel>(p => p.Path.Equals(path));
            if (!isfind)
            {
                _scriptPaths.Add(new ScriptCollectPathsViewModel(path,true));
            }
            return isfind;
        } 
        public ObservableCollection<ScriptCollectPathsViewModel> ScriptPaths
        {
            get { return _scriptPaths; }
            set {
                _scriptPaths = value;
                this.OnPropertyChanged("ScriptPaths");
            }
        }
        public ObservableCollection<ScriptCollectPathsViewModel> _xmlPaths;
        public ObservableCollection<ScriptCollectPathsViewModel> XMLPaths
        {
            get { return _xmlPaths; }
            set {
                _xmlPaths = value;
                this.OnPropertyChanged("XMLPaths");

            }
        }
        public ObservableCollection<MaxViewModel> MaxInstallSelect
        {
            get { return _maxInstallSelect; }
            set { 
                _maxInstallSelect = value;
                this.OnPropertyChanged("MaxInstallSelect");
            }
        }
        public void SelectAllMaxViewModel(bool IsChecked)
        {
            foreach (MaxViewModel m in MaxInstallSelect)
            { 
                m.isSelected = IsChecked;
            }
        }

        public void SelectAllFile(bool IsChecked)
        {
            foreach (DownloadItemViewModel m in FileDownloadSelect)
            {
                m.isSelected = IsChecked;
            }
        }
        public ObservableCollection<DownloadItemViewModel> FileDownloadSelect
        {
            get { return _fileDownloadSelect; }
            set
            {
                _fileDownloadSelect = value;
                this.OnPropertyChanged("FileDownloadSelect");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine("InstallViewModel PropertyChanged is null");
        }
    }
}
