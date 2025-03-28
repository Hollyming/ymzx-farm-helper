using System;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;

namespace ymzx
{
    public class ManualLoopSettingsForm : Form
    {
        private ComboBox comboBoxFarmRanchTimes;
        private CheckBox checkBoxWorkshop;
        private CheckBox checkBoxFishing;
        private NumericUpDown numericUpDownFishingCount;
        private Button btnOK;
        private Button btnCancel;
        private readonly string settingsFilePath;

        public int FarmRanchTimes { get; private set; } = 2;
        public bool ExecuteWorkshop { get; private set; } = true;
        public bool ExecuteFishing { get; private set; } = false;
        public int FishingCount { get; private set; } = 24;

        public ManualLoopSettingsForm(int processId)
        {
            settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YmzxHelper",
                $"manual_settings_{processId}.json"
            );
            
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "手动循环设置";
            this.Size = new System.Drawing.Size(300, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 农场牧场次数选择
            Label lblFarmRanch = new Label
            {
                Text = "农场+牧场次数:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(100, 20),
                AutoSize = true
            };
            this.Controls.Add(lblFarmRanch);

            comboBoxFarmRanchTimes = new ComboBox
            {
                Location = new System.Drawing.Point(120, 20),
                Size = new System.Drawing.Size(100, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxFarmRanchTimes.Items.AddRange(new object[] { "0", "1", "2" });
            comboBoxFarmRanchTimes.SelectedIndex = 2; // 默认选择2次
            this.Controls.Add(comboBoxFarmRanchTimes);

            // 加工坊勾选框
            checkBoxWorkshop = new CheckBox
            {
                Text = "执行加工坊",
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(200, 20),
                Checked = true
            };
            this.Controls.Add(checkBoxWorkshop);

            // 钓鱼勾选框
            checkBoxFishing = new CheckBox
            {
                Text = "执行钓鱼",
                Location = new System.Drawing.Point(20, 90),
                Size = new System.Drawing.Size(200, 20),
                Checked = false
            };
            checkBoxFishing.CheckedChanged += CheckBoxFishing_CheckedChanged;
            this.Controls.Add(checkBoxFishing);

            // 钓鱼次数输入框
            Label lblFishingCount = new Label
            {
                Text = "钓鱼次数:",
                Location = new System.Drawing.Point(20, 120),
                Size = new System.Drawing.Size(100, 20),
                AutoSize = true
            };
            this.Controls.Add(lblFishingCount);

            numericUpDownFishingCount = new NumericUpDown
            {
                Location = new System.Drawing.Point(120, 120),
                Size = new System.Drawing.Size(100, 20),
                Minimum = 1,
                Maximum = 100,
                Value = 24,
                Enabled = false
            };
            this.Controls.Add(numericUpDownFishingCount);

            // 确定按钮
            btnOK = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(100, 160),
                Size = new System.Drawing.Size(80, 30)
            };
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            // 取消按钮
            btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(200, 160),
                Size = new System.Drawing.Size(80, 30)
            };
            this.Controls.Add(btnCancel);
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string jsonString = File.ReadAllText(settingsFilePath);
                    var settings = JsonSerializer.Deserialize<ManualSettings>(jsonString);
                    
                    if (settings != null)
                    {
                        comboBoxFarmRanchTimes.SelectedIndex = settings.FarmRanchTimes;
                        checkBoxWorkshop.Checked = settings.ExecuteWorkshop;
                        checkBoxFishing.Checked = settings.ExecuteFishing;
                        numericUpDownFishingCount.Value = settings.FishingCount;
                        numericUpDownFishingCount.Enabled = settings.ExecuteFishing;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载设置失败: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new ManualSettings
                {
                    FarmRanchTimes = int.Parse(comboBoxFarmRanchTimes.SelectedItem.ToString()),
                    ExecuteWorkshop = checkBoxWorkshop.Checked,
                    ExecuteFishing = checkBoxFishing.Checked,
                    FishingCount = (int)numericUpDownFishingCount.Value
                };

                string directory = Path.GetDirectoryName(settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonString = JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        private void CheckBoxFishing_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownFishingCount.Enabled = checkBoxFishing.Checked;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            FarmRanchTimes = int.Parse(comboBoxFarmRanchTimes.SelectedItem.ToString());
            ExecuteWorkshop = checkBoxWorkshop.Checked;
            ExecuteFishing = checkBoxFishing.Checked;
            FishingCount = (int)numericUpDownFishingCount.Value;
            
            SaveSettings();
        }

        // 添加设置类
        public class ManualSettings
        {
            public int FarmRanchTimes { get; set; }
            public bool ExecuteWorkshop { get; set; }
            public bool ExecuteFishing { get; set; }
            public int FishingCount { get; set; }
        }
    }
} 