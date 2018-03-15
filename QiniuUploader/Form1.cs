using Newtonsoft.Json;
using Qiniu.Common;
using Qiniu.Http;
using Qiniu.Storage;
using Qiniu.Storage.Model;
using Qiniu.Storage.Persistent;
using Qiniu.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace QiniuUploader
{
    public partial class frmMain : Form
    {
        public string BucDomain = "";

        public frmMain()
        {
            InitializeComponent();
        }

        private Mac GetMac()
        {
            return new Mac(Properties.Settings.Default.AccessKey, Properties.Settings.Default.SecretKey);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string localFile = textBox1.Text.Trim();

            FileInfo fi = new FileInfo(localFile);

            string saveKey = fi.Name;
            string bucket = comboBox1.Text;

            // 生成上传凭证
            Mac mac = this.GetMac();

            BucketManager bm = new BucketManager(mac);
            //BucketsResult rs = bm.buckets();

            DomainsResult dr = bm.domains(bucket);
            this.BucDomain = dr.Domains[0];
            //this.BucDomain

            // 上传策略
            PutPolicy putPolicy = new PutPolicy();
            // 设置要上传的目标空间
            putPolicy.Scope = bucket;
            // 上传策略的过期时间(单位:秒)
            putPolicy.SetExpires(3600);
            // 文件上传完毕后，在多少天后自动被删除
            putPolicy.DeleteAfterDays = 0;
            // 请注意这里的Zone设置(如果不设置，就默认为华东机房)
            // var zoneId = Qiniu.Common.AutoZone.Query(AK,BUCKET);
            // Qiniu.Common.Config.ConfigZone(zoneId);
            string uploadToken = Auth.createUploadToken(putPolicy, mac);

            UploadOptions uploadOptions = null;
            // 上传完毕事件处理
            UpCompletionHandler uploadCompleted = new UpCompletionHandler(OnUploadCompleted);
            // 方式1：使用UploadManager
            //默认设置 Qiniu.Common.Config.PUT_THRESHOLD = 512*1024;
            //可以适当修改,UploadManager会根据这个阈值自动选择是否使用分片(Resumable)上传    
            UploadManager um = new UploadManager();
            um.uploadFile(localFile, saveKey, uploadToken, uploadOptions, uploadCompleted);
            // 方式2：使用FormManager
            //FormUploader fm = new FormUploader();
            //fm.uploadFile(localFile, saveKey, token, uploadOptions, uploadCompleted);
            //Console.ReadKey();
        }

        private void OnUploadCompleted(string key, ResponseInfo respInfo, string respJson)
        {
            // respInfo.StatusCode

            if(respInfo.StatusCode == 200)
            {
                Dictionary<string, string> respAttr = JsonConvert.DeserializeObject<Dictionary<string, string>>(respJson);
                textBox2.Text = textBox1.Text + " [SUCCESS] \r\n    http://" + this.BucDomain + "/" + respAttr["key"] + "\r\n";
            }
            else
            {
                textBox2.Text = textBox1.Text + " SUCCESS \r\n    " + respInfo.Error + "\r\n";
            }
            // respJson是返回的json消息，示例: { "key":"FILE","hash":"HASH","fsize":FILE_SIZE }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = dlg.FileName;
                button2.Enabled = true;
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.freshBuckets();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            this.freshBuckets();
        }

        private void freshBuckets()
        {
            Mac mac = this.GetMac();

            BucketManager bm = new BucketManager(mac);
            BucketsResult rs = bm.buckets();

            if(rs.ResponseInfo.StatusCode!=200)
            {
                MessageBox.Show(rs.ResponseInfo.Error);
                return;
            }

            comboBox1.Enabled = true;
            button1.Enabled = true;

            comboBox1.Items.Clear();
            foreach (string buc in rs.Buckets)
            {
                //DomainsResult domains = bm.domains(buc);
                comboBox1.Items.Add(buc);
            }
            comboBox1.SelectedIndex = 0;
        }

    }
}
