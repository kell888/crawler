using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace 网络爬虫
{
    public static class Logs
    {
        private static string logPath = AppDomain.CurrentDomain.BaseDirectory + "Logs";
        public static void Create(string msg)
        {
            DateTime now = DateTime.Now;
            try
            {
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);
                string path = logPath + "\\" + now.ToString("yyyy-MM-dd-HH-mm-ss") + ".log";
                File.WriteAllText(path, msg + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                if (!Directory.Exists("d:\\Logs"))
                    Directory.CreateDirectory("d:\\Logs");
                string path = "d:\\Logs\\" + now.ToString("yyyy-MM-dd-HH-mm-ss") + ".log";
                File.WriteAllText(path, msg + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
