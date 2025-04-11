using System;
using System.Windows.Forms;

namespace ymzx
{
    internal partial class ScheduledTasksForm : Form
    {
        private CheckBox checkBoxFishTank;
        private NumericUpDown numericUpDownHour;
        private NumericUpDown numericUpDownMinute;
        private Button btnConfirm;
        private Label labelHour;
        private Label labelMinute;
        
        // 新增控件
        private CheckBox checkBoxFishing;
        private NumericUpDown numericUpDownFishingHour;
        private NumericUpDown numericUpDownFishingMinute;
        private Label labelFishingHour;
        private Label labelFishingMinute;
        private Label labelFishingPlayer;
        private TextBox textBoxFishingPlayer;
        
        private CheckBox checkBoxHotSpring;
        private NumericUpDown numericUpDownHotSpringHour;
        private NumericUpDown numericUpDownHotSpringMinute;
        private Label labelHotSpringHour;
        private Label labelHotSpringMinute;
        private Label labelHotSpringPlayer;
        private TextBox textBoxHotSpringPlayer;

        public ScheduledTasksForm()
        {
            // 设置DPI缩放模式
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            // 获取系统DPI缩放比例
            float dpiScale;
            using (var graphics = this.CreateGraphics())
            {
                dpiScale = graphics.DpiX / 96.0f;
            }

            // 根据DPI缩放调整基础尺寸
            int baseSpacing = (int)(8 * dpiScale);
            int baseControlHeight = (int)(25 * dpiScale);
            int baseControlWidth = (int)(120 * dpiScale);
            int timeControlWidth = (int)(60 * dpiScale);

            this.Text = "定时任务设置";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(10);

            int currentY = baseSpacing;

            // 鱼缸收获任务复选框
            checkBoxFishTank = new CheckBox();
            checkBoxFishTank.Text = "启用鱼缸收获";
            checkBoxFishTank.Location = new System.Drawing.Point(baseSpacing, currentY);
            checkBoxFishTank.Size = new System.Drawing.Size(baseControlWidth * 2, baseControlHeight);
            checkBoxFishTank.CheckedChanged += CheckBoxFishTank_CheckedChanged;
            this.Controls.Add(checkBoxFishTank);

            currentY += baseControlHeight + baseSpacing;

            // 时间设置面板
            FlowLayoutPanel timePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new System.Drawing.Point(baseSpacing * 3, currentY),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            // 小时设置
            labelHour = new Label
            {
                Text = "时",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            numericUpDownHour = new NumericUpDown
            {
                Size = new System.Drawing.Size(timeControlWidth, baseControlHeight),
                Minimum = 0,
                Maximum = 23,
                Margin = new Padding(0, 0, baseSpacing * 2, 0)
            };

            // 分钟设置
            labelMinute = new Label
            {
                Text = "分",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            numericUpDownMinute = new NumericUpDown
            {
                Size = new System.Drawing.Size(timeControlWidth, baseControlHeight),
                Minimum = 0,
                Maximum = 59
            };

            timePanel.Controls.AddRange(new Control[] { labelHour, numericUpDownHour, labelMinute, numericUpDownMinute });
            this.Controls.Add(timePanel);

            currentY += baseControlHeight + baseSpacing * 2;

            // 钓鱼任务设置
            checkBoxFishing = new CheckBox();
            checkBoxFishing.Text = "启用定时偷鱼";
            checkBoxFishing.Location = new System.Drawing.Point(baseSpacing, currentY);
            checkBoxFishing.Size = new System.Drawing.Size(baseControlWidth * 2, baseControlHeight);
            checkBoxFishing.CheckedChanged += CheckBoxFishing_CheckedChanged;
            this.Controls.Add(checkBoxFishing);

            currentY += baseControlHeight + baseSpacing;

            // 钓鱼时间设置面板
            FlowLayoutPanel fishingTimePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new System.Drawing.Point(baseSpacing * 3, currentY),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            labelFishingHour = new Label
            {
                Text = "时",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            numericUpDownFishingHour = new NumericUpDown
            {
                Size = new System.Drawing.Size(timeControlWidth, baseControlHeight),
                Minimum = 0,
                Maximum = 23,
                Margin = new Padding(0, 0, baseSpacing * 2, 0)
            };

            labelFishingMinute = new Label
            {
                Text = "分",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            numericUpDownFishingMinute = new NumericUpDown
            {
                Size = new System.Drawing.Size(timeControlWidth, baseControlHeight),
                Minimum = 0,
                Maximum = 59
            };

            fishingTimePanel.Controls.AddRange(new Control[] { labelFishingHour, numericUpDownFishingHour, labelFishingMinute, numericUpDownFishingMinute });
            this.Controls.Add(fishingTimePanel);

            currentY += baseControlHeight + baseSpacing;

            // 钓鱼玩家设置
            FlowLayoutPanel fishingPlayerPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new System.Drawing.Point(baseSpacing * 3, currentY),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            labelFishingPlayer = new Label
            {
                Text = "玩家UID/昵称:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            textBoxFishingPlayer = new TextBox
            {
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight)
            };

            fishingPlayerPanel.Controls.AddRange(new Control[] { labelFishingPlayer, textBoxFishingPlayer });
            this.Controls.Add(fishingPlayerPanel);

            currentY += baseControlHeight + baseSpacing * 2;

            // 泡温泉任务设置
            checkBoxHotSpring = new CheckBox();
            checkBoxHotSpring.Text = "启用泡温泉";
            checkBoxHotSpring.Location = new System.Drawing.Point(baseSpacing, currentY);
            checkBoxHotSpring.Size = new System.Drawing.Size(baseControlWidth * 2, baseControlHeight);
            checkBoxHotSpring.CheckedChanged += CheckBoxHotSpring_CheckedChanged;
            this.Controls.Add(checkBoxHotSpring);

            currentY += baseControlHeight + baseSpacing;

            // 温泉时间设置面板
            FlowLayoutPanel hotSpringTimePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new System.Drawing.Point(baseSpacing * 3, currentY),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            labelHotSpringHour = new Label
            {
                Text = "时",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            numericUpDownHotSpringHour = new NumericUpDown
            {
                Size = new System.Drawing.Size(timeControlWidth, baseControlHeight),
                Minimum = 0,
                Maximum = 23,
                Margin = new Padding(0, 0, baseSpacing * 2, 0)
            };

            labelHotSpringMinute = new Label
            {
                Text = "分",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            numericUpDownHotSpringMinute = new NumericUpDown
            {
                Size = new System.Drawing.Size(timeControlWidth, baseControlHeight),
                Minimum = 0,
                Maximum = 59
            };

            hotSpringTimePanel.Controls.AddRange(new Control[] { labelHotSpringHour, numericUpDownHotSpringHour, labelHotSpringMinute, numericUpDownHotSpringMinute });
            this.Controls.Add(hotSpringTimePanel);

            currentY += baseControlHeight + baseSpacing;

            // 温泉玩家设置
            FlowLayoutPanel hotSpringPlayerPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new System.Drawing.Point(baseSpacing * 3, currentY),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            labelHotSpringPlayer = new Label
            {
                Text = "玩家UID/昵称:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, baseSpacing, 0)
            };

            textBoxHotSpringPlayer = new TextBox
            {
                Size = new System.Drawing.Size(baseControlWidth, baseControlHeight)
            };

            hotSpringPlayerPanel.Controls.AddRange(new Control[] { labelHotSpringPlayer, textBoxHotSpringPlayer });
            this.Controls.Add(hotSpringPlayerPanel);

            currentY += baseControlHeight + baseSpacing * 2;

            // 确认按钮
            btnConfirm = new Button();
            btnConfirm.Text = "确认";
            btnConfirm.Size = new System.Drawing.Size(baseControlWidth, baseControlHeight);
            btnConfirm.Location = new System.Drawing.Point(baseSpacing, currentY);
            btnConfirm.Click += BtnConfirm_Click;
            this.Controls.Add(btnConfirm);
        }

        private void LoadCurrentSettings()
        {
            var fishTankTask = ControlActions.GetScheduledTask("FishTank");
            if (fishTankTask != null)
            {
                checkBoxFishTank.Checked = fishTankTask.IsEnabled;
                numericUpDownHour.Value = fishTankTask.ExecutionHour;
                numericUpDownMinute.Value = fishTankTask.ExecutionMinute;
            }
            
            var fishingTask = ControlActions.GetScheduledTask("Fishing");
            if (fishingTask != null)
            {
                checkBoxFishing.Checked = fishingTask.IsEnabled;
                numericUpDownFishingHour.Value = fishingTask.ExecutionHour;
                numericUpDownFishingMinute.Value = fishingTask.ExecutionMinute;
                textBoxFishingPlayer.Text = fishingTask.ExtraParam;
            }
            
            var hotSpringTask = ControlActions.GetScheduledTask("HotSpring");
            if (hotSpringTask != null)
            {
                checkBoxHotSpring.Checked = hotSpringTask.IsEnabled;
                numericUpDownHotSpringHour.Value = hotSpringTask.ExecutionHour;
                numericUpDownHotSpringMinute.Value = hotSpringTask.ExecutionMinute;
                textBoxHotSpringPlayer.Text = hotSpringTask.ExtraParam;
            }

            UpdateControlsState();
        }

        private void CheckBoxFishTank_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateControlsState();
        }
        
        private void CheckBoxFishing_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateControlsState();
        }
        
        private void CheckBoxHotSpring_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateControlsState();
        }

        private void UpdateControlsState()
        {
            // 鱼缸收获控件状态
            bool fishTankEnabled = checkBoxFishTank.Checked;
            numericUpDownHour.Enabled = fishTankEnabled;
            numericUpDownMinute.Enabled = fishTankEnabled;
            labelHour.Enabled = fishTankEnabled;
            labelMinute.Enabled = fishTankEnabled;
            
            // 偷鱼控件状态
            bool fishingEnabled = checkBoxFishing.Checked;
            numericUpDownFishingHour.Enabled = fishingEnabled;
            numericUpDownFishingMinute.Enabled = fishingEnabled;
            labelFishingHour.Enabled = fishingEnabled;
            labelFishingMinute.Enabled = fishingEnabled;
            labelFishingPlayer.Enabled = fishingEnabled;
            textBoxFishingPlayer.Enabled = fishingEnabled;
            
            // 泡温泉控件状态
            bool hotSpringEnabled = checkBoxHotSpring.Checked;
            numericUpDownHotSpringHour.Enabled = hotSpringEnabled;
            numericUpDownHotSpringMinute.Enabled = hotSpringEnabled;
            labelHotSpringHour.Enabled = hotSpringEnabled;
            labelHotSpringMinute.Enabled = hotSpringEnabled;
            labelHotSpringPlayer.Enabled = hotSpringEnabled;
            textBoxHotSpringPlayer.Enabled = hotSpringEnabled;
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            var fishTankTask = ControlActions.GetScheduledTask("FishTank");
            if (fishTankTask != null)
            {
                bool statusChanged = fishTankTask.IsEnabled != checkBoxFishTank.Checked || 
                                     fishTankTask.ExecutionHour != (int)numericUpDownHour.Value ||
                                     fishTankTask.ExecutionMinute != (int)numericUpDownMinute.Value;
                
                fishTankTask.IsEnabled = checkBoxFishTank.Checked;
                fishTankTask.ExecutionHour = (int)numericUpDownHour.Value;
                fishTankTask.ExecutionMinute = (int)numericUpDownMinute.Value;
                
                // 如果设置有变化，重置执行状态
                if (statusChanged && fishTankTask.IsEnabled)
                {
                    ControlActions.ResetTaskExecutionStatus("FishTank");
                }
            }
            
            var fishingTask = ControlActions.GetScheduledTask("Fishing");
            if (fishingTask != null)
            {
                bool statusChanged = fishingTask.IsEnabled != checkBoxFishing.Checked || 
                                     fishingTask.ExecutionHour != (int)numericUpDownFishingHour.Value ||
                                     fishingTask.ExecutionMinute != (int)numericUpDownFishingMinute.Value ||
                                     fishingTask.ExtraParam != textBoxFishingPlayer.Text;
                
                fishingTask.IsEnabled = checkBoxFishing.Checked;
                fishingTask.ExecutionHour = (int)numericUpDownFishingHour.Value;
                fishingTask.ExecutionMinute = (int)numericUpDownFishingMinute.Value;
                fishingTask.ExtraParam = textBoxFishingPlayer.Text;
                
                // 如果设置有变化，重置执行状态
                if (statusChanged && fishingTask.IsEnabled)
                {
                    ControlActions.ResetTaskExecutionStatus("Fishing");
                }
            }
            
            var hotSpringTask = ControlActions.GetScheduledTask("HotSpring");
            if (hotSpringTask != null)
            {
                bool statusChanged = hotSpringTask.IsEnabled != checkBoxHotSpring.Checked || 
                                     hotSpringTask.ExecutionHour != (int)numericUpDownHotSpringHour.Value ||
                                     hotSpringTask.ExecutionMinute != (int)numericUpDownHotSpringMinute.Value ||
                                     hotSpringTask.ExtraParam != textBoxHotSpringPlayer.Text;
                
                hotSpringTask.IsEnabled = checkBoxHotSpring.Checked;
                hotSpringTask.ExecutionHour = (int)numericUpDownHotSpringHour.Value;
                hotSpringTask.ExecutionMinute = (int)numericUpDownHotSpringMinute.Value;
                hotSpringTask.ExtraParam = textBoxHotSpringPlayer.Text;
                
                // 如果设置有变化，重置执行状态
                if (statusChanged && hotSpringTask.IsEnabled)
                {
                    ControlActions.ResetTaskExecutionStatus("HotSpring");
                }
            }

            this.Close();
        }
    }
}