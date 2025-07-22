using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO.Compression;
using System.Net;
using NDDownload.ViewModel;
using System.Collections.ObjectModel;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Security;
using System.Security.Cryptography;

namespace NDDownload.Download
{
    public class PackageContents
    {
        private XmlDocument xml;
        private XmlNode applicationXml;
        private XmlNode NDTOOLSXml;
        private List<string> Maxlist;

    }
    // 解析 InstallBox_version_full.xml
    public static class GetInstallitem
    {
        
       

        /// <summary>
        /// /从Max安装路径 截取 版本,如：2015
        /// </summary>
        /// <param name="_path"></param>
        /// <returns></returns>

        public static int GetMaxVersionFormPath(string _path)
        {
            if (string.IsNullOrEmpty(_path))
            {
                return ResourcesUrl.SeriesMin;
            }
            else
            {
                return int.Parse(_path.Substring(_path.Length - 4, 4));
            }
        }
        /// <summary>
        /// 从3dsMax安装路径中截取 MAX版本 名字 如 ：3ds Max 2015
        /// </summary>
        /// <param name="_path"></param>
        /// <returns></returns>
        public static string GetMaxNameFormPath(string _path)
        {
            string name = System.IO.Path.GetDirectoryName(_path);
            name = System.IO.Path.GetFileName(name);
            //3ds Max 2015 
            return name;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="path"></param>
        /// <param name="body_information"></param>
        /// <returns></returns>
        public static List<DownloadItem> DeserializePack(string path, string remote, ref float ver)
        {
            List<DownloadItem> itmelist = new List<DownloadItem>();
            if (File.Exists(path))
            {
                string jsontext = System.IO.File.ReadAllText(path, new System.Text.UTF8Encoding(false));

                JToken Jview_body = JToken.Parse(jsontext);

                JValue Jver = (JValue)Jview_body["Version"];
                ver = float.Parse(Jver.Value<string>());

                //实际 的资源下载链接
                /*JValue JUrl = (JValue)Jview_body["remoteUrl"];
                if (JUrl != null)
                {
                    string remoteUrl = JUrl.Value<string>();
                    if (!string.IsNullOrWhiteSpace(remoteUrl))
                    {
                        remote = remoteUrl;
                    }
                }*/
                //body_information = Jview_body.ToObject<packInformation>();

                JArray items = (JArray)Jview_body["item"];

                foreach (JToken i in items)
                {
                    DownloadItem ifile = i.ToObject<DownloadItem>();
                    if (ifile != null)
                    {
                        ifile.SetTempPath();
                        ifile.serverUrl = remote;

                        JArray ichild = (JArray)i["child"];
                        if (ichild != null)
                        {
                            ifile.isParent = true;
                            List<DownloadItem> ichild_ = new List<DownloadItem>();
                            foreach (JToken citem in ichild)
                            {
                                DownloadItem ici = citem.ToObject<DownloadItem>();
                                if (ici != null)
                                {
                                    ici.serverUrl = remote;

                                    ici.SetTempPath();
                                    //ici.SetLastPackTime();
                                    //ici.dirpath = ifile.dirpath;
                                    ici.version = ifile.version;

                                    ici.parent = ifile;

                                    ichild_.Add(ici);

                                }
                            }
                            ifile.child = ichild_;
                            //if (is_Ischange > 0) { ifile.ischange = true; } else { ifile.ischange = false; }
                        }
                        else
                        {
                            //ifile.SetLastPackTime();
                        }
                        itmelist.Add(ifile);
                    }
                }
            }
            return itmelist;
        }
        public static List<DownloadItem> ReadXml(string xmlfile, string remote,ref float v)
        {
            List<DownloadItem> downloadItems = new List<DownloadItem>();
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlfile);
            //获取根据节点
            XmlNodeList sitenode = xml.SelectNodes("Root/site");
            string MaxRoot = Path.Combine(Path.GetTempPath(), "NDToolsDownload");
            // 缺少 读取版本

            XmlNode version = xml.SelectSingleNode("Root/version");
            if (version != null)
            {
                //float.TryParse(version.Value,out v);
                v = float.Parse(version.InnerText);
            }
            

            Console.WriteLine(MaxRoot);
            Directory.CreateDirectory(MaxRoot);

            foreach (XmlNode site in sitenode)
            {
                DownloadItem item = new DownloadItem();
                //item.Url = string.Concat(remote, site.Attributes["name"].Value);
                item.serverUrl = remote;

                item.zipname = site.Attributes["name"].Value;
                item.SetTempPath();

                //item.FileName = Path.Combine(MaxRoot, site.Attributes["name"].Value);

                //item.tempPath = Path.Combine(MaxRoot, Guid.NewGuid().ToString("N"));

                //item.MaxRoots = MaxInstallPath;

                XmlNode path = site.Attributes.GetNamedItem("path");
                string pathName = "";
                if (path != null)
                {
                    pathName = path.Value.TrimStart('\\');
                }

                string InstallType = site.Attributes.GetNamedItem("InstallType").Value;
                if (string.IsNullOrEmpty(InstallType))
                {
                    item.DirType = "MaxRoot";
                    item.DirPath = "scripts";
                }
                else {
                    //item.SetApplicationPath(InstallType.Value, pathName);
                    item.DirType = InstallType;
                    item.DirPath = pathName;
                    //item.SetApplicationPath("Application", pathName);
                }
                //设置下载资源的版本范围
                string seriesMin = site.Attributes.GetNamedItem("SeriesMin").Value;
                string seriesMax = site.Attributes.GetNamedItem("SeriesMax").Value;

                if (string.IsNullOrEmpty(seriesMin))
                {
                    item.SeriesMin = ResourcesUrl.SeriesMin;
                    item.SeriesMax = ResourcesUrl.SeriesMax;

                }
                else {
                    item.SeriesMin = int.Parse(seriesMin);
                    item.SeriesMax = int.Parse(seriesMax);
                }

                XmlNode urlnode = site.Attributes.GetNamedItem("help");
                if (urlnode != null)
                {
                    if (string.IsNullOrEmpty(urlnode.Value))
                    {
                        //item.hasUrl = false;
                    }
                    else
                    {
                        //item.hasUrl = true;
                        item.helplink = urlnode.Value;
                    }
                }
                //是否 能让用户选择,true , 不让用户选择
                XmlNode IsEnabled = site.Attributes.GetNamedItem("IsEnabled");
                if (IsEnabled != null && !string.IsNullOrEmpty(IsEnabled.Value))
                {
                    item.IsEnabled = !bool.Parse(IsEnabled.Value);
                    if (item.IsEnabled)
                    {
                        item.selected = true;
                    }
                }
                else {
                    item.IsEnabled = true;
                }
                //SHA256
                XmlNode sha = site.Attributes.GetNamedItem("SHA");
                if (sha != null && !string.IsNullOrEmpty(sha.Value))
                {
                    item.sha = sha.Value;
                }

                XmlNode about = site.Attributes.GetNamedItem("abouttext");
                if (about != null)
                {
                    item.about = about.Value;
                }


                XmlNode ver = site.Attributes.GetNamedItem("Ver");
                if (ver != null)
                {
                    item.version = ver.Value;
                }

                downloadItems.Add(item);

                //site.Attributes.GetNamedItem("path");

            }
            return downloadItems;
        }
       
        //获取系统中安装的MAX路径
        public static List<string> GetMax()
        {
            RegistryKey hkml = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            string[] subkeys;
            List<string> LocationPath = new List<string>();
            using (RegistryKey software = hkml.OpenSubKey("SOFTWARE\\Autodesk\\3dsMax", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
            {
                subkeys = software.GetSubKeyNames();
                foreach (string key in subkeys)
                {
                    RegistryKey Maxkey = software.OpenSubKey(key, RegistryKeyPermissionCheck.ReadSubTree);
                    //Console.WriteLine(Maxkey.ValueCount);
                    var value = Maxkey.GetValue("location", null);
                    //拿到全部 3dsMax 安装路径
                    if (value != null)
                    {
                        LocationPath.Add(value.ToString());
                        //Console.WriteLine(value);
                    }
                    Maxkey.Close();
                }
            }
            return LocationPath;
        }
        
       
        public static bool GetMaxIsInstall(ref Ini config, string name)
        {
            string v = config.GetValue( name,"NDBox", "false");
            return bool.Parse(v);
        }

        public static void ReMoveInstallFileLog(ref Ini config, DownloadItem Items)
        {
            string filename = Path.GetFileName(Items.FileName);
            
           
            config.RemoveKey("Version", filename);
            config.RemoveKey("SHA", filename);

            if (Items.DirType.Equals("MaxRoot"))
            {
                foreach (var i in Items.MaxRoots)
                {
                    // config.WriteValue(i, filename, "0");
                    config.RemoveKey(i, filename);
                }
            }
            if (Items.DirType.Equals("Application"))
            {
                //config.WriteValue("Application", filename, "0");
                config.RemoveKey("Application", filename);
            }


        }

        public static void GetInstallFileVersion(ref Ini config, DownloadItem Items)
        {
            
            Items.oldVersion = config.GetValue("Version", Items.zipname, "0");
        }
        //保存安装文件包记录
        public static void SetInstallFile(ref Ini config, DownloadItem Items)
        {
            if (Items.child == null)
            {

                config.WriteValue("SHA", Items.ZipName, Items.sha);
                config.WriteValue("Version", Items.ZipName, Items.version);

                if (Items.DirType.Equals("MaxRoot"))
                {
                    for (int i = 0; i < Items.MaxRoots.Count; i++)
                    {
                        config.WriteValue(Items.MaxRoots[i], Items.ZipName, "1");
                    }
                }
                if (Items.DirType.Equals("Application"))
                {
                    config.WriteValue("Application", Items.ZipName, "1");
                }
            }
            else { 
                config.WriteValue("Version", Items.ZipName, Items.version);
                //Console.WriteLine($"N={Items.zipname} ,v= {Items.version}");
            }
        }
        //检测资源是不是已经安装过
        public static void GetInstallFile(Ini config, DownloadItemViewModel dItems)
        {
            DownloadItem Items = dItems.Item;
            //string name = Path.GetFileName(Items.FileName);
            ////if (Items.child == null)
            //{
                string sha = config.GetValue("SHA", Items.ZipName, "");
                dItems.oldVersion = config.GetValue("Version", Items.ZipName, "0.0");

                //Console.WriteLine($"{Items.ZipName}-> {dItems.oldVersion} , {dItems.oldVersion}");

                if (Items.sha == null || !Items.sha.Equals(sha))
                {
                    return;
                }
            //}

            if (Items.DirType.Equals("MaxRoot"))
            {
                if (Items.child == null)
                {
                    int MaxCount = 0;
                    foreach (string p in Items.MaxVersion)
                    {
                        string max = config.GetValue(p, Items.ZipName, "");
                        if (max.Equals("1"))
                        {
                            MaxCount += 1;
                        }
                    }
                    if (MaxCount == Items.MaxVersion.Count && MaxCount != 0)
                    {
                        //Items.isDone = true;
                        Items.selected = true;
                    }
                }
                return;
            }
            if (Items.DirType.Equals("Application"))
            {
                string max = config.GetValue("Application", Items.zipname, "");
                if (max.Equals("1"))
                {
                    //Items.isDone = true;
                    Items.selected = true;
                }
            }
           
        }

        //获取已安装盒子的MAX版本
        public static void GetMaxIsInstall(ref Ini config,ref float Ver)
        {
            string v = config.GetValue("Version", "NDBoxDownload", "0.0");
            float.TryParse(v, out Ver);
            //Ver = float.Parse(v);

        }
        //设置已安装盒子的MAX版本

        public static void SetMaxIsInstall(ref Ini config, float Ver)
        {
            config.WriteValue("Version", "NDBoxDownload", Ver.ToString());
        }
        public static float GetInstallVersion()
        {
            string keyString = "SOFTWARE\\NDToolsBox";
            float Version = -1.0f;
            RegistryKey hkml = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using (RegistryKey software = hkml.OpenSubKey(keyString, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
            {
                if (software != null)
                {
                    var v = software.GetValue("version");
                    try
                    {
                        Version = float.Parse((string)v);
                    }
                    catch
                    { 
                        
                    }
                }
            }
            return Version;
        }
        public static void SetInstallVersion(float Ver)
        {
            string keyString = "SOFTWARE\\NDToolsBox";
            RegistryKey hkml = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using (RegistryKey software = hkml.OpenSubKey(keyString, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.WriteKey))
            {
                if (software == null)
                {
                    using (RegistryKey soft = hkml.CreateSubKey(keyString))
                    {
                        soft.SetValue("version", Ver);
                    }
                }
                else {
                    software.SetValue("version", Ver.ToString());
                }
            }
        }

        public static float GetRemoteVersion()
        {
            try
            {
                WebClient client = new WebClient();
                client.Proxy = null;
                string reply = client.DownloadString(ResourcesUrl.serverVersion);
                try
                {
                    return float.Parse(reply);
                }
                catch
                {
                    return 0.0f;
                }
            }
            catch { 
                return 0.0f;
            }
        }
    }

    public static class ResetMaxCUI
    {
        //private static string CUI = "C:\\Users\\Administrator\\AppData\\Local\\Autodesk\\3dsMax\\2015 - 64bit\\ENU\\en-US\\UI";

        public static void Reset()
        { 
            for (int i = 0; i < 4; i++)
            {
                int max = 2015 + i;
                //高版本Max 没有这个配置文件
                SetCUiFile(max);
            }
        }
        public static void SetCUiFile(int max)
        {
            //拿到用户 的 \AppData\Local
            string appdata = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string CUIUs = $"{appdata}\\Autodesk\\3dsMax\\{max} - 64bit\\ENU\\en-US\\UI\\MaxManaged.cuix";
            string CUIZh = $"{appdata}\\Autodesk\\3dsMax\\{max} - 64bit\\CHS\\zh-CN\\UI\\MaxManaged.cuix";
            
            RemoveNDBox(CUIUs);
            RemoveNDBox(CUIZh);
        }
        public static void RemoveNDBox(string CUIfile)
        {
            if (File.Exists(CUIfile))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(CUIfile);
                XmlNodeList Windows = xml.SelectNodes("ADSK_CUI/CUIWindows/Window");
                foreach (XmlNode item in Windows)
                {
                    if (item.Attributes["name"].Value.Contains("天晴盒子"))
                    {
                        item.ParentNode.RemoveChild(item);
                        break;
                    }
                }
                xml.Save(CUIfile);
                Console.WriteLine($"save -> {CUIfile}");
            }
            else { 
                //Console.WriteLine($"No existe -> {CUIfile}");
            }
        }
    }
}
