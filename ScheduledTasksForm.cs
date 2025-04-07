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
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "定时任务设置";
            this.Size = new System.Drawing.Size(350, 400); // 调整整体窗口宽度
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // 鱼缸收获任务复选框
            checkBoxFishTank = new CheckBox();
            checkBoxFishTank.Text = "启用鱼缸收获";
            checkBoxFishTank.Location = new System.Drawing.Point(20, 20);
            checkBoxFishTank.Size = new System.Drawing.Size(150, 20);
            checkBoxFishTank.CheckedChanged += CheckBoxFishTank_CheckedChanged;

            // 小时标签
            labelHour = new Label();
            labelHour.Text = "时";
            labelHour.Location = new System.Drawing.Point(20, 50);
            labelHour.Size = new System.Drawing.Size(30, 20);

            // 小时输入框
            numericUpDownHour = new NumericUpDown();
            numericUpDownHour.Location = new System.Drawing.Point(50, 50);
            numericUpDownHour.Size = new System.Drawing.Size(50, 20);
            numericUpDownHour.Minimum = 0;
            numericUpDownHour.Maximum = 23;

            // 分钟标签
            labelMinute = new Label();
            labelMinute.Text = "分";
            labelMinute.Location = new System.Drawing.Point(120, 50);
            labelMinute.Size = new System.Drawing.Size(30, 20);

            // 分钟输入框
            numericUpDownMinute = new NumericUpDown();
            numericUpDownMinute.Location = new System.Drawing.Point(150, 50);
            numericUpDownMinute.Size = new System.Drawing.Size(50, 20);
            numericUpDownMinute.Minimum = 0;
            numericUpDownMinute.Maximum = 59;
            
            // 钓鱼任务复选框
            checkBoxFishing = new CheckBox();
            checkBoxFishing.Text = "启用定时偷鱼";
            checkBoxFishing.Location = new System.Drawing.Point(20, 100);
            checkBoxFishing.Size = new System.Drawing.Size(150, 20);
            checkBoxFishing.CheckedChanged += CheckBoxFishing_CheckedChanged;

            // 钓鱼小时标签
            labelFishingHour = new Label();
            labelFishingHour.Text = "时";
            labelFishingHour.Location = new System.Drawing.Point(20, 130);
            labelFishingHour.Size = new System.Drawing.Size(30, 20);

            // 钓鱼小时输入框
            numericUpDownFishingHour = new NumericUpDown();
            numericUpDownFishingHour.Location = new System.Drawing.Point(50, 130);
            numericUpDownFishingHour.Size = new System.Drawing.Size(50, 20);
            numericUpDownFishingHour.Minimum = 0;
            numericUpDownFishingHour.Maximum = 23;

            // 钓鱼分钟标签
            labelFishingMinute = new Label();
            labelFishingMinute.Text = "分";
            labelFishingMinute.Location = new System.Drawing.Point(120, 130);
            labelFishingMinute.Size = new System.Drawing.Size(30, 20);

            // 钓鱼分钟输入框
            numericUpDownFishingMinute = new NumericUpDown();
            numericUpDownFishingMinute.Location = new System.Drawing.Point(150, 130);
            numericUpDownFishingMinute.Size = new System.Drawing.Size(50, 20);
            numericUpDownFishingMinute.Minimum = 0;
            numericUpDownFishingMinute.Maximum = 59;
            
            // 钓鱼玩家标签
            labelFishingPlayer = new Label();
            labelFishingPlayer.Text = "玩家UID/昵称:";
            labelFishingPlayer.Location = new System.Drawing.Point(20, 160);
            labelFishingPlayer.Size = new System.Drawing.Size(100, 20);
            
            // 钓鱼玩家输入框
            textBoxFishingPlayer = new TextBox();
            textBoxFishingPlayer.Location = new System.Drawing.Point(120, 160);
            textBoxFishingPlayer.Size = new System.Drawing.Size(150, 20);
            
            // 泡温泉任务复选框
            checkBoxHotSpring = new CheckBox();
            checkBoxHotSpring.Text = "启用泡温泉";
            checkBoxHotSpring.Location = new System.Drawing.Point(20, 190);
            checkBoxHotSpring.Size = new System.Drawing.Size(150, 20);
            checkBoxHotSpring.CheckedChanged += CheckBoxHotSpring_CheckedChanged;

            // 泡温泉小时标签
            labelHotSpringHour = new Label();
            labelHotSpringHour.Text = "时";
            labelHotSpringHour.Location = new System.Drawing.Point(20, 220);
            labelHotSpringHour.Size = new System.Drawing.Size(30, 20);

            // 泡温泉小时输入框
            numericUpDownHotSpringHour = new NumericUpDown();
            numericUpDownHotSpringHour.Location = new System.Drawing.Point(50, 220);
            numericUpDownHotSpringHour.Size = new System.Drawing.Size(50, 20);
            numericUpDownHotSpringHour.Minimum = 0;
            numericUpDownHotSpringHour.Maximum = 23;

            // 泡温泉分钟标签
            labelHotSpringMinute = new Label();
            labelHotSpringMinute.Text = "分";
            labelHotSpringMinute.Location = new System.Drawing.Point(120, 220);
            labelHotSpringMinute.Size = new System.Drawing.Size(30, 20);

            // 泡温泉分钟输入框
            numericUpDownHotSpringMinute = new NumericUpDown();
            numericUpDownHotSpringMinute.Location = new System.Drawing.Point(150, 220);
            numericUpDownHotSpringMinute.Size = new System.Drawing.Size(50, 20);
            numericUpDownHotSpringMinute.Minimum = 0;
            numericUpDownHotSpringMinute.Maximum = 59;
            
            // 泡温泉玩家标签
            labelHotSpringPlayer = new Label();
            labelHotSpringPlayer.Text = "玩家UID/昵称:";
            labelHotSpringPlayer.Location = new System.Drawing.Point(20, 250);
            labelHotSpringPlayer.Size = new System.Drawing.Size(100, 20);
            
            // 泡温泉玩家输入框
            textBoxHotSpringPlayer = new TextBox();
            textBoxHotSpringPlayer.Location = new System.Drawing.Point(120, 250);
            textBoxHotSpringPlayer.Size = new System.Drawing.Size(150, 20);

            // 确认按钮
            btnConfirm = new Button();
            btnConfirm.Text = "确认";
            btnConfirm.Location = new System.Drawing.Point(140, 300);
            btnConfirm.Size = new System.Drawing.Size(75, 23);
            btnConfirm.Click += BtnConfirm_Click;

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] {
                checkBoxFishTank,
                labelHour,
                numericUpDownHour,
                labelMinute,
                numericUpDownMinute,
                checkBoxFishing,
                labelFishingHour,
                numericUpDownFishingHour,
                labelFishingMinute,
                numericUpDownFishingMinute,
                labelFishingPlayer,
                textBoxFishingPlayer,
                checkBoxHotSpring,
                labelHotSpringHour,
                numericUpDownHotSpringHour,
                labelHotSpringMinute,
                numericUpDownHotSpringMinute,
                labelHotSpringPlayer,
                textBoxHotSpringPlayer,
                btnConfirm
            });
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