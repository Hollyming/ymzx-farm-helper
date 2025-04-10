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
            this.mainForm = mainForm;
            this.accountInfos = new Dictionary<string, AccountInfo>();
            InitializeComponents();
            LoadAccountsList();
        }

        private void InitializeComponents()
        {
            this.Text = "账号管理";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // 账号列表
            accountList = new ListBox();
            accountList.Location = new Point(10, 10);
            accountList.Size = new Size(150, 200);
            this.Controls.Add(accountList);

            // 账号名称输入框
            accountNameInput = new TextBox();
            accountNameInput.Location = new Point(170, 10);
            accountNameInput.Size = new Size(200, 25);
            accountNameInput.PlaceholderText = "请输入账号别名";
            this.Controls.Add(accountNameInput);

            // 保存账号按钮
            btnSaveAccount = new Button();
            btnSaveAccount.Text = "保存账号";
            btnSaveAccount.Location = new Point(170, 45);
            btnSaveAccount.Size = new Size(200, 30);
            btnSaveAccount.Click += BtnSaveAccount_Click;
            this.Controls.Add(btnSaveAccount);

            // 加载账号按钮
            btnLoadAccount = new Button();
            btnLoadAccount.Text = "加载账号";
            btnLoadAccount.Location = new Point(170, 85);
            btnLoadAccount.Size = new Size(200, 30);
            btnLoadAccount.Click += BtnLoadAccount_Click;
            this.Controls.Add(btnLoadAccount);

            // 删除账号按钮
            btnDeleteAccount = new Button();
            btnDeleteAccount.Text = "删除账号";
            btnDeleteAccount.Location = new Point(170, 125);
            btnDeleteAccount.Size = new Size(200, 30);
            btnDeleteAccount.Click += BtnDeleteAccount_Click;
            this.Controls.Add(btnDeleteAccount);

            // 当前账号信息标签
            lblCurrentAccount = new Label();
            lblCurrentAccount.Location = new Point(10, 220);
            lblCurrentAccount.Size = new Size(360, 40);
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
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var accounts = JsonSerializer.Deserialize<List<AccountInfo>>(json);
                    foreach (var account in accounts)
                    {
                        // 只添加WebView2实例目录仍然存在的账号
                        if (Directory.Exists(account.WebView2Path))
                        {
                            accountInfos[account.Name] = account;
                            accountList.Items.Add(account.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载账号配置失败: {ex.Message}");
                }
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存账号配置失败: {ex.Message}");
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
                    WebView2Path = mainForm.CurrentUserDataFolder
                };

                // 更新账号信息
                accountInfos[accountName] = accountInfo;
                SaveAccountsList();

                // 刷新列表
                LoadAccountsList();
                MessageBox.Show("账号保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
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
                if (!accountInfos.TryGetValue(accountName, out var accountInfo))
                {
                    MessageBox.Show("账号信息不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!Directory.Exists(accountInfo.WebView2Path))
                {
                    MessageBox.Show("账号数据不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // 从列表中移除无效账号
                    accountInfos.Remove(accountName);
                    SaveAccountsList();
                    LoadAccountsList();
                    return;
                }

                // 切换到保存的用户数据目录
                await mainForm.SwitchUserDataFolder(accountInfo.WebView2Path);

                lblCurrentAccount.Text = $"当前账号：{accountName}";
                MessageBox.Show("账号加载成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
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