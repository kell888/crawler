using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace 网络爬虫
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public const string PAGEINDEX = "{PAGEINDEX}";        
        Dictionary<string, int> unload = new Dictionary<string, int>();
        Dictionary<string, int> loaded = new Dictionary<string, int>();
        string baseUrl;
        volatile int download = -1;//下载尚未开始
        ManualResetEvent signal = new ManualResetEvent(false);

        private void Form1_Load(object sender, EventArgs e)
        {
            numericUpDown3.Value = Convert.ToInt32(Common.GetAppSettingConfig("depth"));
            textBox1.Text = baseUrl = Common.GetAppSettingConfig("baseUrl");
            checkBox1.Checked = Common.GetAppSettingConfig("list") == "1";
            textBox10.Text = Common.GetAppSettingConfig("listVar");
            numericUpDown1.Value = Convert.ToInt32(Common.GetAppSettingConfig("listIndexFrom"));
            numericUpDown2.Value = Convert.ToInt32(Common.GetAppSettingConfig("listIndexTo"));
            textBox2.Text = Common.GetAppSettingConfig("picDir");
            textBox3.Text = Common.GetAppSettingConfig("mailFile");
            textBox4.Text = Common.GetAppSettingConfig("linkDir");
            textBox5.Text = Common.GetAppSettingConfig("scriptDir");
            textBox6.Text = Common.GetAppSettingConfig("picReg").Replace("#", "\"").Replace("@", "&").Replace(",", "<");
            textBox7.Text = Common.GetAppSettingConfig("mailReg");
            textBox8.Text = Common.GetAppSettingConfig("linkReg").Replace("#", "\"").Replace("@", "&").Replace(",", "<");
            textBox9.Text = Common.GetAppSettingConfig("scriptReg").Replace("#", "\"").Replace("@", "&").Replace(",", "<");
            string encoder = Common.GetAppSettingConfig("encoding");
            if (!string.IsNullOrEmpty(encoder))
                comboBox1.Text = encoder;
            else
                comboBox1.SelectedIndex = 0;
        }
        Thread thr;
        private void button1_Click(object sender, EventArgs e)
        {
            if (download == -1)
            {
                ShowMsg("开始爬取");
                AddItem("开始爬取");
                download = 1;//爬取进行中
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(delegate
                    {
                        button1.Enabled = false;
                        button2.Enabled = button3.Enabled = true;
                        baseUrl = textBox1.Text.Trim();
                        button13.Enabled = button14.Enabled = false;
                    }));
                }
                else
                {
                    button1.Enabled = false;
                    button2.Enabled = button3.Enabled = true;
                    baseUrl = textBox1.Text.Trim();
                    button13.Enabled = button14.Enabled = false;
                }
                if (!baseUrl.EndsWith("/"))
                    baseUrl += "/";
                if (this.UserList)
                {
                    List<string> baseUrls = new List<string>();
                    string listVar = ListVar;
                    if (listVar != "" && listVar.Contains(PAGEINDEX))
                    {
                        int start = (int)numericUpDown1.Value;
                        int end = (int)numericUpDown2.Value;
                        if (start > end)
                        {
                            int tmp = start;
                            start = end;
                            end = tmp;
                        }
                        for (int i = start; i <= end; i++)
                        {
                            baseUrls.Add(baseUrl + listVar.Replace(PAGEINDEX, i.ToString()));
                        }
                    }
                    else
                    {
                        MessageBox.Show("请填写合法的列表变量！" + Environment.NewLine + "类似【page-{PAGEINDEX}】的格式");
                    }
                    foreach (string URL in baseUrls)
                    {
                        string u = URL;
                        if (!u.Contains("://"))
                            u = "http://" + u;
                        if (!unload.ContainsKey(u))
                        {
                            unload.Add(u, 0);
                            RefreshUnLoadList();
                        }
                    }
                }
                else
                {
                    if (!baseUrl.Contains("://"))
                        baseUrl = "http://" + baseUrl;
                    if (!unload.ContainsKey(baseUrl))
                    {
                        unload.Add(baseUrl, 0);
                        RefreshUnLoadList();
                    }
                }
                thr = new Thread(new ThreadStart(Start));
                thr.Start();
            }
            else
            {
                ShowMsg("继续爬取");
                AddItem("继续爬取");
                download = 1;//爬取进行中
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(delegate
                    {
                        button1.Enabled = false;
                        button2.Enabled = button3.Enabled = true;
                        button13.Enabled = button14.Enabled = false;
                    }));
                }
                else
                {
                    button1.Enabled = false;
                    button2.Enabled = button3.Enabled = true;
                    button13.Enabled = button14.Enabled = false;
                }
                thr.Resume();
            }
            //ThreadPool.QueueUserWorkItem(
            //    delegate
            //    {
            //        Start();
            //    });
        }

        private bool UserList
        {
            get
            {
                if (this.InvokeRequired)
                {
                    return (bool)this.Invoke(new Func<bool>(delegate { return checkBox1.Checked; }));
                }
                else
                {
                    return checkBox1.Checked;
                }
            }
        }

        private void Start()
        {
            string msg = "";
            signal.Set();
            string patternPic = textBox6.Text.Trim();
            string patternMail = textBox7.Text.Trim();
            string patternLink = textBox8.Text.Trim();
            string patternScript = textBox9.Text.Trim();
            string picDir = textBox2.Text.Trim();
            string mailFile = textBox3.Text.Trim();
            string linkDir = textBox4.Text.Trim();
            string scriptDir = textBox5.Text.Trim();
            if (!Directory.Exists(picDir))
                Directory.CreateDirectory(picDir);
            if (!Directory.Exists(linkDir))
                Directory.CreateDirectory(linkDir);
            if (!Directory.Exists(scriptDir))
                Directory.CreateDirectory(scriptDir);
            if (!File.Exists(mailFile))
            {
                StreamWriter sw = File.CreateText(mailFile);
                sw.Close();
            }
            else
            {
                File.WriteAllText(mailFile, "", Encoding.UTF8);
            }
            while (unload.Count > 0)
            {
                signal.WaitOne();
                if (download == 0)
                    break;
                string url = unload.First().Key;
                int depth = unload.First().Value;
                if (!loaded.ContainsKey(url))
                {
                    loaded.Add(url, depth);
                    unload.Remove(url);
                    RefreshUnLoadList();
                }
                msg = "正在下载[" + url + "]";
                ShowMsg(msg);
                AddItem(msg);

                try
                {
                    WebClient client = new WebClient();
                    client.Credentials = CredentialCache.DefaultCredentials;
                    client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)");
                    client.Encoding = Encoding.GetEncoding(CurrentEncoder);
                    client.Headers.Add(HttpRequestHeader.ContentEncoding, CurrentEncoder);//"GB18030");
                    client.Headers.Add(HttpRequestHeader.Accept, "text/html");
                    string html = client.DownloadString(url);
                    if (!string.IsNullOrEmpty(html))
                    {
                        AnalysisHtml(url, html, patternPic, picDir);
                        AnalysisHtml(url, html, patternMail, mailFile, true);
                        AnalysisHtml(url, html, patternLink, linkDir);
                        AnalysisHtml(url, html, patternScript, scriptDir);
                        msg = "爬取[" + url + "]完毕";
                        ShowMsg(msg);
                        AddItem(msg);
                    }
                    else
                    {
                        msg = "下载[" + url + "]的网页内容为空！";
                        ShowMsg(msg);
                        AddItem(msg);
                    }
                    string[] links = GetLinks(html);
                    AddUrls(links, depth + 1, baseUrl, unload, loaded);
                }
                catch (WebException we)
                {
                    msg = "抓取[" + url + "]时出现异常：" + we.Message;
                    ShowMsg(msg);
                    AddItem(msg);
                }
            }
            if (download != 0)
            {
                download = 3;//下载完毕
                ShowMsg("爬取结束");
                AddItem("爬取结束");
            }
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(delegate
                {
                    button1.Enabled = true;
                    button2.Enabled = button3.Enabled = false;
                    button13.Enabled = button14.Enabled = true;
                }));
            }
            else
            {
                button1.Enabled = true;
                button2.Enabled = button3.Enabled = false;
                button13.Enabled = button14.Enabled = true;
            }
        }

        private void ShowMsg(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(ShowMsg), msg);
            }
            else
            {
                toolStripStatusLabel1.Text = msg;
                statusStrip1.Refresh();
            }
        }

        private string ListVar
        {
            get
            {
                if (this.InvokeRequired)
                {
                    return (string)this.Invoke(new Func<string>(delegate { return ListVar; }));
                }
                else
                {
                    return textBox10.Text.Trim();
                }
            }
        }

        private void AddItem(string item)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AddItem), item);
            }
            else
            {
                listBox1.Items.Add(item);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            }
        }

        private void AnalysisHtml(string url, string html, string pattern, string dir, bool isFile = false)
        {
            List<string> uris = new List<string>();
            ShowMsg("开始分析网页[" + url + "]");
            AddItem("开始分析网页[" + url + "]");
            Regex re = new Regex(pattern);
            MatchCollection matches = re.Matches(html);
            foreach (Match match in matches)
            {
                if (download == 0)
                    break;
                if (match.Success)
                {
                    string uri = match.Value;
                    if (pattern.Contains("Url"))
                        uri = match.Groups["Url"].Value;
                    if (!uris.Contains(uri))
                    {
                        ShowMsg("分析网页[" + url + "]成功，正在下载链接资源[" + uri + "]");
                        Download(uri, dir, isFile);
                        ShowMsg("下载链接资源[" + uri + "]完毕");
                        AddItem("下载链接资源[" + uri + "]完毕");
                    }
                }
                else
                {
                    ShowMsg("分析网页[" + url + "]失败");
                    AddItem("分析网页[" + url + "]失败");
                }
            }
            //Winista.Text.HtmlParser.Lex.Lexer lexer = new Winista.Text.HtmlParser.Lex.Lexer(html);
            //Winista.Text.HtmlParser.Parser parser = new Winista.Text.HtmlParser.Parser(lexer);
            //Winista.Text.HtmlParser.Util.NodeList htmlNodes = parser.Parse(null);
            //for (int i = 0; i < htmlNodes.Count; i++)
            //{
            //    ParseHtml(htmlNodes[i], false, dir);
            //}
        }

        //private void ParseHtml(Winista.Text.HtmlParser.INode htmlNode, bool siblingRequired, string dir)
        //{
        //    if (htmlNode is Winista.Text.HtmlParser.ITag)
        //    {
        //        Winista.Text.HtmlParser.ITag tag = (htmlNode as Winista.Text.HtmlParser.ITag);
        //        if (!tag.IsEndTag())
        //        {
        //            string nodeString = tag.TagName;
        //            if (tag.Attributes != null && tag.Attributes.Count > 0)
        //            {
        //                if (tag.Attributes["ID"] != null)
        //                {
        //                    nodeString = nodeString + " { id=\"" + tag.Attributes["ID"].ToString() + "\" }";
        //                }
        //                if (tag.Attributes["HREF"] != null)
        //                {
        //                    nodeString = nodeString + " { href=\"" + tag.Attributes["HREF"].ToString() + "\" }";
        //                }
        //            }
        //            Download(nodeString, dir);
        //        }
        //    }

        //    //获取节点间的内容  
        //    if (htmlNode.Children != null && htmlNode.Children.Count > 0)
        //    {
        //        ParseHtml(htmlNode.FirstChild, true, dir);
        //        Download(htmlNode.FirstChild.GetText(), dir);
        //    }

        //    //the sibling nodes
        //    if (siblingRequired)
        //    {
        //        Winista.Text.HtmlParser.INode sibling = htmlNode.NextSibling;
        //        while (sibling != null)
        //        {
        //            ParseHtml(sibling, false, dir);
        //            sibling = sibling.NextSibling;
        //        }
        //    }
        //}

        private void Download(string uri, string dir, bool isFile = false)
        {
            if (isFile)
            {
                File.AppendAllText(dir, uri + Environment.NewLine, Encoding.UTF8);
            }
            else
            {
                if (!dir.EndsWith("\\"))
                    dir += "\\";
                if (!string.IsNullOrEmpty(uri))
                {
                    try
                    {
                        string filename = Path.GetFileName(uri);
                        if (!Common.IsValidFileName(filename))
                        {
                            string ext = Path.GetExtension(uri);
                            filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ext;
                        }
                        WebClient client = new WebClient();
                        client.Credentials = CredentialCache.DefaultCredentials;
                        client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)");
                        client.DownloadFile(uri, dir + filename);
                        //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                        //req.Method = "GET";
                        //req.Accept = "text/html";
                        //req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)";

                        //    HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                        //    if (res.StatusCode == HttpStatusCode.OK)
                        //    {
                        //        using (Stream inStream = res.GetResponseStream())
                        //        {
                        //            byte[] buffer = new byte[1024];
                        //            string FileName = dir + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ext;
                        //            using (Stream outStream = System.IO.File.Create(FileName))
                        //            {
                        //                int l;
                        //                do
                        //                {
                        //                    l = inStream.Read(buffer, 0, buffer.Length);
                        //                    if (l > 0)
                        //                        outStream.Write(buffer, 0, l);
                        //                }
                        //                while (l > 0);
                        //            }
                        //        }
                        //    }
                    }
                    catch (Exception e)
                    {
                        Logs.Create("下载[" + uri + "]时出现异常：" + e.Message);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            thr.Suspend();
            signal.Reset();
            download = 2;//暂停
            button1.Enabled = true;
            button2.Enabled = false;
            button13.Enabled = button14.Enabled = true;
            listBox1.Items.Add("暂停爬取");
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            toolStripStatusLabel1.Text = "暂停爬取";
            statusStrip1.Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            thr.Abort();
            signal.Set();
            download = 0;//停止
            button1.Enabled = true;
            button2.Enabled = button3.Enabled = false;
            button13.Enabled = button14.Enabled = true;
            listBox1.Items.Add("停止爬取");
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            toolStripStatusLabel1.Text = "停止爬取";
            statusStrip1.Refresh();
        }

        private static string[] GetLinks(string html)
        {
            const string pattern = @"http://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(html);
            string[] links = new string[m.Count];

            for (int i = 0; i < m.Count; i++)
            {
                links[i] = m[i].ToString();
            }
            return links;
        }

        private static bool UrlAvailable(string url, Dictionary<string, int> unload, Dictionary<string, int> loaded)
        {
            if (unload.ContainsKey(url) || loaded.ContainsKey(url))
            {
                return false;
            }
            if (url.Contains(".jpg") || url.Contains(".gif")
                || url.Contains(".png") || url.Contains(".css")
                || url.Contains(".js"))
            {
                return false;
            }
            return true;
        }

        private static void AddUrls(string[] urls, int depth, string baseUrl, Dictionary<string, int> unload, Dictionary<string, int> loaded)
        {
            if (depth >= Convert.ToInt32(Common.GetAppSettingConfig("depth")))
            {
                return;
            }
            foreach (string url in urls)
            {
                string cleanUrl = url.Trim();
                int end = cleanUrl.IndexOf(' ');
                if (end > 0)
                {
                    cleanUrl = cleanUrl.Substring(0, end);
                }
                if (UrlAvailable(cleanUrl, unload, loaded))
                {
                    if (cleanUrl.Contains(baseUrl))
                    {
                        unload.Add(cleanUrl, depth);
                    }
                    else
                    {
                        // 外链
                    }
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                Common.SaveAppSettingConfig("depth", numericUpDown3.Value.ToString());
                Common.SaveAppSettingConfig("baseUrl", textBox1.Text.Trim());
                Common.SaveAppSettingConfig("list", checkBox1.Checked ? "1" : "0");
                Common.SaveAppSettingConfig("listVar", textBox10.Text.Trim());
                Common.SaveAppSettingConfig("listIndexFrom", numericUpDown1.Value.ToString());
                Common.SaveAppSettingConfig("listIndexTo", numericUpDown2.Value.ToString());
                Common.SaveAppSettingConfig("picDir", textBox2.Text);
                Common.SaveAppSettingConfig("mailFile", textBox3.Text);
                Common.SaveAppSettingConfig("linkDir", textBox4.Text);
                Common.SaveAppSettingConfig("scriptDir", textBox5.Text);
                Common.SaveAppSettingConfig("picReg", textBox6.Text.Trim().Replace("\"", "#").Replace("&", "@").Replace("<", ","));
                Common.SaveAppSettingConfig("mailReg", textBox7.Text.Trim());
                Common.SaveAppSettingConfig("linkReg", textBox8.Text.Trim().Replace("\"", "#").Replace("&", "@").Replace("<", ","));
                Common.SaveAppSettingConfig("scriptReg", textBox9.Text.Trim().Replace("\"", "#").Replace("&", "@").Replace("<", ","));
                Common.SaveAppSettingConfig("encoding", comboBox1.Text);
                MessageBox.Show("设置保存成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置保存失败：" + ex.Message);
            }
        }

        private void 保存为日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog3.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> history = new List<string>();
                for (int i = 1; i <= listBox1.Items.Count; i++)
                {
                    string msg = "[" + i + "]" + listBox1.Items[i - 1];
                    history.Add(msg);
                }
                File.WriteAllLines(saveFileDialog3.FileName, history, Encoding.UTF8);
                MessageBox.Show("爬虫的运动轨迹已经保存为日志！您可以清空目前为止所有的运动轨迹了。");
            }
        }

        private void 清空爬虫运动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空所有运动轨迹么？如果想要记录轨迹，请先保存为日志后再清空。", "清空提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
            {
                listBox1.Items.Clear();
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listBox1.Items.Count == 0)
            {
                保存为日志ToolStripMenuItem.Enabled = 清空爬虫运动ToolStripMenuItem.Enabled = false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox10.Enabled = numericUpDown1.Enabled = numericUpDown2.Enabled = checkBox1.Checked;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox3.Text = openFileDialog2.FileName;
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox4.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox5.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private string GetBaseUrl()
        {
            string BASEURL = "";
            baseUrl = textBox1.Text.Trim();
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";
            if (checkBox1.Checked)
            {
                List<string> baseUrls = new List<string>();
                string listVar = textBox10.Text.Trim();
                if (listVar != "" && listVar.Contains(PAGEINDEX))
                {
                    int start = (int)numericUpDown1.Value;
                    int end = (int)numericUpDown2.Value;
                    if (start > end)
                    {
                        int tmp = start;
                        start = end;
                        end = tmp;
                    }
                    for (int i = start; i <= end; i++)
                    {
                        baseUrls.Add(baseUrl + listVar.Replace(PAGEINDEX, i.ToString()));
                    }
                }
                else
                {
                    MessageBox.Show("请填写合法的列表变量！" + Environment.NewLine + "类似【page-{PAGEINDEX}】的格式");
                }
                foreach (string URL in baseUrls)
                {
                    if (!URL.Contains("://"))
                        BASEURL = "http://" + URL;
                    else
                        BASEURL = URL;
                }
            }
            else
            {
                if (!baseUrl.Contains("://"))
                    BASEURL = "http://" + baseUrl;
                else
                    BASEURL = baseUrl;
            }
            return BASEURL;
        }

        //private void ParseHtml(Winista.Text.HtmlParser.INode htmlNode, bool siblingRequired, ref List<string> results)
        //{
        //    if (htmlNode is Winista.Text.HtmlParser.ITag)
        //    {
        //        Winista.Text.HtmlParser.ITag tag = (htmlNode as Winista.Text.HtmlParser.ITag);
        //        if (!tag.IsEndTag())
        //        {
        //            string nodeString = tag.TagName;
        //            if (tag.Attributes != null && tag.Attributes.Count > 0)
        //            {
        //                if (tag.Attributes["ID"] != null)
        //                {
        //                    nodeString = nodeString + " { id=\"" + tag.Attributes["ID"].ToString() + "\" }";
        //                }
        //                if (tag.Attributes["HREF"] != null)
        //                {
        //                    nodeString = nodeString + " { href=\"" + tag.Attributes["HREF"].ToString() + "\" }";
        //                }
        //            }
        //            results.Add(nodeString);
        //        }
        //    }

        //    //获取节点间的内容  
        //    if (htmlNode.Children != null && htmlNode.Children.Count > 0)
        //    {
        //        ParseHtml(htmlNode.FirstChild, true, ref results);
        //        results.Add(htmlNode.FirstChild.GetText());
        //    }

        //    //the sibling nodes
        //    if (siblingRequired)
        //    {
        //        Winista.Text.HtmlParser.INode sibling = htmlNode.NextSibling;
        //        while (sibling != null)
        //        {
        //            ParseHtml(sibling, false, ref results);
        //            sibling = sibling.NextSibling;
        //        }
        //    }
        //}

        private List<string> GetResults(string pattern, string html)
        {
            List<string> results = new List<string>();
            Regex re = new Regex(pattern);
            MatchCollection matches = re.Matches(html);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string result = match.Value;
                    if (pattern.Contains("Url"))
                        result = match.Groups["Url"].Value;
                    if (!results.Contains(result))
                        results.Add(result);
                }
            }
            //Winista.Text.HtmlParser.Lex.Lexer lexer = new Winista.Text.HtmlParser.Lex.Lexer(html);
            //Winista.Text.HtmlParser.Parser parser = new Winista.Text.HtmlParser.Parser(lexer);
            //Winista.Text.HtmlParser.Util.NodeList htmlNodes = parser.Parse(null);
            //for (int i = 0; i < htmlNodes.Count; i++)
            //{
            //    ParseHtml(htmlNodes[i], false, ref results);
            //}
            return results;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string url = GetBaseUrl();
            if (!string.IsNullOrEmpty(url))
            {
                string pattern = textBox6.Text.Trim();
                if (pattern != "")
                {
                    string html = GetHtml(url);
                    List<string> results = GetResults(pattern, html);
                    Form2 testResult = new Form2(results);
                    testResult.Show();
                }
                else
                {
                    MessageBox.Show("请填写抓取模式（正则表达式）！");
                }
            }
            else
            {
                MessageBox.Show("目前没有任何基地址！");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string url = GetBaseUrl();
            if (!string.IsNullOrEmpty(url))
            {
                string pattern = textBox7.Text.Trim();
                if (pattern != "")
                {
                    string html = GetHtml(url);
                    List<string> results = GetResults(pattern, html);
                    Form2 testResult = new Form2(results);
                    testResult.Show();
                }
                else
                {
                    MessageBox.Show("请填写抓取模式（正则表达式）！");
                }
            }
            else
            {
                MessageBox.Show("目前没有任何基地址！");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string url = GetBaseUrl();
            if (!string.IsNullOrEmpty(url))
            {
                string pattern = textBox8.Text.Trim();
                if (pattern != "")
                {
                    string html = GetHtml(url);
                    List<string> results = GetResults(pattern, html);
                    Form2 testResult = new Form2(results);
                    testResult.Show();
                }
                else
                {
                    MessageBox.Show("请填写抓取模式（正则表达式）！");
                }
            }
            else
            {
                MessageBox.Show("目前没有任何基地址！");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string url = GetBaseUrl();
            if (!string.IsNullOrEmpty(url))
            {
                string pattern = textBox9.Text.Trim();
                if (pattern != "")
                {
                    string html = GetHtml(url);
                    List<string> results = GetResults(pattern, html);
                    Form2 testResult = new Form2(results);
                    testResult.Show();
                }
                else
                {
                    MessageBox.Show("请填写抓取模式（正则表达式）！");
                }
            }
            else
            {
                MessageBox.Show("目前没有任何基地址！");
            }
        }

        private string GetHtml(string url)
        {
            string html = "";
            //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            //req.Method = "GET";
            //req.Accept = "text/html";
            //req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)";
            try
            {
                WebClient client = new WebClient();
                client.Credentials = CredentialCache.DefaultCredentials;
                client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)");
                client.Encoding = Encoding.GetEncoding(CurrentEncoder);
                client.Headers.Add(HttpRequestHeader.ContentEncoding, CurrentEncoder);//"GB18030");
                client.Headers.Add(HttpRequestHeader.Accept, "text/html");
                html = client.DownloadString(url);
                //HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                //if (res.StatusCode == HttpStatusCode.OK)
                //{
                //    using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                //    {
                //        html = reader.ReadToEnd();
                //    }
                //}
                //else
                //{
                //    MessageBox.Show("下载[" + url + "]失败！");
                //}
            }
            catch (WebException we)
            {
                MessageBox.Show("下载[" + url + "]时出现异常：" + we.Message);
            }
            return html;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                List<string> list = new List<string>();
                foreach (string key in unload.Keys)
                {
                    int level = unload[key];
                    list.Add("[" + level + "]" + key);
                }
                list.Sort();
                File.WriteAllLines(saveFileDialog1.FileName, list, Encoding.UTF8);
                MessageBox.Show("保存成功！");
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog()== DialogResult.OK)
            {
                string[] list = File.ReadAllLines(openFileDialog1.FileName, Encoding.UTF8);
                foreach (string line in list)
                {
                    int split = line.IndexOf(']');
                    if (split > -1)
                    {
                        //[123]http://www.abc.com/image.jpg
                        string url = line.Substring(split + 1);
                        int level = Convert.ToInt32(line.Substring(1, split - 1));
                        if (!unload.ContainsKey(url))
                        {
                            unload.Add(url, level);
                            RefreshUnLoadList();
                        }
                    }
                }
                MessageBox.Show("载入成功！");
            }
        }

        private void RefreshUnLoadList()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RefreshUnLoadList));
            }
            else
            {
                listBox2.Items.Clear();
                List<string> list = new List<string>();
                foreach (string url in unload.Keys)
                {
                    int level = Convert.ToInt32(unload[url]);
                    list.Add("[" + level + "]" + url);
                }
                list.Sort();
                foreach (string u in list)
                {
                    listBox2.Items.Add(u);
                }
            }
        }

        private void textBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                Process.Start(textBox2.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开目录时出错：" + ex.Message);
            }
        }

        private void textBox3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                Process.Start(textBox3.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开文件时出错：" + ex.Message);
            }
        }

        private void textBox4_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                Process.Start(textBox4.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开目录时出错：" + ex.Message);
            }
        }

        private void textBox5_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                Process.Start(textBox5.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开目录时出错：" + ex.Message);
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (saveFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                INI ini = new INI(saveFileDialog2.FileName);

                ini.WriteItem("appSettings", "depth", numericUpDown3.Value.ToString());
                ini.WriteItem("appSettings", "baseUrl", textBox1.Text.Trim());
                ini.WriteItem("appSettings", "list", checkBox1.Checked ? "1" : "0");
                ini.WriteItem("appSettings", "listVar", textBox10.Text.Trim());
                ini.WriteItem("appSettings", "listIndexFrom", numericUpDown1.Value.ToString());
                ini.WriteItem("appSettings", "listIndexTo", numericUpDown2.Value.ToString());
                ini.WriteItem("appSettings", "picDir", textBox2.Text);
                ini.WriteItem("appSettings", "mailFile", textBox3.Text);
                ini.WriteItem("appSettings", "linkDir", textBox4.Text);
                ini.WriteItem("appSettings", "scriptDir", textBox5.Text);
                ini.WriteItem("appSettings", "picReg", textBox6.Text.Trim().Replace("\"", "#").Replace("&", "@").Replace("<", ","));
                ini.WriteItem("appSettings", "mailReg", textBox7.Text.Trim());
                ini.WriteItem("appSettings", "linkReg", textBox8.Text.Trim().Replace("\"", "#").Replace("&", "@").Replace("<", ","));
                ini.WriteItem("appSettings", "scriptReg", textBox9.Text.Trim().Replace("\"", "#").Replace("&", "@").Replace("<", ","));
                ini.WriteItem("appSettings", "encoding", comboBox1.Text);
                MessageBox.Show("保存成功！");
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (openFileDialog3.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                INI ini = new INI(openFileDialog3.FileName);

                numericUpDown3.Value = Convert.ToInt32(ini.ReadItemValue("appSettings", "depth"));
                textBox1.Text = baseUrl = ini.ReadItemValue("appSettings", "baseUrl");
                checkBox1.Checked = ini.ReadItemValue("appSettings", "list") == "1";
                textBox10.Text = ini.ReadItemValue("appSettings", "listVar");
                numericUpDown1.Value = Convert.ToInt32(ini.ReadItemValue("appSettings", "listIndexFrom"));
                numericUpDown2.Value = Convert.ToInt32(ini.ReadItemValue("appSettings", "listIndexTo"));
                textBox2.Text = ini.ReadItemValue("appSettings", "picDir");
                textBox3.Text = ini.ReadItemValue("appSettings", "mailFile");
                textBox4.Text = ini.ReadItemValue("appSettings", "linkDir");
                textBox5.Text = ini.ReadItemValue("appSettings", "scriptDir");
                textBox6.Text = ini.ReadItemValue("appSettings", "picReg").Replace("#", "\"").Replace("@", "&").Replace(",", "<");
                textBox7.Text = ini.ReadItemValue("appSettings", "mailReg");
                textBox8.Text = ini.ReadItemValue("appSettings", "linkReg").Replace("#", "\"").Replace("@", "&").Replace(",", "<");
                textBox9.Text = ini.ReadItemValue("appSettings", "scriptReg").Replace("#", "\"").Replace("@", "&").Replace(",", "<");
                comboBox1.Text = ini.ReadItemValue("appSettings", "encoding");
                MessageBox.Show("载入成功！");
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            baseUrl = textBox1.Text.Trim();
            comboBox1.Text = Common.GetEncoding(baseUrl);
        }

        public string CurrentEncoder
        {
            get
            {
                if (this.InvokeRequired)
                {
                    return this.Invoke(new Func<string>(delegate { return CurrentEncoder; })).ToString();
                }
                else
                {
                    return comboBox1.Text.Trim();
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (thr != null)
            {
                if (thr.ThreadState != System.Threading.ThreadState.Aborted)
                    thr.Abort();
            }
        }
    }
}
