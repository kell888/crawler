using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace 网络爬虫
{
    public partial class Form2 : Form
    {
        public Form2(List<string> results)
        {
            InitializeComponent();
            this.results = results;
            this.Text = "抓取测试结果 -- " + results.Count + "条记录";
        }

        List<string> results;

        private void Form2_Load(object sender, EventArgs e)
        {
            if (results != null)
            {
                ThreadPool.QueueUserWorkItem(
                    delegate
                    {
                        if (listBox1.InvokeRequired)
                        {
                            listBox1.Invoke(new Action(delegate { listBox1.Items.Clear(); }));
                        }
                        else
                        {
                            listBox1.Items.Clear();
                        }
                        foreach (string result in results)
                        {
                            if (listBox1.InvokeRequired)
                            {
                                listBox1.Invoke(new Action<string>(a => listBox1.Items.Add(a)), result);
                            }
                            else
                            {
                                listBox1.Items.Add(result);
                            }
                        }
                    });
            }
        }
    }
}
