using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NDDownload.ViewModel;
using System.Collections.ObjectModel;

namespace NDDownload.Download
{
    public class DownloadItem
    {
        //public string Name { get; set; }//列表中显示的名字

        public string FolderPath;//下载资源包的本地路径

        public string FileName//下载资源包的本地路径 + 文件名\
        {
            get {
                if (!Path.HasExtension(zipname))
                {
                    zipname = Path.ChangeExtension(zipname, ".zip");
                }
                return Path.Combine(FolderPath, zipname); }
        }

        public string zipname;// 资源包文件名 
        public string ZipName 
        {
            get {
                if (!Path.HasExtension(zipname))
                {
                    return Path.ChangeExtension(zipname, ".zip");
                }
                return zipname;
            }
            set { zipname = value; }
        }
        public string serverUrl; //服务器地址
        public string Url
        {
            get { return (serverUrl + zipname); }
        }
        //完整的下载链接
        public List<string> MaxRoots { get; set; }//选中的路径的3dsMax版本
        public List<string> MaxVersion { get; set; }//所有适合安装的3dsMax版本


        public List<string> ExtractPath;//最终的资源去处目录，不管是否选中3dsMax版本的目录


        public string tempPath { get; set; } //临时解压的缓存目录

        public int type; //0 MaxRoot ; 1 Application
        public string DirType {
            get {
                if (type == 1) { return "Application"; }
                if (type == 0) { return "MaxRoot"; }
                return "MaxRoot";
            }
            set {
                DirType = value;
            }
        }//安装路径类型 Application - MaxRoot

        public string dirpath
        {
            get { return DirPath; }
            set { DirPath = value; }
        }

        public string DirPath;// scripts ,MAx根目录下的子目录
        public int SeriesMin { get; set; } //兼容3dsMax的最小版本号
        public int SeriesMax { get; set; } //兼容3dsMax的最大版本号
        public bool quick { get; set; }//是否可以在不关闭Max下安装
        public bool isDone { get; set; }//是否下载完成
        public bool selected { get; set; }//是否被选中下载

        public bool IsEnabled { get; set; } //是否可以让用户选择，false 不让用户选择
        public bool hasUrl
        {
            get
            {
                if (helplink != null) { return true; } else { return false; }
            }
        }
        public string helpUrl  //;//帮助链接
        {
            get
            {
                
                return helplink;
                
            }
            set { helplink = value; }
        }
        public string helplink;
        
        public string about="";
        public string abouttext
        {
            get { return about; }
            set { about = value; }
        }
        public string version = "";
        public string oldVersion = "";
        public string sha = "";

        public bool isParent { get; set; }
        public DownloadItem parent;
        public List<DownloadItem> child;


        public DownloadItem()
        {
            this.sha = "";
            
        }
        public List<DownloadItem> TestMaxVersion(ObservableCollection<MaxViewModel> Maxinstall)
        {
            List<DownloadItem> temp_item = new List<DownloadItem>();

            foreach (MaxViewModel max in Maxinstall)
            {
                if (max.isSelected)
                {
                    string max_name = GetInstallItem.GetMaxNameFromPath(max.Path);
                    List<DownloadItem> ims = find_child_items(max_name);
                    foreach (DownloadItem im in ims)
                    {
                        if (!temp_item.Contains(im))
                        {
                            temp_item.Add(im);
                        }
                    }
                }
            }
            return temp_item;
        }

        public List<DownloadItem> find_child_items(string max_name)
        {
            List<DownloadItem> items = new List<DownloadItem>();
            foreach (DownloadItem i in child)
            {
                if (i.SeriesMinMax(GetInstallItem.GetMaxVersionFromPath(max_name)))
                {
                    items.Add(i);
                }
            }
            return items;
        }
        /// <summary>
        /// 设置资源包的临时解压目录
        /// </summary>
        public void SetTempPath()
        {
            FolderPath = Path.Combine(Path.GetTempPath(), "NDToolsDownload");
            this.SetTempGUIDPath();
        }
        public void SetTempGUIDPath()
        { 
            tempPath = Path.Combine(FolderPath, Guid.NewGuid().ToString("N"));
        }
        /// <summary>
        /// 设置该资源包的最终去处路径
        /// </summary>
        /// <param name="install"></param>
        public void SetWillPath(ObservableCollection<MaxViewModel> install, bool detect_select_max_verion)
        {
            if (child != null)
            {
                foreach (DownloadItem item in child)
                {
                    item.SetWillPath(install, detect_select_max_verion);
                }
            }
            if (this.DirType.Equals("MaxRoot"))
            {
                this.ExtractPath = new List<string>();

                this.MaxRoots = new List<string>();
                
                this.MaxVersion = new List<string>();
                foreach (MaxViewModel max in install)
                {
                    //版本名字
                    string max_name = GetInstallItem.GetMaxNameFromPath(max.Path);
                    //检测选中的Max版本是否符合改资源包的版本区间

                    if (this.SeriesMinMax(GetInstallItem.GetMaxVersionFromPath(max_name)))
                    {


                        if (detect_select_max_verion)
                        {
                            if (max.isSelected)
                            {
                                this.MaxVersion.Add(max_name);
                                this.MaxRoots.Add(max_name);
                                //在兼容范围内的
                                this.ExtractPath.Add(Path.Combine(max.Path, DirPath.TrimStart('\\')));
                            }
                            
                        }
                        else
                        {
                            
                            this.MaxVersion.Add(max_name);
                            this.MaxRoots.Add(max_name);
                            //在兼容范围内的
                            this.ExtractPath.Add(Path.Combine(max.Path, DirPath.TrimStart('\\')));
                        }
                    }

                }
                
            }
            if (this.DirType.Equals("Application"))
            {
                if (string.IsNullOrEmpty(this.DirPath))
                {
                    this.ExtractPath = new List<string> {
                        @"C:\ProgramData\Autodesk\ApplicationPlugins"
                    };
                }
                else {
                    this.ExtractPath = new List<string> {
                        System.IO.Path.Combine(@"C:\ProgramData\Autodesk\ApplicationPlugins",this.DirPath)
                    };
                }
                
                this.MaxRoots = ExtractPath;
            }
        }
        
        public bool SeriesMinMax(int version)
        {
            if (SeriesMax == 0)
            {
                if (version == SeriesMin)
                {
                    return true;
                }
            }
            if (version >= this.SeriesMin && version <= this.SeriesMax)
            {
                return true;
            }
            return false;
        }



    }
}
