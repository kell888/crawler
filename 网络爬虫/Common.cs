using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

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
    }
}
