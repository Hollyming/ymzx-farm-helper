using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace ymzx
{
    // 账号信息类
    internal class AccountInfo
    {
        public string Name { get; set; }
        public string WebView2Path { get; set; }
        public bool NoMonthlyCard { get; set; }
        public int LoopTimeSeconds { get; set; }
        public ManualLoopSettingsForm.ManualSettings ManualSettings { get; set; }
        public ScheduledTasksSettings ScheduledTasksSettings { get; set; }
    }

    // 添加定时任务设置类
    internal class ScheduledTasksSettings
    {
        public bool FishTankEnabled { get; set; }
        public int FishTankHour { get; set; }
        public int FishTankMinute { get; set; }
        
        public bool FishingEnabled { get; set; }
        public int FishingHour { get; set; }
        public int FishingMinute { get; set; }
        public string FishingPlayer { get; set; }
        
        public bool HotSpringEnabled { get; set; }
        public int HotSpringHour { get; set; }
        public int HotSpringMinute { get; set; }
        public string HotSpringPlayer { get; set; }
    }

    internal class AccountManagerForm : Form
    {
        private ListBox accountList;
        private TextBox accountNameInput;
        private Button btnSaveAccount;
        private Button btnLoadAccount;
        private Button btnDeleteAccount;
        private Label lblCurrentAccount;
        private string accountsDirectory;
        private Form1 mainForm;
        private Dictionary<string, AccountInfo> accountInfos;

        public AccountManagerForm(Form1 mainForm)
        {
            // 设置DPI缩放模式
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            
            this.mainForm = mainForm;
            this.accountInfos = new Dictionary<string, AccountInfo>();
            InitializeComponents();
            LoadAccountsList();
        }

        private void InitializeComponents()
        {
            // 获取系统DPI缩放比例
            float dpiScale;
            using (var graphics = this.CreateGraphics())
            {
                dpiScale = graphics.DpiX / 96.0f;
            }

            // 根据DPI缩放调整控件尺寸
            int baseWidth = (int)(400 * dpiScale);
            int baseHeight = (int)(300 * dpiScale);
            int spacing = (int)(10 * dpiScale);
            int controlHeight = (int)(30 * dpiScale);
            int listWidth = (int)(150 * dpiScale);
            int buttonWidth = (int)(200 * dpiScale);
            int labelHeight = (int)(40 * dpiScale);

            this.Text = "账号管理";
            this.Size = new Size(baseWidth, baseHeight);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // 账号列表
            accountList = new ListBox();
            accountList.Location = new Point(spacing, spacing);
            accountList.Size = new Size(listWidth, (int)(150 * dpiScale));
            this.Controls.Add(accountList);

            // 账号名称输入框
            accountNameInput = new TextBox();
            accountNameInput.Location = new Point(spacing * 2 + listWidth, spacing);
            accountNameInput.Size = new Size(buttonWidth, controlHeight);
            accountNameInput.PlaceholderText = "请输入账号别名";
            this.Controls.Add(accountNameInput);

            // 保存账号按钮
            btnSaveAccount = new Button();
            btnSaveAccount.Text = "保存账号";
            btnSaveAccount.Location = new Point(spacing * 2 + listWidth, spacing * 2 + controlHeight);
            btnSaveAccount.Size = new Size(buttonWidth, controlHeight);
            btnSaveAccount.Click += BtnSaveAccount_Click;
            this.Controls.Add(btnSaveAccount);

            // 加载账号按钮
            btnLoadAccount = new Button();
            btnLoadAccount.Text = "加载账号";
            btnLoadAccount.Location = new Point(spacing * 2 + listWidth, spacing * 3 + controlHeight * 2);
            btnLoadAccount.Size = new Size(buttonWidth, controlHeight);
            btnLoadAccount.Click += BtnLoadAccount_Click;
            this.Controls.Add(btnLoadAccount);

            // 删除账号按钮
            btnDeleteAccount = new Button();
            btnDeleteAccount.Text = "删除账号";
            btnDeleteAccount.Location = new Point(spacing * 2 + listWidth, spacing * 4 + controlHeight * 3);
            btnDeleteAccount.Size = new Size(buttonWidth, controlHeight);
            btnDeleteAccount.Click += BtnDeleteAccount_Click;
            this.Controls.Add(btnDeleteAccount);

            // 当前账号信息标签
            lblCurrentAccount = new Label();
            lblCurrentAccount.Location = new Point(spacing, spacing + (int)(150 * dpiScale) + spacing);
            lblCurrentAccount.Size = new Size(baseWidth - spacing * 2, labelHeight);
            lblCurrentAccount.ForeColor = Color.Blue;
            lblCurrentAccount.Text = "当前未加载任何账号";
            this.Controls.Add(lblCurrentAccount);

            // 设置账号目录为程序所在目录下的 Accounts 文件夹
            accountsDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Accounts"
            );
            Directory.CreateDirectory(accountsDirectory);
        }

        private void LoadAccountsList()
        {
            accountList.Items.Clear();
            accountInfos.Clear();

            string configPath = Path.Combine(accountsDirectory, "accounts.json");
            Debug.WriteLine($"尝试从路径加载账号列表: {configPath}");
            
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    Debug.WriteLine($"读取到的账号列表: {json}");
                    
                    var accounts = JsonSerializer.Deserialize<List<AccountInfo>>(json);
                    foreach (var account in accounts)
                    {
                        // 只添加WebView2实例目录仍然存在的账号
                        if (Directory.Exists(account.WebView2Path))
                        {
                            accountInfos[account.Name] = account;
                            accountList.Items.Add(account.Name);
                        }
                        else
                        {
                            Debug.WriteLine($"账号 {account.Name} 的WebView2目录不存在: {account.WebView2Path}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载账号列表失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Debug.WriteLine("账号列表文件不存在");
            }
        }

        private void SaveAccountsList()
        {
            try
            {
                string configPath = Path.Combine(accountsDirectory, "accounts.json");
                var accounts = accountInfos.Values.ToList();
                string json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                Debug.WriteLine($"保存账号列表到: {configPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存账号列表失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void BtnSaveAccount_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(accountNameInput.Text))
            {
                MessageBox.Show("请输入账号别名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string accountName = accountNameInput.Text.Trim();
                string accountPath = Path.Combine(accountsDirectory, accountName);
                
                Debug.WriteLine($"尝试保存账号到路径: {accountPath}");
                
                // 检查账号是否已存在
                if (accountInfos.ContainsKey(accountName))
                {
                    var result = MessageBox.Show("账号已存在，是否覆盖？", "提示", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                        return;
                }

                // 创建账号信息
                var accountInfo = new AccountInfo
                {
                    Name = accountName,
                    WebView2Path = mainForm.CurrentUserDataFolder,
                    NoMonthlyCard = mainForm.checkBoxNoMonthlyCard.Checked,
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

                // 更新账号信息字典
                accountInfos[accountName] = accountInfo;
                SaveAccountsList();

                // 更新当前账号状态
                mainForm.CurrentAccountName = accountName;
                lblCurrentAccount.Text = $"当前账号：{accountName}";

                // 刷新列表
                LoadAccountsList();
                MessageBox.Show("账号保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存账号时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"保存账号失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnLoadAccount_Click(object sender, EventArgs e)
        {
            if (accountList.SelectedItem == null)
            {
                MessageBox.Show("请选择要加载的账号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string accountName = accountList.SelectedItem.ToString();
                string accountPath = Path.Combine(accountsDirectory, accountName);
                string accountInfoPath = Path.Combine(accountPath, "account_info.json");
                
                Debug.WriteLine($"尝试从路径加载账号: {accountInfoPath}");
                
                if (!Directory.Exists(accountPath))
                {
                    Debug.WriteLine($"账号目录不存在: {accountPath}");
                    MessageBox.Show("账号目录不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (!File.Exists(accountInfoPath))
                {
                    Debug.WriteLine($"账号信息文件不存在: {accountInfoPath}");
                    MessageBox.Show("账号信息不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // 读取账号信息
                string accountInfoJson = File.ReadAllText(accountInfoPath);
                Debug.WriteLine($"读取到的账号信息: {accountInfoJson}");
                
                var accountInfo = JsonSerializer.Deserialize<AccountInfo>(accountInfoJson);
                
                if (accountInfo == null)
                {
                    Debug.WriteLine("账号信息反序列化失败");
                    MessageBox.Show("账号信息格式错误！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // 切换到保存的用户数据目录
                await mainForm.SwitchUserDataFolder(accountInfo.WebView2Path);
                
                // 加载脚本设置
                mainForm.checkBoxNoMonthlyCard.Checked = accountInfo.NoMonthlyCard;
                ControlActions.SetLastLoopTimeSeconds(accountInfo.LoopTimeSeconds);
                
                // 加载手动设置
                if (accountInfo.ManualSettings != null)
                {
                    string settingsFilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "YmzxHelper",
                        $"manual_settings_{Process.GetCurrentProcess().Id}.json"
                    );
                    
                    string settingsJson = JsonSerializer.Serialize(accountInfo.ManualSettings);
                    File.WriteAllText(settingsFilePath, settingsJson);
                    Debug.WriteLine($"保存手动设置到: {settingsFilePath}");
                }
                
                // 加载定时任务设置
                if (accountInfo.ScheduledTasksSettings != null)
                {
                    var settings = accountInfo.ScheduledTasksSettings;
                    
                    var fishTankTask = ControlActions.GetScheduledTask("FishTank");
                    if (fishTankTask != null)
                    {
                        fishTankTask.IsEnabled = settings.FishTankEnabled;
                        fishTankTask.ExecutionHour = settings.FishTankHour;
                        fishTankTask.ExecutionMinute = settings.FishTankMinute;
                    }
                    
                    var fishingTask = ControlActions.GetScheduledTask("Fishing");
                    if (fishingTask != null)
                    {
                        fishingTask.IsEnabled = settings.FishingEnabled;
                        fishingTask.ExecutionHour = settings.FishingHour;
                        fishingTask.ExecutionMinute = settings.FishingMinute;
                        fishingTask.ExtraParam = settings.FishingPlayer;
                    }
                    
                    var hotSpringTask = ControlActions.GetScheduledTask("HotSpring");
                    if (hotSpringTask != null)
                    {
                        hotSpringTask.IsEnabled = settings.HotSpringEnabled;
                        hotSpringTask.ExecutionHour = settings.HotSpringHour;
                        hotSpringTask.ExecutionMinute = settings.HotSpringMinute;
                        hotSpringTask.ExtraParam = settings.HotSpringPlayer;
                    }
                }
                
                // 更新当前账号状态
                mainForm.CurrentAccountName = accountName;
                lblCurrentAccount.Text = $"当前账号：{accountName}";
                MessageBox.Show("账号加载成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载账号时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"加载账号失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteAccount_Click(object sender, EventArgs e)
        {
            if (accountList.SelectedItem == null)
            {
                MessageBox.Show("请选择要删除的账号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string accountName = accountList.SelectedItem.ToString();
                var result = MessageBox.Show($"确定要删除账号 {accountName} 吗？\n注意：这将同时删除账号的所有数据！", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    if (accountInfos.TryGetValue(accountName, out var accountInfo))
                    {
                        // 删除WebView2数据目录
                        if (Directory.Exists(accountInfo.WebView2Path))
                        {
                            Directory.Delete(accountInfo.WebView2Path, true);
                        }

                        // 从配置中移除账号
                        accountInfos.Remove(accountName);
                        SaveAccountsList();
                        LoadAccountsList();
                        MessageBox.Show("账号删除成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除账号失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 