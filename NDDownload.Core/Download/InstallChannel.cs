using System;
using System.IO;
using System.Reflection;
using NDDownload.ViewModel;

namespace NDDownload.Download
{
    /// <summary>
    /// 区分公网版 (NDDownload.exe) 与内网版 (NDDownloadIn.exe) 的渠道行为。
    /// </summary>
    public static class InstallChannel
    {
        public static string InstallerExeName { get; private set; } = "NDDownload.exe";

        public static string ChannelFolderName { get; private set; } = "NDDownload-公网";

        public static bool AllowServerSelection { get; private set; } = true;

        /// <summary>true = 腾讯云 (serverName2)，false = 公司内网 (serverName1)</summary>
        public static bool DefaultUseTencentServer { get; private set; } = true;

        public static void ConfigurePublic()
        {
            InstallerExeName = "NDDownload.exe";
            ChannelFolderName = "NDDownload-公网";
            AllowServerSelection = false;
            DefaultUseTencentServer = true;
        }

        public static void ConfigureIntranet()
        {
            InstallerExeName = "NDDownloadIn.exe";
            ChannelFolderName = "NDDownloadIn-内网";
            AllowServerSelection = false;
            DefaultUseTencentServer = false;
        }

        /// <summary>根据启动 exe 文件名自动选择渠道（在 App 启动时调用）。</summary>
        public static void ConfigureFromEntryAssembly()
        {
            string exeName = Path.GetFileName(Assembly.GetEntryAssembly()?.Location ?? string.Empty);
            if (string.Equals(exeName, "NDDownloadIn.exe", StringComparison.OrdinalIgnoreCase))
            {
                ConfigureIntranet();
            }
            else
            {
                ConfigurePublic();
            }
        }

        public static string GetInstalledExePath()
        {
            return Path.Combine(ResourcesUrl.ApplicationPlugins, InstallerExeName);
        }

        /// <summary>
        /// NDDownload.exe 固定腾讯云；NDDownloadIn.exe 固定公司内网。
        /// <paramref name="useIntranetServer"/> 仅历史绑定保留，渠道固定时忽略。
        /// </summary>
        public static bool UseTencentServer(bool useIntranetServer = false)
        {
            if (!AllowServerSelection)
            {
                return DefaultUseTencentServer;
            }
            return !useIntranetServer;
        }

        public static void ApplyFixedServerToViewModel(InstallViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }
            viewModel.SelectSource = !DefaultUseTencentServer;
        }
    }
}
