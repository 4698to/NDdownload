using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDownload.Download
{
    public class ExplorerHelper
    {
        public static void OpenFolder(string folderPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = folderPath,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }
    }
}
