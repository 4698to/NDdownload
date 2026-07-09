using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;

namespace NDDownload.Download
{
    public static class WebAddress
    {

       
        public static bool GetBestServer(string type_remote)
        {
            Uri uri = new Uri(type_remote);
            return pingServer(uri.Host);
        }
        //检测腾讯服务器是否正常
        public static bool pingServer(string host_port)
        {
            using (Ping pingsender = new Ping())
            {
                PingReply reply = pingsender.Send(host_port, 50);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            return false;
        }
        /*public static bool pingServer2()
        {
            using (Ping pingsender = new Ping())
            {
                PingReply reply = pingsender.Send(ResourcesUrl.serverPort, 50);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            return false;
        }*/
        //检测内网的服务器是否正常
        public static bool pingIp()
        {
            return WebAddress.CheckConnect(ResourcesUrl.serverPort, 8019);
        }
        
        /// 检查服务器和端口是否可以连接
        /// </summary>
        /// <param name="ipString">服务器ip</param>
        /// <param name="port">端口</param>
        /// <returns></returns>
        public static bool CheckConnect(string ipString, int port)
        {
            bool right = false;
            System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient()
            { SendTimeout = 200 };
            IPAddress ip = IPAddress.Parse(ipString);
            try
            {
                var result = tcpClient.BeginConnect(ip, port, null, null);
                var back = result.AsyncWaitHandle.WaitOne(200);
                right = tcpClient.Connected;
            }
            catch
            {
                //LogHelpter.AddLog($"连接服务{ipString}:{port}失败，设置的超时时间{tcpClient.SendTimeout}毫秒");
                //连接失败
                return false;
            }
            tcpClient.Close();
            tcpClient.Dispose();
            return right;
        }

        public static bool checkMaxProcess(ref string names)
        {
            Process[] process_ = Process.GetProcesses();
            int index = 0;
            foreach (Process _process in process_)
            {
                if (_process.ProcessName.Contains("3dsmax"))
                {
                    names += string.Concat(_process.ProcessName,"\n");
                    index += 1;
                }
            }
            
            if (index > 1)
            {
                return true;
            }

            return false;
        }

        

    }
}
