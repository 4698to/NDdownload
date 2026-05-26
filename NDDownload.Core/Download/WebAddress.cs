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

        public static string GetBestServer()
        {
            if (pingServer2())
            {
                return ResourcesUrl.serverName1;
            }
            if (pingServer())
            {
                return ResourcesUrl.serverName2;
            }
            return null;
        }
        /// <param name="useTencentServer">true=腾讯云，false=公司内网</param>
        public static bool GetBestServer(bool useTencentServer)
        {
            if (useTencentServer)
            {
                return pingServer();
            }
            return pingServer2();
        }
        private const int PingTimeoutMs = 2000;

        //检测腾讯服务器是否正常
        public static bool pingServer()
        {
            return TryPingHost(ResourcesUrl.serverPort2);
        }

        public static bool pingServer2()
        {
            return TryPingHost(ResourcesUrl.serverPort);
        }

        private static bool TryPingHost(string host)
        {
            try
            {
                using (Ping pingsender = new Ping())
                {
                    PingReply reply = pingsender.Send(host, PingTimeoutMs);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ping failed ({host}): {ex.Message}");
                return false;
            }
        }

        //检测内网的服务器是否正常
        public static bool pingIp()
        {
            return CheckConnect(ResourcesUrl.serverPort, 8019);
        }

        /// <summary>检查服务器和端口是否可以连接（支持主机名或 IP）</summary>
        public static bool CheckConnect(string hostOrIp, int port)
        {
            TcpClient tcpClient = null;
            try
            {
                if (!TryResolveHostAddress(hostOrIp, out IPAddress ip))
                {
                    return false;
                }

                tcpClient = new TcpClient { SendTimeout = 200 };
                IAsyncResult result = tcpClient.BeginConnect(ip, port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(2000))
                {
                    return false;
                }
                return tcpClient.Connected;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckConnect failed ({hostOrIp}:{port}): {ex.Message}");
                return false;
            }
            finally
            {
                tcpClient?.Close();
                tcpClient?.Dispose();
            }
        }

        private static bool TryResolveHostAddress(string hostOrIp, out IPAddress address)
        {
            address = null;
            try
            {
                if (IPAddress.TryParse(hostOrIp, out address))
                {
                    return true;
                }
                IPAddress[] addresses = Dns.GetHostAddresses(hostOrIp);
                address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                    ?? addresses.FirstOrDefault();
                return address != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DNS resolve failed ({hostOrIp}): {ex.Message}");
                return false;
            }
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
