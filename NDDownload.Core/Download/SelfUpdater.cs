using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace NDDownload.Download
{
    /// <summary>
    /// 安装器自更新：比对 NDDownload_version.txt，下载 NDToolsBox.zip，
    /// 退出后由临时脚本解压覆盖 ApplicationPlugins 并重启当前渠道 exe。
    /// </summary>
    public static class SelfUpdater
    {
        public static bool TryGetRemoteVersion(string remoteUrl, out float remoteVersion)
        {
            remoteVersion = 0f;
            if (string.IsNullOrEmpty(remoteUrl))
            {
                return false;
            }

            string url = string.Concat(remoteUrl, ResourcesUrl.installerVersionFile);
            try
            {
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    client.Proxy = null;
                    string reply = client.DownloadString(url)?.Trim();
                    if (string.IsNullOrEmpty(reply))
                    {
                        return false;
                    }
                    return float.TryParse(reply, NumberStyles.Float, CultureInfo.InvariantCulture, out remoteVersion)
                        || float.TryParse(reply, NumberStyles.Float, CultureInfo.CurrentCulture, out remoteVersion);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelfUpdater.TryGetRemoteVersion failed: {url}: {ex.Message}");
                remoteVersion = 0f;
                return false;
            }
        }

        public static bool NeedsUpdate(string remoteUrl, out float remoteVersion)
        {
            if (!TryGetRemoteVersion(remoteUrl, out remoteVersion))
            {
                return false;
            }
            return remoteVersion > ResourcesUrl.version;
        }

        public static string DownloadZip(string remoteUrl)
        {
            string url = string.Concat(remoteUrl, ResourcesUrl.installerZipName);
            string localZip = Path.Combine(ResourcesUrl.TempDownPath, ResourcesUrl.installerZipName);
            try
            {
                Directory.CreateDirectory(ResourcesUrl.TempDownPath);
                if (File.Exists(localZip))
                {
                    File.Delete(localZip);
                }

                using (WebClient web = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    web.Proxy = null;
                    web.DownloadFile(url, localZip);
                }

                if (File.Exists(localZip) && new FileInfo(localZip).Length > 0)
                {
                    return localZip;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelfUpdater.DownloadZip failed: {url}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 启动覆盖脚本并关闭当前进程。Shutdown 使用 BeginInvoke，避免与 Start() 的同步等待死锁。
        /// </summary>
        public static bool ApplyAndRestart(string zipPath)
        {
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(ResourcesUrl.TempDownPath);

                string targetDir = ResourcesUrl.ApplicationPlugins.TrimEnd('\\', '/');
                string restartExe = InstallChannel.GetInstalledExePath();
                string extractDir = Path.Combine(ResourcesUrl.TempDownPath, "extract_update");
                string scriptPath = Path.Combine(ResourcesUrl.TempDownPath, "apply_update.cmd");
                string logPath = Path.Combine(ResourcesUrl.TempDownPath, "apply_update.log");
                int pid = Process.GetCurrentProcess().Id;

                string script = BuildApplyScript(pid, zipPath, extractDir, targetDir, restartExe, logPath);
                File.WriteAllText(scriptPath, script, Encoding.Default);

                Process.Start(new ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    WorkingDirectory = ResourcesUrl.TempDownPath
                });

                var app = Application.Current;
                if (app != null)
                {
                    app.Dispatcher.BeginInvoke(new Action(() => app.Shutdown()));
                }
                else
                {
                    Environment.Exit(0);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelfUpdater.ApplyAndRestart failed: {ex.Message}");
                return false;
            }
        }

        private static string BuildApplyScript(
            int pid,
            string zipPath,
            string extractDir,
            string targetDir,
            string restartExe,
            string logPath)
        {
            // cmd 脚本：等进程退出 → Expand-Archive → xcopy 覆盖 → 启动 exe → 清理
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal");
            sb.AppendLine($"set \"PID={pid}\"");
            sb.AppendLine($"set \"ZIP={zipPath}\"");
            sb.AppendLine($"set \"EXTRACT={extractDir}\"");
            sb.AppendLine($"set \"TARGET={targetDir}\"");
            sb.AppendLine($"set \"EXE={restartExe}\"");
            sb.AppendLine($"set \"LOG={logPath}\"");
            sb.AppendLine("echo apply_update start %date% %time% > \"%LOG%\"");
            sb.AppendLine(":wait");
            sb.AppendLine("tasklist /FI \"PID eq %PID%\" 2>NUL | find \"%PID%\" >NUL");
            sb.AppendLine("if not errorlevel 1 (");
            sb.AppendLine("  timeout /t 1 /nobreak >NUL");
            sb.AppendLine("  goto wait");
            sb.AppendLine(")");
            sb.AppendLine("if exist \"%EXTRACT%\" rmdir /s /q \"%EXTRACT%\"");
            sb.AppendLine("mkdir \"%EXTRACT%\" >> \"%LOG%\" 2>&1");
            sb.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -LiteralPath '%ZIP%' -DestinationPath '%EXTRACT%' -Force\" >> \"%LOG%\" 2>&1");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  echo Expand-Archive failed >> \"%LOG%\"");
            sb.AppendLine("  goto cleanup");
            sb.AppendLine(")");
            sb.AppendLine("if not exist \"%TARGET%\" mkdir \"%TARGET%\"");
            sb.AppendLine("xcopy /E /Y /I \"%EXTRACT%\\*\" \"%TARGET%\\\" >> \"%LOG%\" 2>&1");
            sb.AppendLine("if exist \"%EXE%\" (");
            sb.AppendLine("  start \"\" \"%EXE%\"");
            sb.AppendLine(") else (");
            sb.AppendLine("  echo restart exe missing: %EXE% >> \"%LOG%\"");
            sb.AppendLine(")");
            sb.AppendLine(":cleanup");
            sb.AppendLine("if exist \"%EXTRACT%\" rmdir /s /q \"%EXTRACT%\"");
            sb.AppendLine("if exist \"%ZIP%\" del /f /q \"%ZIP%\"");
            sb.AppendLine("echo apply_update done %date% %time% >> \"%LOG%\"");
            sb.AppendLine("del \"%~f0\"");
            return sb.ToString();
        }
    }
}
