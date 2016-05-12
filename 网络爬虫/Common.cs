using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Net;

namespace 网络爬虫
{
    public static class Common
    {
        public static bool SaveAppSettingConfig(string key, string value, string configPath = null)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".config";
            if (!string.IsNullOrEmpty(configPath))
                path = configPath;
            if (File.Exists(path))
            {
                try
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(path);
                    XmlNode xNode;
                    XmlElement xElem;
                    xNode = xDoc.SelectSingleNode("//appSettings");
                    xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + key + "']");
                    xElem.SetAttribute("value", value);
                    xDoc.Save(path);
                    return true;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return false;
        }

        public static string GetAppSettingConfig(string key, string configPath = null)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".config";
            if (!string.IsNullOrEmpty(configPath))
                path = configPath;
            if (File.Exists(path))
            {
                try
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(path);
                    XmlNode xNode;
                    XmlElement xElem;
                    xNode = xDoc.SelectSingleNode("//appSettings");
                    if (xNode != null)
                    {
                        xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + key + "']");
                        if (xElem != null)
                        {
                            string s = xElem.GetAttribute("value");
                            return s;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return "";
        }

        public static bool IsValidFileName(string filename)
        {
            if (!string.IsNullOrEmpty(filename) && filename.Length <= 260)
            {
                if (filename.Contains("\r\n"))
                    return false;
                Regex regex = new Regex(@"(?<fpath>([a-zA-Z]:\\){0,1}([\s\.\-\w]+\\)*)(?<fname>[\w]+)(?<namext>(\.[\w]+)*)");
                return regex.IsMatch(filename);
            }
            return false;
        }

        public static string GetEncoding(string url)
        {
            string endoder = "utf8";
            if (!url.StartsWith("://"))
                url = "http://"+ url;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UseDefaultCredentials = true;
            req.Method = "HEAD";
            req.Accept = "text/html";
            req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)";
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            if (res.StatusCode == HttpStatusCode.OK)
            {
                string meta = res.GetResponseHeader("Content-Type");
                Match charSetMatch = Regex.Match(meta, "charset=([^<]*)", RegexOptions.IgnoreCase);
                if (charSetMatch.Success)
                    endoder = charSetMatch.Groups[1].Value.Trim(new char[] { '"', '\'' });
                //Match charSetMatch = Regex.Match(meta, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                //if (charSetMatch.Success)
                //    endoder = charSetMatch.Groups[2].Value;
            }
            res.Close();
            return endoder;
        }

        public static string GetEncoding(string url, out string html)
        {
            if (!url.StartsWith("://"))
                url = "http://" + url;
            var data = new WebClient { }.DownloadData(url); //根据url的网址下载html
            var r_utf8 = new StreamReader(new MemoryStream(data), Encoding.UTF8); //将html放到utf8编码的StreamReader内
            var r_gbk = new StreamReader(new MemoryStream(data), Encoding.Default); //将html放到gbk编码的StreamReader内
            var t_utf8 = r_utf8.ReadToEnd(); //读出html内容
            var t_gbk = r_gbk.ReadToEnd(); //读出html内容
            if (!IsLuan(t_utf8)) //判断utf8是否有乱码
            {
                html = t_utf8;
                return "utf8";
            }
            else
            {
                html = t_gbk;
                return "gbk";
            }
        }

        private static bool IsLuan(string txt)
        {
            {
                var bytes = Encoding.UTF8.GetBytes(txt);
                //239 191 189
                for (var i = 0; i < bytes.Length; i++)
                {
                    if (i < bytes.Length - 3)
                        if (bytes[i] == 239 && bytes[i + 1] == 191 && bytes[i + 2] == 189)
                        {
                            return true;
                        }
                }
                return false;
            }
        }
    }
}