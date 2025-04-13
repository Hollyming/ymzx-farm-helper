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
        private ComboBox comboBoxRestTime;
        private Label labelRestTime;
        private CheckBox checkBoxFarmCar;

        public int FarmRanchTimes { get; private set; } = 2;
        public bool ExecuteWorkshop { get; private set; } = true;
        public bool ExecuteFishing { get; private set; } = false;
        public int FishingCount { get; private set; } = 24;
        public int RestTimeSeconds { get; private set; } = 0;
        public bool UseFarmCar { get; private set; } = false;

        public ManualLoopSettingsForm(int processId)
        {
            settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YmzxHelper",
                $"manual_settings_{processId}.json"
            );
            
            // 设置DPI缩放模式
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "手动循环设置";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(10);

            // 获取系统DPI缩放比例
            float dpiScale;
            using (var graphics = this.CreateGraphics())
            {
                dpiScale = graphics.DpiX / 96.0f;
            }

            // 根据DPI缩放调整控件大小和位置
            int baseSpacing = (int)(8 * dpiScale);
            int baseControlHeight = (int)(25 * dpiScale);
            int baseControlWidth = (int)(120 * dpiScale);
            int currentY = baseSpacing;

            // 农场牧场次数选择
            Label lblFarmRanch = new Label
            {
                Text = "农场+牧场次数:",
                Location = new System.Drawing.Point(baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblFarmRanch);

            comboBoxFarmRanchTimes = new ComboBox
            {
                Location = new System.Drawing.Point(baseSpacing + baseControlWidth + baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxFarmRanchTimes.Items.AddRange(new object[] { "0", "1", "2" });
            comboBoxFarmRanchTimes.SelectedIndex = 2;
            this.Controls.Add(comboBoxFarmRanchTimes);

            currentY += baseControlHeight + baseSpacing;

            // 添加小车选项
            checkBoxFarmCar = new CheckBox
            {
                Text = "65级有小车",
                Location = new System.Drawing.Point(baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth * 2, baseControlHeight),
                Checked = false
            };
            this.Controls.Add(checkBoxFarmCar);

            currentY += baseControlHeight + baseSpacing;

            // 加工坊勾选框
            checkBoxWorkshop = new CheckBox
            {
                Text = "执行加工坊",
                Location = new System.Drawing.Point(baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth * 2, baseControlHeight),
                Checked = true
            };
            this.Controls.Add(checkBoxWorkshop);

            currentY += baseControlHeight + baseSpacing;

            // 钓鱼勾选框
            checkBoxFishing = new CheckBox
            {
                Text = "执行钓鱼",
                Location = new System.Drawing.Point(baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth * 2, baseControlHeight),
                Checked = false
            };
            checkBoxFishing.CheckedChanged += CheckBoxFishing_CheckedChanged;
            this.Controls.Add(checkBoxFishing);

            currentY += baseControlHeight + baseSpacing;

            // 钓鱼次数输入框
            Label lblFishingCount = new Label
            {
                Text = "钓鱼次数:",
                Location = new System.Drawing.Point(baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblFishingCount);

            numericUpDownFishingCount = new NumericUpDown
            {
                Location = new System.Drawing.Point(baseSpacing + baseControlWidth + baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight),
                Minimum = 1,
                Maximum = 100,
                Value = 24,
                Enabled = false
            };
            this.Controls.Add(numericUpDownFishingCount);

            currentY += baseControlHeight + baseSpacing;

            // 休息时长设置
            labelRestTime = new Label
            {
                Text = "休息时长:",
                Location = new System.Drawing.Point(baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(labelRestTime);

            comboBoxRestTime = new ComboBox
            {
                Location = new System.Drawing.Point(baseSpacing + baseControlWidth + baseSpacing, currentY),
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxRestTime.Items.AddRange(new object[] { "不休息", "30秒", "1分钟", "2分钟" });
            comboBoxRestTime.SelectedIndex = 0;
            this.Controls.Add(comboBoxRestTime);

            currentY += baseControlHeight + baseSpacing * 2;

            // 按钮布局
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new System.Drawing.Point(baseSpacing, currentY),
                Padding = new Padding(0)
            };

            // 确定按钮
            btnOK = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight)
            };
            btnOK.Click += BtnOK_Click;
            buttonPanel.Controls.Add(btnOK);

            // 取消按钮
            btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight),
                Margin = new Padding(baseSpacing, 0, 0, 0)
            };
            buttonPanel.Controls.Add(btnCancel);

            this.Controls.Add(buttonPanel);
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
                        checkBoxFarmCar.Checked = settings.UseFarmCar;
                        comboBoxRestTime.SelectedIndex = settings.RestTimeSeconds switch
                        {
                            0 => 0,    // 不休息
                            30 => 1,   // 30秒
                            60 => 2,   // 1分钟
                            120 => 3,  // 2分钟
                            _ => 0     // 默认不休息
                        };
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
                    FishingCount = (int)numericUpDownFishingCount.Value,
                    UseFarmCar = checkBoxFarmCar.Checked,
                    RestTimeSeconds = comboBoxRestTime.SelectedIndex switch
                    {
                        0 => 0,    // 不休息
                        1 => 30,   // 30秒
                        2 => 60,   // 1分钟
                        3 => 120,  // 2分钟
                        _ => 0     // 默认不休息
                    }
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
            UseFarmCar = checkBoxFarmCar.Checked;
            RestTimeSeconds = comboBoxRestTime.SelectedIndex switch
            {
                0 => 0,    // 不休息
                1 => 30,   // 30秒
                2 => 60,   // 1分钟
                3 => 120,  // 2分钟
                _ => 0     // 默认不休息
            };
            
            SaveSettings();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // 添加设置类
        public class ManualSettings
        {
            public int FarmRanchTimes { get; set; }
            public bool ExecuteWorkshop { get; set; }
            public bool ExecuteFishing { get; set; }
            public int FishingCount { get; set; }
            public int RestTimeSeconds { get; set; }
            public bool UseFarmCar { get; set; }
        }
    }
} 