using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.IO;

// 发布命令：dotnet publish -c Release -r win-x64 --self-contained false
// 测试命令：dotnet run

namespace ymzx
{
    internal partial class Form1 : Form
    {
        private Button btnStartStop;
        private Button btnRefresh;
        private Button btnClearCache; // 改名：从btnVersionUpdate改为btnClearCache
        private Button btnFarmGuide;
        private Button btnScheduledTasks;
        private Button btnGithub;
        private CheckBox checkBoxGPU;
        internal CheckBox checkBoxNoMonthlyCard;
        internal WebView2 webView2;
        
        // 记录当前实例的用户数据目录
        private string userDataFolder;

        public Form1()
        {
            InitializeComponent();
            ControlActions.SetMainForm(this);  // 设置主窗体引用
            // 设置主窗体属性
            this.Text = "元梦之星农场助手 2.5 GPU启用版+无月卡可用";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ClientSize = new Size(960, 580);
            this.Icon = new Icon("./app.ico");  // 从项目根目录加载图标

            // 初始化所有控件
            InitializeControls();

            // 在窗体加载时初始化 WebView2 控件
            this.Load += Form1_Load;
        }

        private void InitializeControls()
        {
            // 第一行区域：按钮和GPU选项
            int buttonWidth = 100;
            int buttonHeight = 30;
            int spacing = 5;
            int startX = spacing;
            int startY = spacing;

            // "开始/停止"按钮
            btnStartStop = new Button();
            btnStartStop.Text = "开始/停止";
            btnStartStop.Size = new Size(buttonWidth, buttonHeight);
            btnStartStop.Location = new Point(startX, startY);
            btnStartStop.FlatStyle = FlatStyle.Flat;
            btnStartStop.FlatAppearance.BorderSize = 0;
            btnStartStop.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnStartStop.Click += ControlActions.BtnStartStop_Click;
            this.Controls.Add(btnStartStop);

            // "刷新"按钮
            btnRefresh = new Button();
            btnRefresh.Text = "刷新";
            btnRefresh.Size = new Size(buttonWidth, buttonHeight);
            btnRefresh.Location = new Point(startX + (buttonWidth + spacing) * 1, startY);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnRefresh.Click += ControlActions.BtnRefresh_Click;
            this.Controls.Add(btnRefresh);

            // "清理缓存"按钮 (原"版本更新"按钮)
            btnClearCache = new Button();
            btnClearCache.Text = "清理缓存";
            btnClearCache.Size = new Size(buttonWidth, buttonHeight);
            btnClearCache.Location = new Point(startX + (buttonWidth + spacing) * 2, startY);
            btnClearCache.FlatStyle = FlatStyle.Flat;
            btnClearCache.FlatAppearance.BorderSize = 0;
            btnClearCache.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnClearCache.Click += ControlActions.BtnClearCache_Click;
            this.Controls.Add(btnClearCache);

            // "偷菜"按钮
            btnFarmGuide = new Button();
            btnFarmGuide.Text = "偷菜";
            btnFarmGuide.Size = new Size(buttonWidth, buttonHeight);
            btnFarmGuide.Location = new Point(startX + (buttonWidth + spacing) * 3, startY);
            btnFarmGuide.FlatStyle = FlatStyle.Flat;
            btnFarmGuide.FlatAppearance.BorderSize = 0;
            btnFarmGuide.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnFarmGuide.Click += ControlActions.BtnStealing_Click;
            this.Controls.Add(btnFarmGuide);

            // "定时任务"按钮
            btnScheduledTasks = new Button();
            btnScheduledTasks.Text = "定时任务";
            btnScheduledTasks.Size = new Size(buttonWidth, buttonHeight);
            btnScheduledTasks.Location = new Point(startX + (buttonWidth + spacing) * 4, startY);
            btnScheduledTasks.FlatStyle = FlatStyle.Flat;
            btnScheduledTasks.FlatAppearance.BorderSize = 0;
            btnScheduledTasks.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnScheduledTasks.Click += new System.EventHandler(ControlActions.BtnScheduledTasks_Click);
            this.Controls.Add(btnScheduledTasks);

            // "Github"按钮
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
                    MessageBox.Show("无法打开链接: " + ex.Message);
                }
            };
            this.Controls.Add(btnGithub);

            // "必读"按钮
            Button btnMustRead = new Button();
            btnMustRead.Text = "必读";
            btnMustRead.Size = new Size(buttonWidth, buttonHeight);
            btnMustRead.Location = new Point(startX + (buttonWidth + spacing) * 6, startY);
            btnMustRead.FlatStyle = FlatStyle.Flat;
            btnMustRead.FlatAppearance.BorderSize = 0;
            btnMustRead.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnMustRead.Click += ControlActions.BtnMustRead_Click;
            this.Controls.Add(btnMustRead);

            // 最右边添加 GPU 启用选项和无月卡版本选项
            checkBoxGPU = new CheckBox();
            checkBoxGPU.Text = "启用GPU";
            checkBoxGPU.Checked = true; // 固定为启用GPU
            checkBoxGPU.Enabled = false; // 禁用用户操作，不可更改
            checkBoxGPU.Size = new Size(100, buttonHeight);
            // 放置在倒数第二个位置
            checkBoxGPU.Location = new Point(this.ClientSize.Width - 200 - spacing, startY);
            this.Controls.Add(checkBoxGPU);

            // 添加无月卡版本复选框
            checkBoxNoMonthlyCard = new CheckBox();
            checkBoxNoMonthlyCard.Text = "无月卡版本";
            checkBoxNoMonthlyCard.Checked = false; // 默认不启用
            checkBoxNoMonthlyCard.Size = new Size(100, buttonHeight);
            // 放置在最右边
            checkBoxNoMonthlyCard.Location = new Point(this.ClientSize.Width - 100 - spacing, startY);
            this.Controls.Add(checkBoxNoMonthlyCard);

            // 第二行区域：WebView2 控件，无边框，位置紧贴第一行下方
            webView2 = new WebView2();
            webView2.Location = new Point(0, buttonHeight + 2 * spacing);
            webView2.Size = new Size(960, 540);
            this.Controls.Add(webView2);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await InitializeWebView2();
        }

        // 根据 GPU 选项初始化 WebView2 控件，并支持多开
        public async Task InitializeWebView2()
        {
            try
            {
                // 创建一个基于当前进程ID和时间戳的唯一用户数据文件夹
                string baseFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "YmzxHelper", "UserData");
                
                // 确保基础目录存在
                if (!Directory.Exists(baseFolder))
                {
                    Directory.CreateDirectory(baseFolder);
                }
                
                // 创建基于进程ID和时间戳的唯一文件夹名
                int processId = Process.GetCurrentProcess().Id;
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                userDataFolder = Path.Combine(baseFolder, $"Instance_{processId}_{timestamp}");
                
                // 确保用户数据目录存在
                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }
                
                // 配置WebView2环境选项
                var options = new CoreWebView2EnvironmentOptions();
                if (!checkBoxGPU.Checked)
                {
                    options.AdditionalBrowserArguments = "--disable-gpu";
                }
                else
                {
                    // options.AdditionalBrowserArguments = "--enable-gpu";
                    /*使用高性能GPU*/
                    options.AdditionalBrowserArguments = "--use-angle=d3d11 --gpu-preference=high-performance";
                }
                
                // 创建WebView2环境，指定唯一的用户数据目录
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                await webView2.EnsureCoreWebView2Async(env);
                
                // 获取系统DPI缩放比例
                float dpiScale;
                using (var graphics = this.CreateGraphics())
                {
                    dpiScale = graphics.DpiX / 96.0f; // 标准DPI是96
                }
                
                // 设置WebView2的缩放因子以适应DPI缩放
                if (Math.Abs(dpiScale - 1.0f) > 0.01f)
                {
                    // 仅当系统DPI缩放不是1.0时才调整ZoomFactor
                    webView2.CoreWebView2.Settings.IsZoomControlEnabled = false; // 禁用用户缩放控制
                    
                    // 等待一段时间确保网页完全加载
                    await Task.Delay(500);
                    
                    // 设置缩放比例与系统DPI相匹配
                    webView2.ZoomFactor = 1.0 / dpiScale;
                    
                    Console.WriteLine($"设置WebView2缩放比例为: {1.0 / dpiScale} (系统DPI缩放: {dpiScale})");
                }
                
                // 加载指定网页
                webView2.CoreWebView2.Navigate("https://gamer.qq.com/v2/game/96897?ichannel=pcgames0Fpcgames1");
                
                // 添加表单关闭时的清理代码
                this.FormClosed += (s, e) => {
                    try {
                        // 尝试清理用户数据目录
                        if (Directory.Exists(userDataFolder))
                        {
                            // 可选: 清理用户数据目录（如果需要完全删除数据）
                            // Directory.Delete(userDataFolder, true);
                        }
                    }
                    catch (Exception ex) {
                        // 忽略清理过程中的错误
                        Debug.WriteLine($"清理用户数据目录时出错: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 初始化失败: " + ex.Message);
            }
        }
    }
}
