using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.IO;

// �������dotnet publish -c Release -r win-x64 --self-contained false
// �������dotnet run

namespace ymzx
{
    internal partial class Form1 : Form
    {
        private Button btnStartStop;
        private Button btnRefresh;
        private Button btnClearCache; // ��������btnVersionUpdate��ΪbtnClearCache
        private Button btnFarmGuide;
        private Button btnScheduledTasks;
        private Button btnGithub;
        private CheckBox checkBoxGPU;
        internal CheckBox checkBoxNoMonthlyCard;
        internal WebView2 webView2;
        
        // ��¼��ǰʵ�����û�����Ŀ¼
        private string userDataFolder;

        public Form1()
        {
            InitializeComponent();
            ControlActions.SetMainForm(this);  // ��������������
            // ��������������
            this.Text = "Ԫ��֮��ũ������ 2.5 GPU���ð�+���¿�����";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ClientSize = new Size(960, 580);
            this.Icon = new Icon("./app.ico");  // ����Ŀ��Ŀ¼����ͼ��

            // ��ʼ�����пؼ�
            InitializeControls();

            // �ڴ������ʱ��ʼ�� WebView2 �ؼ�
            this.Load += Form1_Load;
        }

        private void InitializeControls()
        {
            // ��һ�����򣺰�ť��GPUѡ��
            int buttonWidth = 100;
            int buttonHeight = 30;
            int spacing = 5;
            int startX = spacing;
            int startY = spacing;

            // "��ʼ/ֹͣ"��ť
            btnStartStop = new Button();
            btnStartStop.Text = "��ʼ/ֹͣ";
            btnStartStop.Size = new Size(buttonWidth, buttonHeight);
            btnStartStop.Location = new Point(startX, startY);
            btnStartStop.FlatStyle = FlatStyle.Flat;
            btnStartStop.FlatAppearance.BorderSize = 0;
            btnStartStop.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnStartStop.Click += ControlActions.BtnStartStop_Click;
            this.Controls.Add(btnStartStop);

            // "ˢ��"��ť
            btnRefresh = new Button();
            btnRefresh.Text = "ˢ��";
            btnRefresh.Size = new Size(buttonWidth, buttonHeight);
            btnRefresh.Location = new Point(startX + (buttonWidth + spacing) * 1, startY);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnRefresh.Click += ControlActions.BtnRefresh_Click;
            this.Controls.Add(btnRefresh);

            // "������"��ť (ԭ"�汾����"��ť)
            btnClearCache = new Button();
            btnClearCache.Text = "������";
            btnClearCache.Size = new Size(buttonWidth, buttonHeight);
            btnClearCache.Location = new Point(startX + (buttonWidth + spacing) * 2, startY);
            btnClearCache.FlatStyle = FlatStyle.Flat;
            btnClearCache.FlatAppearance.BorderSize = 0;
            btnClearCache.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnClearCache.Click += ControlActions.BtnClearCache_Click;
            this.Controls.Add(btnClearCache);

            // "͵��"��ť
            btnFarmGuide = new Button();
            btnFarmGuide.Text = "͵��";
            btnFarmGuide.Size = new Size(buttonWidth, buttonHeight);
            btnFarmGuide.Location = new Point(startX + (buttonWidth + spacing) * 3, startY);
            btnFarmGuide.FlatStyle = FlatStyle.Flat;
            btnFarmGuide.FlatAppearance.BorderSize = 0;
            btnFarmGuide.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnFarmGuide.Click += ControlActions.BtnStealing_Click;
            this.Controls.Add(btnFarmGuide);

            // "��ʱ����"��ť
            btnScheduledTasks = new Button();
            btnScheduledTasks.Text = "��ʱ����";
            btnScheduledTasks.Size = new Size(buttonWidth, buttonHeight);
            btnScheduledTasks.Location = new Point(startX + (buttonWidth + spacing) * 4, startY);
            btnScheduledTasks.FlatStyle = FlatStyle.Flat;
            btnScheduledTasks.FlatAppearance.BorderSize = 0;
            btnScheduledTasks.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnScheduledTasks.Click += new System.EventHandler(ControlActions.BtnScheduledTasks_Click);
            this.Controls.Add(btnScheduledTasks);

            // "Github"��ť
            btnGithub = new Button();
            btnGithub.Text = "Github";
            btnGithub.Size = new Size(buttonWidth, buttonHeight);
            btnGithub.Location = new Point(startX + (buttonWidth + spacing) * 5, startY);
            btnGithub.FlatStyle = FlatStyle.Flat;
            btnGithub.FlatAppearance.BorderSize = 0;
            btnGithub.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnGithub.Click += (sender, e) => {
                try
                {
                    Process.Start(new ProcessStartInfo("https://github.com/Hollyming") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�޷�������: " + ex.Message);
                }
            };
            this.Controls.Add(btnGithub);

            // "�ض�"��ť
            Button btnMustRead = new Button();
            btnMustRead.Text = "�ض�";
            btnMustRead.Size = new Size(buttonWidth, buttonHeight);
            btnMustRead.Location = new Point(startX + (buttonWidth + spacing) * 6, startY);
            btnMustRead.FlatStyle = FlatStyle.Flat;
            btnMustRead.FlatAppearance.BorderSize = 0;
            btnMustRead.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnMustRead.Click += ControlActions.BtnMustRead_Click;
            this.Controls.Add(btnMustRead);

            // ���ұ���� GPU ����ѡ������¿��汾ѡ��
            checkBoxGPU = new CheckBox();
            checkBoxGPU.Text = "����GPU";
            checkBoxGPU.Checked = true; // �̶�Ϊ����GPU
            checkBoxGPU.Enabled = false; // �����û����������ɸ���
            checkBoxGPU.Size = new Size(100, buttonHeight);
            // �����ڵ����ڶ���λ��
            checkBoxGPU.Location = new Point(this.ClientSize.Width - 200 - spacing, startY);
            this.Controls.Add(checkBoxGPU);

            // ������¿��汾��ѡ��
            checkBoxNoMonthlyCard = new CheckBox();
            checkBoxNoMonthlyCard.Text = "���¿��汾";
            checkBoxNoMonthlyCard.Checked = false; // Ĭ�ϲ�����
            checkBoxNoMonthlyCard.Size = new Size(100, buttonHeight);
            // ���������ұ�
            checkBoxNoMonthlyCard.Location = new Point(this.ClientSize.Width - 100 - spacing, startY);
            this.Controls.Add(checkBoxNoMonthlyCard);

            // �ڶ�������WebView2 �ؼ����ޱ߿�λ�ý�����һ���·�
            webView2 = new WebView2();
            webView2.Location = new Point(0, buttonHeight + 2 * spacing);
            webView2.Size = new Size(960, 540);
            this.Controls.Add(webView2);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await InitializeWebView2();
        }

        // ���� GPU ѡ���ʼ�� WebView2 �ؼ�����֧�ֶ࿪
        public async Task InitializeWebView2()
        {
            try
            {
                // ����һ�����ڵ�ǰ����ID��ʱ�����Ψһ�û������ļ���
                string baseFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "YmzxHelper", "UserData");
                
                // ȷ������Ŀ¼����
                if (!Directory.Exists(baseFolder))
                {
                    Directory.CreateDirectory(baseFolder);
                }
                
                // �������ڽ���ID��ʱ�����Ψһ�ļ�����
                int processId = Process.GetCurrentProcess().Id;
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                userDataFolder = Path.Combine(baseFolder, $"Instance_{processId}_{timestamp}");
                
                // ȷ���û�����Ŀ¼����
                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }
                
                // ����WebView2����ѡ��
                var options = new CoreWebView2EnvironmentOptions();
                if (!checkBoxGPU.Checked)
                {
                    options.AdditionalBrowserArguments = "--disable-gpu";
                }
                else
                {
                    // options.AdditionalBrowserArguments = "--enable-gpu";
                    /*ʹ�ø�����GPU*/
                    options.AdditionalBrowserArguments = "--use-angle=d3d11 --gpu-preference=high-performance";
                }
                
                // ����WebView2������ָ��Ψһ���û�����Ŀ¼
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                await webView2.EnsureCoreWebView2Async(env);
                
                // ��ȡϵͳDPI���ű���
                float dpiScale;
                using (var graphics = this.CreateGraphics())
                {
                    dpiScale = graphics.DpiX / 96.0f; // ��׼DPI��96
                }
                
                // ����WebView2��������������ӦDPI����
                if (Math.Abs(dpiScale - 1.0f) > 0.01f)
                {
                    // ����ϵͳDPI���Ų���1.0ʱ�ŵ���ZoomFactor
                    webView2.CoreWebView2.Settings.IsZoomControlEnabled = false; // �����û����ſ���
                    
                    // �ȴ�һ��ʱ��ȷ����ҳ��ȫ����
                    await Task.Delay(500);
                    
                    // �������ű�����ϵͳDPI��ƥ��
                    webView2.ZoomFactor = 1.0 / dpiScale;
                    
                    Console.WriteLine($"����WebView2���ű���Ϊ: {1.0 / dpiScale} (ϵͳDPI����: {dpiScale})");
                }
                
                // ����ָ����ҳ
                webView2.CoreWebView2.Navigate("https://gamer.qq.com/v2/game/96897?ichannel=pcgames0Fpcgames1");
                
                // ��ӱ��ر�ʱ���������
                this.FormClosed += (s, e) => {
                    try {
                        // ���������û�����Ŀ¼
                        if (Directory.Exists(userDataFolder))
                        {
                            // ��ѡ: �����û�����Ŀ¼�������Ҫ��ȫɾ�����ݣ�
                            // Directory.Delete(userDataFolder, true);
                        }
                    }
                    catch (Exception ex) {
                        // ������������еĴ���
                        Debug.WriteLine($"�����û�����Ŀ¼ʱ����: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 ��ʼ��ʧ��: " + ex.Message);
            }
        }
    }
}
