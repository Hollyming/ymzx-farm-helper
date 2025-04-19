using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        private Button btnAccountManager; // 改为账号管理按钮
        private CheckBox checkBoxGPU;
        internal CheckBox checkBoxNoMonthlyCard;
        internal WebView2 webView2;
        
        // 记录当前实例的用户数据目录
        internal string CurrentUserDataFolder { get; private set; }

        // 保存账号相关属性
        internal string AccountToSave { get; set; }
        internal bool NeedSaveAccount { get; set; }

        // 添加当前账号名称属性
        internal string CurrentAccountName { get; set; } = "";

        public Form1()
        {
            // 设置DPI缩放模式
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            
            InitializeComponent();
            ControlActions.SetMainForm(this);  // 设置主窗体引用
            // 设置主窗体属性
            this.Text = "ymzxhelper 4.3";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ClientSize = new Size(960, 580);
            this.Icon = new Icon("./app.ico");  // 从项目根目录加载图标

            // 初始化所有控件
            InitializeControls();

            // 在窗体加载时初始化 WebView2 控件
            this.Load += Form1_Load;

            // 添加窗体关闭事件处理
            this.FormClosed += Form1_FormClosed;

            // 添加GPU选项变更事件处理
            checkBoxGPU.CheckedChanged += async (s, e) => {
                try {
                    // 保存当前URL
                    string currentUrl = webView2.Source?.ToString();
                    
                    // 从控件集合中移除当前的WebView2
                    this.Controls.Remove(webView2);
                    
                    // 释放当前的WebView2实例
                    if (webView2 != null) {
                        webView2.Dispose();
                        webView2 = null;
                    }
                    
                    // 创建新的WebView2实例
                    webView2 = new WebView2();
                    webView2.Location = new Point(0, btnStartStop.Height + 10);
                    webView2.Size = new Size(960, 540);
                    this.Controls.Add(webView2);
                    
                    // 重新初始化WebView2
                    await InitializeWebView2();
                    
                    // 重新加载之前的URL
                    if (!string.IsNullOrEmpty(currentUrl)) {
                        webView2.CoreWebView2.Navigate(currentUrl);
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show($"更改GPU设置时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private void InitializeControls()
        {
            // 获取系统DPI缩放比例
            float dpiScale;
            using (var graphics = this.CreateGraphics())
            {
                dpiScale = graphics.DpiX / 96.0f;
            }

            // 根据DPI缩放调整控件大小和位置
            int buttonWidth = 100; // 固定按钮宽度
            int buttonHeight = 30; // 固定按钮高度
            int spacing = 5; // 固定间距
            int startX = spacing;
            int startY = spacing;

            // 创建缩小的字体
            float baseFontSize = 9f; // 基础字体大小
            float scaledFontSize = baseFontSize / (dpiScale > 1.0f ? dpiScale : 1.0f);
            Font scaledFont = new Font("Microsoft YaHei UI", scaledFontSize, FontStyle.Regular);

            // "开始/停止"按钮
            btnStartStop = new Button();
            btnStartStop.Text = "开始/停止";
            btnStartStop.Size = new Size(buttonWidth, buttonHeight);
            btnStartStop.Location = new Point(startX, startY);
            btnStartStop.Font = scaledFont;
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
            btnRefresh.Font = scaledFont;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnRefresh.Click += ControlActions.BtnRefresh_Click;
            this.Controls.Add(btnRefresh);

            // "清理缓存"按钮
            btnClearCache = new Button();
            btnClearCache.Text = "清理缓存";
            btnClearCache.Size = new Size(buttonWidth, buttonHeight);
            btnClearCache.Location = new Point(startX + (buttonWidth + spacing) * 2, startY);
            btnClearCache.Font = scaledFont;
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
            btnFarmGuide.Font = scaledFont;
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
            btnScheduledTasks.Font = scaledFont;
            btnScheduledTasks.FlatStyle = FlatStyle.Flat;
            btnScheduledTasks.FlatAppearance.BorderSize = 0;
            btnScheduledTasks.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnScheduledTasks.Click += new System.EventHandler(ControlActions.BtnScheduledTasks_Click);
            this.Controls.Add(btnScheduledTasks);

            // "一键设置"按钮
            btnGithub = new Button();
            btnGithub.Text = "一键设置";
            btnGithub.Size = new Size(buttonWidth, buttonHeight);
            btnGithub.Location = new Point(startX + (buttonWidth + spacing) * 5, startY);
            btnGithub.Font = scaledFont;
            btnGithub.FlatStyle = FlatStyle.Flat;
            btnGithub.FlatAppearance.BorderSize = 0;
            btnGithub.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnGithub.Click += ControlActions.BtnOneClickSettings_Click;
            this.Controls.Add(btnGithub);

            // "账号管理"按钮
            btnAccountManager = new Button();
            btnAccountManager.Text = "账号管理";
            btnAccountManager.Size = new Size(buttonWidth, buttonHeight);
            btnAccountManager.Location = new Point(startX + (buttonWidth + spacing) * 6, startY);
            btnAccountManager.Font = scaledFont;
            btnAccountManager.FlatStyle = FlatStyle.Flat;
            btnAccountManager.FlatAppearance.BorderSize = 0;
            btnAccountManager.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnAccountManager.Click += BtnAccountManager_Click;
            this.Controls.Add(btnAccountManager);

            // 计算新的位置
            int rightEdge = this.ClientSize.Width - spacing;

            // 最右边添加 GPU 启用选项和无月卡版本选项
            checkBoxGPU = new CheckBox();
            checkBoxGPU.Text = "启用GPU";
            checkBoxGPU.Checked = true;// 固定为启用GPU
            checkBoxGPU.Enabled = true;// 允许用户更改GPU设置
            checkBoxGPU.Size = new Size(80, buttonHeight); // 减小勾选框宽度
            checkBoxGPU.Font = scaledFont;
            checkBoxGPU.AutoSize = true; // 启用自动大小
            checkBoxGPU.Location = new Point(rightEdge - 160, startY + (buttonHeight - checkBoxGPU.Height) / 2); // 垂直居中
            this.Controls.Add(checkBoxGPU);

            // 添加无月卡版本复选框
            checkBoxNoMonthlyCard = new CheckBox();
            checkBoxNoMonthlyCard.Text = "无月卡版本";
            checkBoxNoMonthlyCard.Checked = false;// 默认不启用
            checkBoxNoMonthlyCard.Size = new Size(80, buttonHeight); // 减小勾选框宽度
            checkBoxNoMonthlyCard.Font = scaledFont;
            checkBoxNoMonthlyCard.AutoSize = true; // 启用自动大小
            checkBoxNoMonthlyCard.Location = new Point(rightEdge - 80, startY + (buttonHeight - checkBoxNoMonthlyCard.Height) / 2); // 垂直居中
            this.Controls.Add(checkBoxNoMonthlyCard);

            // WebView2 控件
            webView2 = new WebView2();
            webView2.Location = new Point(0, buttonHeight + 2 * spacing);
            int webViewHeight = this.ClientSize.Height - (buttonHeight + 2 * spacing);
            webView2.Size = new Size(this.ClientSize.Width, webViewHeight);
            this.Controls.Add(webView2);
        }

        private void BtnAccountManager_Click(object sender, EventArgs e)
        {
            using (var form = new AccountManagerForm(this))
            {
                form.ShowDialog();
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // 创建临时用户数据目录
            string baseFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "WebView2Data"
            );
            
            // 确保基础目录存在
            Directory.CreateDirectory(baseFolder);
            
            // 创建基于进程ID和时间戳的唯一文件夹名
            int processId = Process.GetCurrentProcess().Id;
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            CurrentUserDataFolder = Path.Combine(baseFolder, $"Instance_{processId}_{timestamp}");
            
            await InitializeWebView2();
        }

        // 根据 GPU 选项初始化 WebView2 控件，并支持多开
        public async Task InitializeWebView2()
        {
            try
            {
                // 等待一小段时间确保之前的实例完全释放
                await Task.Delay(100);
                
                // 确保用户数据目录存在
                if (!Directory.Exists(CurrentUserDataFolder))
                {
                    Directory.CreateDirectory(CurrentUserDataFolder);
                }
                
                // 配置WebView2环境选项
                var options = new CoreWebView2EnvironmentOptions();
                if (!checkBoxGPU.Checked)
                {
                    options.AdditionalBrowserArguments = "--disable-gpu";
                }
                else
                {
                    options.AdditionalBrowserArguments = "--use-angle=d3d11 --gpu-preference=high-performance";
                }
                
                // 创建WebView2环境，指定用户数据目录
                var env = await CoreWebView2Environment.CreateAsync(null, CurrentUserDataFolder, options);
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
                    webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    await Task.Delay(500);
                    webView2.ZoomFactor = 1.0 / dpiScale;
                }
                
                // 加载指定网页
                webView2.CoreWebView2.Navigate("https://gamer.qq.com/v2/game/96897");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private async void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                // 如果当前有加载的账号，自动保存配置
                if (!string.IsNullOrEmpty(CurrentAccountName))
                {
                    string accountPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Accounts",
                        CurrentAccountName
                    );
                    
                    Debug.WriteLine($"自动保存账号配置: {CurrentAccountName}");
                    
                    // 创建账号信息
                    var accountInfo = new AccountInfo
                    {
                        Name = CurrentAccountName,
                        WebView2Path = CurrentUserDataFolder,
                        NoMonthlyCard = checkBoxNoMonthlyCard.Checked,
                        LoopTimeSeconds = ControlActions.GetLastLoopTimeSeconds(),
                        ManualSettings = ManualLoopSettingsForm.LoadManualSettings(Process.GetCurrentProcess().Id),
                        ScheduledTasksSettings = new ScheduledTasksSettings
                        {
                            FishTankEnabled = ControlActions.GetScheduledTask("FishTank")?.IsEnabled ?? false,
                            FishTankHour = ControlActions.GetScheduledTask("FishTank")?.ExecutionHour ?? 0,
                            FishTankMinute = ControlActions.GetScheduledTask("FishTank")?.ExecutionMinute ?? 0,
                            
                            FishingEnabled = ControlActions.GetScheduledTask("Fishing")?.IsEnabled ?? false,
                            FishingHour = ControlActions.GetScheduledTask("Fishing")?.ExecutionHour ?? 0,
                            FishingMinute = ControlActions.GetScheduledTask("Fishing")?.ExecutionMinute ?? 0,
                            FishingPlayer = ControlActions.GetScheduledTask("Fishing")?.ExtraParam ?? "",
                            
                            HotSpringEnabled = ControlActions.GetScheduledTask("HotSpring")?.IsEnabled ?? false,
                            HotSpringHour = ControlActions.GetScheduledTask("HotSpring")?.ExecutionHour ?? 0,
                            HotSpringMinute = ControlActions.GetScheduledTask("HotSpring")?.ExecutionMinute ?? 0,
                            HotSpringPlayer = ControlActions.GetScheduledTask("HotSpring")?.ExtraParam ?? ""
                        }
                    };

                    // 确保账号目录存在
                    if (!Directory.Exists(accountPath))
                    {
                        Directory.CreateDirectory(accountPath);
                        Debug.WriteLine($"创建账号目录: {accountPath}");
                    }

                    // 保存账号信息
                    string accountInfoPath = Path.Combine(accountPath, "account_info.json");
                    string accountInfoJson = JsonSerializer.Serialize(accountInfo, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(accountInfoPath, accountInfoJson);
                    Debug.WriteLine($"保存账号信息到: {accountInfoPath}");

                    // 保存 cookies 和 localStorage
                    if (webView2 != null && webView2.CoreWebView2 != null)
                    {
                        try
                        {
                            // 使用 Task.WhenAll 并行执行异步操作
                            var cookiesTask = webView2.CoreWebView2.CookieManager.GetCookiesAsync("https://gamer.qq.com");
                            var localStorageTask = webView2.CoreWebView2.ExecuteScriptAsync("JSON.stringify(Object.entries(localStorage))");
                            
                            await Task.WhenAll(cookiesTask, localStorageTask);
                            
                            // 保存 cookies
                            var cookies = cookiesTask.Result;
                            var cookiesJson = JsonSerializer.Serialize(cookies.Select(c => new
                            {
                                c.Name,
                                c.Value,
                                c.Domain,
                                c.Path,
                                IsSecure = c.IsSecure,
                                IsHttpOnly = c.IsHttpOnly,
                                c.SameSite,
                                Expires = c.Expires.ToString("O")
                            }));
                            File.WriteAllText(Path.Combine(accountPath, "cookies.json"), cookiesJson);

                            // 保存 localStorage
                            var localStorageJson = localStorageTask.Result;
                            File.WriteAllText(Path.Combine(accountPath, "localStorage.json"), localStorageJson);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"保存 cookies 或 localStorage 时出错: {ex.Message}");
                            // 继续执行，不中断保存流程
                        }
                    }

                    // 复制用户数据目录
                    if (!string.IsNullOrEmpty(CurrentUserDataFolder))
                    {
                        try
                        {
                            var userDataPath = Path.Combine(accountPath, "UserData");
                            if (Directory.Exists(userDataPath))
                            {
                                Directory.Delete(userDataPath, true);
                            }
                            CopyDirectory(CurrentUserDataFolder, userDataPath);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"复制用户数据目录时出错: {ex.Message}");
                            // 继续执行，不中断保存流程
                        }
                    }

                    Debug.WriteLine($"账号 {CurrentAccountName} 的配置已自动保存");
                }

                // 读取已保存的账号列表
                string accountsConfigPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Accounts",
                    "accounts.json"
                );

                var savedPaths = new HashSet<string>();
                if (File.Exists(accountsConfigPath))
                {
                    string json = File.ReadAllText(accountsConfigPath);
                    var accounts = JsonSerializer.Deserialize<List<AccountInfo>>(json);
                    if (accounts != null)
                    {
                        foreach (var account in accounts)
                        {
                            savedPaths.Add(account.WebView2Path);
                        }
                    }
                }

                // 获取WebView2Data目录
                string webView2BaseDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "WebView2Data"
                );

                if (Directory.Exists(webView2BaseDir))
                {
                    foreach (string dir in Directory.GetDirectories(webView2BaseDir))
                    {
                        // 如果目录不在已保存的账号列表中，则删除
                        if (!savedPaths.Contains(dir))
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                // 忽略删除失败的错误，通常是因为文件被占用
                                Debug.WriteLine($"清理临时目录失败: {ex.Message}");
                            }
                        }
                    }
                }

                // 关闭 WebView2
                if (webView2 != null)
                {
                    try
                    {
                        webView2.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"关闭 WebView2 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理资源时出错: {ex.Message}");
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            // 创建目标目录
            Directory.CreateDirectory(destinationDir);

            // 复制文件
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                try
                {
                    string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
                catch (IOException)
                {
                    // 忽略被锁定文件的复制错误
                    continue;
                }
            }

            // 递归复制子目录
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                try
                {
                    string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                    CopyDirectory(dir, destDir);
                }
                catch (IOException)
                {
                    // 忽略被锁定目录的复制错误
                    continue;
                }
            }
        }

        // 切换用户数据目录的方法
        internal async Task SwitchUserDataFolder(string newUserDataFolder)
        {
            try
            {
                // 保存当前的 WebView2 状态
                var currentUrl = webView2.Source;

                // 关闭当前的 WebView2 实例
                webView2.Dispose();
                await Task.Delay(500); // 等待资源释放

                // 创建新的 WebView2 实例
                webView2 = new WebView2();
                webView2.Location = new Point(0, btnStartStop.Height + 10);
                webView2.Size = new Size(960, 540);
                this.Controls.Add(webView2);

                // 使用新的用户数据目录初始化 WebView2
                var options = new CoreWebView2EnvironmentOptions();
                if (!checkBoxGPU.Checked)
                {
                    options.AdditionalBrowserArguments = "--disable-gpu";
                }
                else
                {
                    options.AdditionalBrowserArguments = "--use-angle=d3d11 --gpu-preference=high-performance";
                }

                // 创建环境前确保目录存在
                Directory.CreateDirectory(newUserDataFolder);

                // 创建新的 WebView2 环境
                var env = await CoreWebView2Environment.CreateAsync(null, newUserDataFolder, options);
                await webView2.EnsureCoreWebView2Async(env);

                // 设置 DPI 缩放
                float dpiScale;
                using (var graphics = this.CreateGraphics())
                {
                    dpiScale = graphics.DpiX / 96.0f;
                }

                if (Math.Abs(dpiScale - 1.0f) > 0.01f)
                {
                    webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    await Task.Delay(500);
                    webView2.ZoomFactor = 1.0 / dpiScale;
                }

                // 更新当前用户数据目录
                CurrentUserDataFolder = newUserDataFolder;

                // 等待页面加载完成并自动应用游戏设置
                var tcs = new TaskCompletionSource<bool>();
                
                // 创建事件处理程序
                EventHandler<CoreWebView2NavigationCompletedEventArgs> navigationCompletedHandler = null;
                navigationCompletedHandler = async (s, e) => 
                {
                    try 
                    {
                        // 移除事件处理程序，确保只执行一次
                        webView2.NavigationCompleted -= navigationCompletedHandler;
                        
                        // 等待一段时间确保页面完全加载
                        await Task.Delay(2000);
                        
                        // 创建取消令牌
                        using (var cts = new CancellationTokenSource())
                        {
                            // 设置超时时间为30秒
                            cts.CancelAfter(TimeSpan.FromSeconds(30));
                            // 应用游戏设置
                            await ControlActions.ApplyGameSettings(webView2, cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("应用游戏设置超时");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"应用游戏设置时出错: {ex.Message}");
                    }
                    finally
                    {
                        tcs.TrySetResult(true);
                    }
                };

                // 添加事件处理程序
                webView2.NavigationCompleted += navigationCompletedHandler;

                // 导航到之前的 URL 或默认 URL
                if (currentUrl != null)
                {
                    webView2.CoreWebView2.Navigate(currentUrl.ToString());
                }
                else
                {
                    webView2.CoreWebView2.Navigate("https://gamer.qq.com/v2/game/96897");
                }

                // 等待导航和设置完成
                await tcs.Task;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换用户数据目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
    }
}
