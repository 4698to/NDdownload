using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NDDownload.Download
{
    /// <summary>
    /// 安装器自更新：比对 NDDownload_version.txt，下载 NDToolsBox.zip，
    /// 退出后由临时脚本解压覆盖 ApplicationPlugins 并重启当前渠道 exe。
    /// </summary>
    public static class SelfUpdater
    {
        private static bool _declinedThisSession;

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
                    string reply = client.DownloadString(url);
                    return TryParseVersion(reply, out remoteVersion);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SelfUpdater.TryGetRemoteVersion failed: {url}: {ex.Message}");
                remoteVersion = 0f;
                return false;
            }
        }

        internal static bool TryParseVersion(string text, out float remoteVersion)
        {
            remoteVersion = 0f;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string reply = text.Trim().Trim('\uFEFF', '\u200B');
            if (reply.EndsWith("f", StringComparison.OrdinalIgnoreCase) || reply.EndsWith("F"))
            {
                reply = reply.Substring(0, reply.Length - 1).Trim();
            }

            return float.TryParse(reply, NumberStyles.Float, CultureInfo.InvariantCulture, out remoteVersion)
                || float.TryParse(reply, NumberStyles.Float, CultureInfo.CurrentCulture, out remoteVersion);
        }

        public static bool NeedsUpdate(string remoteUrl, out float remoteVersion)
        {
            if (!TryGetRemoteVersion(remoteUrl, out remoteVersion))
            {
                return false;
            }
            return remoteVersion > ResourcesUrl.version;
        }

        /// <summary>
        /// 在 UI 线程调用：弹确认框、显示进度窗口、下载并应用更新。
        /// 若已启动更新并即将退出，返回 true；否则返回 false（继续正常启动）。
        /// </summary>
        public static async Task<bool> RunInteractiveAsync(string remoteUrl)
        {
            if (_declinedThisSession || string.IsNullOrEmpty(remoteUrl))
            {
                return false;
            }

            float remoteVersion = 0f;
            bool needs = false;
            try
            {
                var check = await Task.Run(() =>
                {
                    float v;
                    bool n = NeedsUpdate(remoteUrl, out v);
                    return new Tuple<bool, float>(n, v);
                }).ConfigureAwait(true);
                needs = check.Item1;
                remoteVersion = check.Item2;
            }
            catch
            {
                return false;
            }

            if (!needs || remoteVersion <= ResourcesUrl.version)
            {
                return false;
            }

            var accept = MessageBox.Show(
                Application.Current?.MainWindow,
                $"发现安装器新版本 {remoteVersion}（当前 {ResourcesUrl.version}）。\n是否立即下载并更新？\n\n更新将覆盖整个安装目录并重启程序。",
                "安装器更新",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!accept)
            {
                _declinedThisSession = true;
                return false;
            }

            Window main = Application.Current?.MainWindow;
            bool? mainWasEnabled = null;
            if (main != null)
            {
                mainWasEnabled = main.IsEnabled;
                main.IsEnabled = false;
            }

            var progressWin = new SelfUpdateWindow();
            if (main != null)
            {
                progressWin.Owner = main;
            }
            progressWin.SetStatus($"正在下载安装包 {ResourcesUrl.installerZipName} …");
            progressWin.SetProgress(0);
            progressWin.Show();
            progressWin.Activate();

            string zipPath = null;
            try
            {
                zipPath = await DownloadZipAsync(remoteUrl, (percent) =>
                {
                    progressWin.SetStatus($"正在下载安装包… {percent:0}%");
                    progressWin.SetProgress(percent);
                }).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                progressWin.Close();
                RestoreMainWindow(main, mainWasEnabled);
                MessageBox.Show(main, $"下载失败：{ex.Message}\n将继续使用当前版本。", "安装器更新", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(zipPath) || !IsValidZip(zipPath))
            {
                progressWin.Close();
                RestoreMainWindow(main, mainWasEnabled);
                MessageBox.Show(main, "安装器更新包下载失败或文件无效，将继续使用当前版本。", "安装器更新", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            progressWin.SetStatus("下载完成，正在安装并重启…\n请稍候，不要关闭窗口。");
            progressWin.SetProgress(100);

            if (!ApplyAndRestart(zipPath))
            {
                progressWin.Close();
                RestoreMainWindow(main, mainWasEnabled);
                MessageBox.Show(main, "无法启动更新脚本，将继续使用当前版本。", "安装器更新", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // ApplyAndRestart 内会 Environment.Exit
            return true;
        }

        private static void RestoreMainWindow(Window main, bool? wasEnabled)
        {
            if (main == null || wasEnabled == null)
            {
                return;
            }
            try
            {
                main.IsEnabled = wasEnabled.Value;
            }
            catch
            {
                // ignore
            }
        }

        private static bool IsValidZip(string path)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    return archive.Entries.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static Task<string> DownloadZipAsync(string remoteUrl, Action<double> onProgress = null)
        {
            string url = string.Concat(remoteUrl, ResourcesUrl.installerZipName);
            string localZip = Path.Combine(ResourcesUrl.TempDownPath, ResourcesUrl.installerZipName);
            var tcs = new TaskCompletionSource<string>();

            try
            {
                Directory.CreateDirectory(ResourcesUrl.TempDownPath);
                if (File.Exists(localZip))
                {
                    File.Delete(localZip);
                }

                var web = new WebClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                web.Proxy = null;

                web.DownloadProgressChanged += (s, e) =>
                {
                    try
                    {
                        onProgress?.Invoke(e.ProgressPercentage);
                    }
                    catch
                    {
                        // ignore UI callback errors
                    }
                };
                web.DownloadFileCompleted += (s, e) =>
                {
                    try
                    {
                        web.Dispose();
                        if (e.Cancelled)
                        {
                            tcs.TrySetResult(null);
                            return;
                        }
                        if (e.Error != null)
                        {
                            tcs.TrySetException(e.Error);
                            return;
                        }
                        if (File.Exists(localZip) && new FileInfo(localZip).Length > 0)
                        {
                            tcs.TrySetResult(localZip);
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                };
                web.DownloadFileAsync(new Uri(url), localZip);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// 启动覆盖脚本并强制退出当前进程（避免 UI 阻塞导致 Shutdown 无效、反复弹窗）。
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
                // 使用系统默认编码，避免中文路径在 cmd 中乱码
                File.WriteAllText(scriptPath, script, Encoding.Default);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"" + scriptPath + "\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = ResourcesUrl.TempDownPath
                });

                // 给 cmd 一点时间真正拉起，再退出本进程
                Thread.Sleep(500);
                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SelfUpdater.ApplyAndRestart failed: {ex.Message}");
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
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal EnableExtensions EnableDelayedExpansion");
            sb.AppendLine("title NDToolsBox Installer Update");
            sb.AppendLine($"set \"PID={pid}\"");
            sb.AppendLine($"set \"ZIP={zipPath}\"");
            sb.AppendLine($"set \"EXTRACT={extractDir}\"");
            sb.AppendLine($"set \"TARGET={targetDir}\"");
            sb.AppendLine($"set \"EXE={restartExe}\"");
            sb.AppendLine($"set \"LOG={logPath}\"");
            sb.AppendLine("set \"WAITCOUNT=0\"");
            sb.AppendLine("echo ========================================");
            sb.AppendLine("echo  天晴盒子安装器更新");
            sb.AppendLine("echo ========================================");
            sb.AppendLine("echo apply_update start %date% %time% > \"%LOG%\"");
            sb.AppendLine("echo [1/4] 等待原进程退出 (PID %PID%) ...");
            sb.AppendLine(":wait");
            sb.AppendLine("set /a WAITCOUNT+=1");
            sb.AppendLine("if !WAITCOUNT! GTR 120 (");
            sb.AppendLine("  echo wait timeout >> \"%LOG%\"");
            sb.AppendLine("  echo 等待进程退出超时，继续尝试安装...");
            sb.AppendLine("  goto extract");
            sb.AppendLine(")");
            sb.AppendLine("tasklist /FI \"PID eq %PID%\" 2>NUL | find \"%PID%\" >NUL");
            sb.AppendLine("if not errorlevel 1 (");
            sb.AppendLine("  timeout /t 1 /nobreak >NUL");
            sb.AppendLine("  goto wait");
            sb.AppendLine(")");
            sb.AppendLine(":extract");
            sb.AppendLine("echo [2/4] 解压更新包 ...");
            sb.AppendLine("if exist \"%EXTRACT%\" rmdir /s /q \"%EXTRACT%\"");
            sb.AppendLine("mkdir \"%EXTRACT%\" >> \"%LOG%\" 2>&1");
            sb.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -LiteralPath '%ZIP%' -DestinationPath '%EXTRACT%' -Force\" >> \"%LOG%\" 2>&1");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  echo Expand-Archive failed >> \"%LOG%\"");
            sb.AppendLine("  echo 解压失败，详见日志: %LOG%");
            sb.AppendLine("  pause");
            sb.AppendLine("  goto cleanup");
            sb.AppendLine(")");
            sb.AppendLine("dir /b \"%EXTRACT%\" >nul 2>&1");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  echo extract empty >> \"%LOG%\"");
            sb.AppendLine("  echo 解压结果为空");
            sb.AppendLine("  pause");
            sb.AppendLine("  goto cleanup");
            sb.AppendLine(")");
            // zip 根目录若没有 exe、只有单层文件夹，则用该层作为复制源
            sb.AppendLine("set \"SRC=%EXTRACT%\"");
            sb.AppendLine("dir /b \"%EXTRACT%\\*.exe\" >nul 2>&1");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  for /d %%D in (\"%EXTRACT%\\*\") do (");
            sb.AppendLine("    set \"SRC=%%~fD\"");
            sb.AppendLine("    goto havesrc");
            sb.AppendLine("  )");
            sb.AppendLine(")");
            sb.AppendLine(":havesrc");
            sb.AppendLine("echo [3/4] 覆盖安装目录 ...");
            sb.AppendLine("echo SRC=!SRC! >> \"%LOG%\"");
            sb.AppendLine("if not exist \"%TARGET%\" mkdir \"%TARGET%\"");
            sb.AppendLine("if not exist \"!SRC!\" (");
            sb.AppendLine("  echo source missing >> \"%LOG%\"");
            sb.AppendLine("  echo 找不到解压目录");
            sb.AppendLine("  pause");
            sb.AppendLine("  goto cleanup");
            sb.AppendLine(")");
            sb.AppendLine("xcopy /E /Y /I \"!SRC!\\*\" \"%TARGET%\\\" >> \"%LOG%\" 2>&1");
            sb.AppendLine("if not exist \"%EXE%\" (");
            sb.AppendLine("  echo restart exe missing: %EXE% >> \"%LOG%\"");
            sb.AppendLine("  echo 更新后未找到: %EXE%");
            sb.AppendLine("  echo 请确认 zip 内包含安装器文件，且已上传匹配的 NDDownload_version.txt");
            sb.AppendLine("  pause");
            sb.AppendLine("  goto cleanup");
            sb.AppendLine(")");
            sb.AppendLine("echo [4/4] 重新启动 ...");
            sb.AppendLine("start \"\" \"%EXE%\"");
            sb.AppendLine("echo 更新完成");
            sb.AppendLine(":cleanup");
            sb.AppendLine("if exist \"%EXTRACT%\" rmdir /s /q \"%EXTRACT%\"");
            sb.AppendLine("if exist \"%ZIP%\" del /f /q \"%ZIP%\"");
            sb.AppendLine("echo apply_update done %date% %time% >> \"%LOG%\"");
            sb.AppendLine("timeout /t 2 /nobreak >NUL");
            sb.AppendLine("del \"%~f0\" >nul 2>&1");
            return sb.ToString();
        }
    }
}
