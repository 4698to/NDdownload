using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Shapes;

namespace NDDownload.Download
{
    public class ResourcesUrl
    {

        public static float version = 0.42f;
        public static string buildtime = "2026.07.09";
        public static string Windowtitle = $"天晴盒子安装 - 公共服务器 - QQ群:797581676 | Ver.{version}";
        public static string Windowtitle2 = $"天晴盒子安装 - 内网服务器 - 联系99U:199505 | Ver.{version}";

        public static string TempDownPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NDToolsDownload");

        public static string serverPort = "anim.nd.com.cn";//"192.168.251.138"; //"192.168.251.94";//公司内网IP 
        public static string serverPort2 = "www.sundaybox.cc";


        //public static string serverName1 = "http://sundaybox.cc/downloadfiles?fileid=";//腾讯服务器

        public static string serverName2 = $"http://{serverPort2}/Test_download?fileid=";//腾讯服务器
        public static string serverName1 = $"http://{serverPort}/download?fileid=";//公司内网
        //public static string serverName1 = $"http://{serverPort}:8019/download?fileid=";//公司内网



        //服务器上的版本号文件，每次启动下载该文件检测是否有版本更新
        public static string serverVersion = $"{serverName1}updateBox.txt";

        
        public static string ApplicationPlugins = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\";
        
        public static string Resources = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\Resources";


        //服务器上的内容列表
        public static string contentList = "InstallBox_version_full.json";
        public static string contentLocal = $"{ApplicationPlugins}InstallBox_version_full.json";
        
        public static string dataModelFileXml = @"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\ToolLists\NDToolsList.xml";

        //更新日志
        public static string aboutFile = $"{ApplicationPlugins}About.txt";
        public static string serverAboutFile = $"{serverName1}About.txt";
        
        //本地的配置文件
        public static string iniConfig = $"{ApplicationPlugins}config.ini";

        public static int SeriesMin = 2015;//兼容3dsMax的最小版本号
        public static int SeriesMax = 2025;//兼容3dsMax的最大版本号

        public static string GetServerName(bool type)
        {
            if (type)
            {
                return serverName2;
            }
            return serverName1;
        }
        public static string GetShowGif()
        {
            string randomGifFile = "./image/20171005141703.gif";

            //程序运行路径
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            //string path = System.IO.Path.Combine(ApplicationPlugins, "ShowGIF");
            string path = System.IO.Path.Combine(appPath, "ShowGIF");
            if (Directory.Exists(path))
            {
                // 获取路径下所有 .gif 文件
                string[] gifFiles = Directory.GetFiles(path, "*.gif", SearchOption.TopDirectoryOnly);
                if (gifFiles.Length > 0)
                {
                    // 随机选择一个 .gif 文件
                    Random random = new Random();
                    randomGifFile = gifFiles[random.Next(gifFiles.Length)];
                }
            }
            return randomGifFile;
        }
    }
}
