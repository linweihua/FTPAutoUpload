using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;
using FTPAutoUpload.Properties;
using Timer = System.Threading.Timer;

namespace FTPAutoUpload
{
    public partial class Form1 : Form
    {
        private Timer timer;
        private  delegate int getSetUploadHour();

        private NotifyIcon M_NotifyIcon;

        private getSetUploadHour getUploadHour;
        public Form1()
        {
            InitializeComponent();
            M_NotifyIcon = new NotifyIcon(this.components);
            M_NotifyIcon.Icon = this.Icon; 
            System.Windows.Forms.ContextMenuStrip menu = new ContextMenuStrip(this.components);
           
            ToolStripItem showWindowItem = menu.Items.Add("显示窗口");
            showWindowItem.Click += new EventHandler(showWindow_Click);
            ToolStripItem exitItem = menu.Items.Add("退出程序");
            exitItem.Click += new EventHandler(exitItem_Click);
            M_NotifyIcon.ContextMenuStrip = menu;
            M_NotifyIcon.DoubleClick+=new EventHandler(showWindow_Click);
            M_NotifyIcon.Visible = true;
                
            this.combUploadTime.SelectedIndex = 0;
            CheckForIllegalCrossThreadCalls = false;
            this.getUploadHour = this.getUploadTime;
            this.txtFtpAddress.Text = Settings.Default.FtpIp;
            this.txtFtpPort.Text = Settings.Default.FtpPort;
            this.txtPassword.Text = Settings.Default.FtpPassword;
            this.txtUsername.Text = Settings.Default.FtpUserName;

        }

        private void exitItem_Click(object sender,EventArgs e)
        {
            Application.Exit();
        }

        private void showWindow_Click(object sender, EventArgs e)
        {
           
            this.Show();
            this.ShowInTaskbar = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private int getUploadTime()
        {
            return int.Parse(this.combUploadTime.SelectedItem.ToString());
        }
        private void timer_Callback(object stateInfo)
        {
            DateTime dateTime = DateTime.Now;
            int uploadHour=Convert.ToInt16(this.Invoke(this.getUploadHour, new object[] {})) ;
            if (dateTime.Hour == uploadHour)
            {
                string fileName = string.Format("{0}.7z", DateTime.Now.ToString("yyyyMMdd"));
                string target7ZFilePath = Path.Combine("D:", fileName);
                Compressor.CompressFileFolder(this.txtLocalAddress.Text, target7ZFilePath);
                string ip = this.txtFtpAddress.Text;
                string port = this.txtFtpPort.Text;
                string password = this.txtPassword.Text;
                string userName = this.txtUsername.Text;
                var uploadFile = UploadFile(target7ZFilePath, string.Format(@"ftp://{0}:{2}/{1}", ip, fileName, port), userName, password);
            }
        }
        private void btnSelectLocalAddress_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderOpen = new FolderBrowserDialog();
            if (folderOpen.ShowDialog() == DialogResult.OK)
            {
                this.txtLocalAddress.Text = folderOpen.SelectedPath;
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            this.timer = new Timer(timer_Callback,null,100,1*60*60*1000);
            this.btnConfirm.Enabled = false;
            this.Hide();

        }
        private static FtpStatusCode UploadFile(string fileName, string uploadUrl, string userName, string userPassword)
        {
            Stream requestStream = null;
            FileStream fileStream = null;
            FtpWebResponse uploadResponse = null;
            try
            {
                FtpWebRequest uploadRequest =
                (FtpWebRequest)WebRequest.Create(uploadUrl);
                uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;

                uploadRequest.Proxy = null;
                NetworkCredential nc = new NetworkCredential();
                nc.UserName = userName;
                nc.Password = userPassword;

                uploadRequest.Credentials = nc; //修改getCredential();错误2

                requestStream = uploadRequest.GetRequestStream();
                fileStream = File.Open(fileName, FileMode.Open);

                byte[] buffer = new byte[1024];
                int bytesRead;
                while (true)
                {
                    bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    requestStream.Write(buffer, 0, bytesRead);
                }
                requestStream.Close();

                uploadResponse = (FtpWebResponse)uploadRequest.GetResponse();
                return uploadResponse.StatusCode;

            }
            catch (UriFormatException ex)
            {
                
            }
            catch (IOException ex)
            {
              
            }
            catch (WebException ex)
            {
                
            }
            finally
            {
                if (uploadResponse != null)
                    uploadResponse.Close();
                if (fileStream != null)
                    fileStream.Close();
                if (requestStream != null)
                    requestStream.Close();
            }
            return FtpStatusCode.Undefined;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Minimized)
           this.Hide();
            this.ShowInTaskbar = false;
        }

    }
}
