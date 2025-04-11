using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

using System.Linq;
using System.Timers;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace ymzx
{
    // public static class ControlActions
    internal class ControlActions
    {
        private static Form1? mainForm;  // 添加Form1引用

        // 设置主窗体引用的方法
        internal static void SetMainForm(Form1 form)
        {
            mainForm = form;
        }

        // 用于控制自动化循环的取消标记
        private static CancellationTokenSource? automationCts = null;
        
        // 用于控制偷菜操作的取消标记
        private static CancellationTokenSource? stealingCts = null;

        // 当前偷菜操作类型（用于按钮文本显示）
        private static string currentStealingOperation = "";

        // 预定义的坐标点
        private static readonly Point RefreshButtonPoint = new Point(913, 487);//刷新键
        private static readonly Point UniversalButtonPoint = new Point(841, 433);//万能键
        private static readonly Point JumpButtonPoint = new Point(711, 447);//跳跃键
        private static readonly Point RejectFriendPullPoint = new Point(400, 343);//拒绝好友拉取键   
        
        // 用于控制定时任务的取消标记
        private static CancellationTokenSource? scheduledTaskCts = null;

        // 定时器，用于检查定时任务
        private static System.Timers.Timer? scheduledTaskTimer = null;

        // 记录上次执行定时任务的日期和任务类型
        private static Dictionary<string, DateTime> lastExecutionDates = new Dictionary<string, DateTime>();
        
        // 定时任务委托类型
        public delegate Task ScheduledTask(WebView2 webView, CancellationToken token);

        // 添加在类的开头部分
        // private static float? _dpiScale = null;

        // 获取DPI缩放比例的方法
        // private static float GetDpiScale()
        // {
        //     if (_dpiScale.HasValue)
        //         return _dpiScale.Value;

        //     if (mainForm == null || mainForm.IsDisposed)
        //         return 1.0f;

        //     // 获取主显示器的DPI缩放
        //     using (Graphics g = mainForm.CreateGraphics())
        //     {
        //         _dpiScale = g.DpiX / 96.0f;  // 96 DPI是基准值
        //         Console.WriteLine($"windows dpi scale valuevalue: {_dpiScale}");
        //         return _dpiScale.Value;
        //     }
        // }

        // 添加坐标转换方法
        // private static Point ScalePoint(Point original)
        // {
        //     float scale = GetDpiScale();
        //     return new Point(
        //         (int)(original.X / scale),
        //         (int)(original.Y / scale)
        //     );
        // }

        // "开始/停止"按钮点击事件
        public static async void BtnStartStop_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Parent is not Form form) return;
            if (!(form is Form1 form1)) return;

            if (automationCts == null)
            {
                // 开始自动化循环
                automationCts = new CancellationTokenSource();
                btn.Text = "停止"; // 更改按钮文字为"停止"
                
                // 根据无月卡版本复选框状态选择不同的循环
                Task automationTask;
                if (form1.checkBoxNoMonthlyCard.Checked)
                {
                    // 显示手动循环设置窗体，传入当前进程ID
                    using var settingsForm = new ManualLoopSettingsForm(Environment.ProcessId);
                    if (settingsForm.ShowDialog() == DialogResult.OK)
                    {
                        automationTask = RunManualLoop(
                            form1.webView2,
                            automationCts.Token,
                            settingsForm.FarmRanchTimes,
                            settingsForm.ExecuteWorkshop,
                            settingsForm.ExecuteFishing,
                            settingsForm.FishingCount,
                            settingsForm.RestTimeSeconds
                        );
                    }
                    else
                    {
                        // 用户取消设置，恢复按钮状态
                        btn.Text = "开始/停止";
                        automationCts.Cancel();
                        automationCts = null;
                        return;
                    }
                }
                else
                {
                    automationTask = RunAutomationLoop(form1.webView2, automationCts.Token);
                }

                _ = automationTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Console.WriteLine($"Automation task failed: {t.Exception}");
                    }
                    
                    // 循环结束后在 UI 线程中恢复按钮文字
                    form1.Invoke(() => 
                    { 
                        // 只有在不是因为定时任务而取消时才改变按钮文字
                        if (scheduledTaskCts == null)
                        {
                            btn.Text = "开始/停止";
                            automationCts = null;
                        }
                    });
                });
            }
            else
            {
                // 停止自动化循环
                automationCts.Cancel();
                automationCts.Dispose(); // 添加Dispose调用
                automationCts = null;
                btn.Text = "开始/停止";
            }
        }

        // 定时任务配置
        public class TaskSchedule(string name, ControlActions.ScheduledTask task, int hour, int minute, bool enabled = false, string extraParam = "")
        {
            public string TaskName { get; set; } = name;
            public ScheduledTask Task { get; set; } = task;
            public int ExecutionHour { get; set; } = hour;
            public int ExecutionMinute { get; set; } = minute;
            public bool IsEnabled { get; set; } = enabled;
            public string ExtraParam { get; set; } = extraParam;
        }

        // 定时任务列表
        private static readonly List<TaskSchedule> scheduledTasks = new List<TaskSchedule>
        {
            new("FishTank", async (webView, token) => await FishTankOperation(webView, token), 5, 0, false),//鱼缸收获，默认时间5点，默认不启用
            new("Fishing", async (webView, token) => await StealFishingOperation(webView, token), 12, 0, false, ""),//偷鱼，默认时间12点，默认不启用
            new("HotSpring", async (webView, token) => await HotSpringOperation(webView, token), 20, 0, false, ""),//泡温泉，默认时间20点，默认不启用
            // 在这里添加更多定时任务
            // new TaskSchedule("OtherTask", OtherTaskOperation, 12, 0),
        };

        // 初始化定时器 - 增加错误处理和更改检查频率
        static ControlActions()
        {
            try
            {
                scheduledTaskTimer = new System.Timers.Timer(60000); // 改为每分钟检查一次，减少不必要的检查
                scheduledTaskTimer.Elapsed += CheckScheduledTasks;
                scheduledTaskTimer.Start();

                // 初始化上次执行日期记录
                foreach (var task in scheduledTasks)
                {
                    lastExecutionDates[task.TaskName] = DateTime.MinValue;
                }
                
                Console.WriteLine("Scheduled task timer initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing scheduled task timer: {ex.Message}");
            }
        }

        // 获取定时任务配置（根据用户选择）
        public static TaskSchedule? GetScheduledTask(string taskName)
        {
            return scheduledTasks.FirstOrDefault(t => t.TaskName == taskName);
        }
        
        // 重置定时任务的执行状态，使其可以在当天再次执行
        public static void ResetTaskExecutionStatus(string taskName)
        {
            if (lastExecutionDates.ContainsKey(taskName))
            {
                lastExecutionDates[taskName] = DateTime.MinValue;
                Console.WriteLine($"Reset execution status for task: {taskName}");
            }
        }

        // 检查是否需要执行定时任务
        private static async void CheckScheduledTasks(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                Console.WriteLine($"Checking scheduled tasks - Current time: {now:HH:mm:ss}");
                
                foreach (var task in scheduledTasks.Where(t => t.IsEnabled))
                {
                    Console.WriteLine($"Checking task: {task.TaskName}, Enabled status: {task.IsEnabled}, Execution time: {task.ExecutionHour:00}:{task.ExecutionMinute:00}");
                    
                    // 修复错误：移除了多余的大括号，确保条件判断正确
                    if (now.Hour == task.ExecutionHour && 
                        now.Minute == task.ExecutionMinute && 
                        lastExecutionDates[task.TaskName].Date != now.Date) //这里确保不会在同一天重复执行
                    {
                        Console.WriteLine($"Starting task execution: {task.TaskName}");
                        lastExecutionDates[task.TaskName] = now.Date;
                        
                        try
                        {
                            // 在UI线程上执行任务
                            if (mainForm != null)
                            {
                                mainForm.Invoke(new Action(async () => {
                                    try
                                    {
                                        await ExecuteScheduledTask(task);
                                        Console.WriteLine($"Task completed: {task.TaskName}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Task execution error in UI thread: {task.TaskName}, Error: {ex.Message}");
                                    }
                                }));
                            }
                            else
                            {
                                Console.WriteLine("Cannot execute task: Main form is null");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error invoking UI thread: {ex.Message}");
                        }
                    }
                    // 添加执行时间过期检查，防止错过执行时间后的重复执行尝试
                    else if (now.Hour > task.ExecutionHour || 
                             (now.Hour == task.ExecutionHour && now.Minute > task.ExecutionMinute + 1))
                    {
                        // 如果当前时间已超过执行时间超过1分钟，且今天没有执行过，则标记为今天已执行
                        // 这样可以防止错过执行时间后的后续检查重复触发任务
                        if (lastExecutionDates[task.TaskName].Date != now.Date)
                        {
                            Console.WriteLine($"Marking task as executed for today (time passed): {task.TaskName}");
                            lastExecutionDates[task.TaskName] = now.Date;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scheduled task check error: {ex.Message}");
            }
        }

        // 添加停止当前操作的方法
        private static async Task StopCurrentOperation()
        {
            if (automationCts != null)
            {
                automationCts.Cancel();
                automationCts.Dispose();
                automationCts = null;
                await Task.Delay(1000); // 等待当前操作完全停止
            }
        }

        // 执行定时任务
        private static async Task ExecuteScheduledTask(TaskSchedule task)
        {
            if (mainForm == null)
            {
                Console.WriteLine("Main form not initialized, cannot execute task");
                return;
            }

            bool wasRunning = automationCts != null;
            Console.WriteLine($"Main loop status: {(wasRunning ? "Running" : "Not running")}");
            
            // 保存当前开始/停止按钮的引用
            Button? btnStartStop = null;
            if (wasRunning)
            {
                // 查找开始/停止按钮
                foreach (Control control in mainForm.Controls)
                {
                    if (control is Button btn && btn.Text == "停止")
                    {
                        btnStartStop = btn;
                        break;
                    }
                }
            }

            try
            {
                // 如果主循环在运行，暂停它
                if (wasRunning)
                {
                    Console.WriteLine("Pausing main loop");
                    automationCts?.Cancel();
                    automationCts = null;
                    await Task.Delay(1000); // 等待主循环完全停止
                }

                // 创建新的取消令牌用于定时任务
                using var taskCts = new CancellationTokenSource();
                
                // 执行定时任务
                Console.WriteLine($"Starting task execution: {task.TaskName}");
                await task.Task(mainForm.webView2, taskCts.Token);
                Console.WriteLine($"Task completed: {task.TaskName}");
                
                // 等待一段时间确保任务完全执行完成
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scheduled task execution error: {ex.Message}");
                // 不再抛出异常，以确保恢复主循环的代码能够执行
            }
            finally
            {
                // 如果之前主循环在运行，从头开始重新执行
                if (wasRunning)
                {
                    try
                    {
                        Console.WriteLine("Restarting main loop from beginning");
                        automationCts = new CancellationTokenSource();
                        
                        // 首先执行一次刷新按钮，将角色重置到起始位置
                        await ClickPoint(mainForm.webView2, RefreshButtonPoint);
                        await Task.Delay(1000);
                        
                        // 重新启动主循环
                        if (mainForm.checkBoxNoMonthlyCard.Checked)
                        {
                            // 加载保存的设置
                            var settings = LoadManualSettings();
                            if (settings != null)
                            {
                                _ = RunManualLoop(
                                    mainForm.webView2, 
                                    automationCts.Token,
                                    settings.FarmRanchTimes,
                                    settings.ExecuteWorkshop,
                                    settings.ExecuteFishing,
                                    settings.FishingCount,
                                    settings.RestTimeSeconds
                                );
                            }
                            else
                            {
                                // 如果没有保存的设置，使用默认值
                                _ = RunManualLoop(mainForm.webView2, automationCts.Token);
                            }
                        }
                        else
                        {
                            _ = RunAutomationLoop(mainForm.webView2, automationCts.Token);
                        }
                        
                        // 更新按钮文本为"停止"
                        if (btnStartStop != null)
                        {
                            mainForm.Invoke((Action)(() => 
                            {
                                btnStartStop.Text = "停止";
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Restart main loop error: {ex.Message}");
                    }
                }
            }
        }

        // 修改加载设置的方法，使用进程ID
        private static ManualLoopSettingsForm.ManualSettings? LoadManualSettings()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "YmzxHelper",
                    $"manual_settings_{Process.GetCurrentProcess().Id}.json"
                );

                if (File.Exists(settingsPath))
                {
                    string jsonString = File.ReadAllText(settingsPath);
                    return JsonSerializer.Deserialize<ManualLoopSettingsForm.ManualSettings>(jsonString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载手动设置失败: {ex.Message}");
            }
            return null;
        }

        // 自动化循环操作（有月卡版本）
        private static async Task RunAutomationLoop(WebView2 webView, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 新增步骤：激活网页（确保网页获得焦点）
                    await ActivateWebPage(webView);
                    await Task.Delay(100, token); // 添加短暂延迟确保页面激活

                    // 步骤1：按下刷新按钮
                    await ClickPoint(webView, RefreshButtonPoint);
                    if (token.IsCancellationRequested) break;

                    // 步骤2：按住 A 键 1.2 秒
                    await HoldKey(webView, "A", 1200);
                    if (token.IsCancellationRequested) break;
                    await Task.Delay(200, token);

                    // 步骤3：按下并释放 Q 键
                    await PressKey(webView, "Q");
                    if (token.IsCancellationRequested) break;
                    await Task.Delay(200, token);

                    // 步骤4：连续按下 10 次中心键，每次间隔 10 秒
                    for (int i = 0; i < 10; i++)
                    {
                        await ActivateWebPage(webView);
                        if (token.IsCancellationRequested) break;
                        await Task.Delay(10000, token);
                    }
                    if (token.IsCancellationRequested) break;

                    // 步骤5：等待 50 秒
                    await Task.Delay(50000, token);
                }
            }
            catch (TaskCanceledException)
            {
                // 正常取消时抛出的异常，无需处理
            }
        }

        // 农场操作脚本 - 增加点击次数参数
        private static async Task FarmOperation(WebView2 webView, CancellationToken token, int clickTimes = 9)
        {
            // 定期检查是否请求取消操作
            if (token.IsCancellationRequested) return;
            
            // 点击刷新按钮
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(200, token);

            // 按W键1.6秒
            await HoldKey(webView, "W", 1600);
            await Task.Delay(200, token);

            // 拖动转向回正
            await SimulateMouseDrag(webView, 543, 40, 450, 40, 1300);
            await Task.Delay(200, token);

            // 第一列
            await HoldKey(webView, "W", 1200); // 到第一块地
            await Task.Delay(200, token);
            for (int i = 0; i < 5; i++)
            {
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);//自己农场牧场设置500没问题，但是偷持续较长，设置1s
                if (i < 4)
                {
                    await HoldKey(webView, "W", 700);
                    await Task.Delay(200, token);
                }
            }

            // 第二列
            await HoldKey(webView, "D", 700);
            await Task.Delay(200, token);

            for (int i = 0; i < 5; i++)
            {
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
                if (i < 4)
                {
                    await HoldKey(webView, "S", 700);
                    await Task.Delay(200, token);
                }
            }

            // 第三列
            await HoldKey(webView, "D", 700);
            await Task.Delay(200, token);
            for (int i = 0; i < 5; i++)
            {
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
                if (i < 4)
                {
                    await HoldKey(webView, "W", 700);
                    await Task.Delay(200, token);
                }
            }

            // 第四列
            await HoldKey(webView, "D", 700);
            await Task.Delay(200, token);
            for (int i = 0; i < 5; i++)
            {
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
                if (i < 4)
                {
                    await HoldKey(webView, "S", 700);
                    await Task.Delay(200, token);
                }
            }

            // 第五列
            await HoldKey(webView, "D", 700);
            await Task.Delay(200, token);
            for (int i = 0; i < 5; i++)
            {
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
                if (i < 4)
                {
                    await HoldKey(webView, "W", 700);
                    await Task.Delay(200, token);
                }
            }

            // 第六列  (小号可能还没开通，问题不大)
            await HoldKey(webView, "D", 700);
            await Task.Delay(200, token);
            for (int i = 0; i < 5; i++)
            {
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
                if (i < 4)
                {
                    await HoldKey(webView, "S", 700);
                    await Task.Delay(200, token);
                }
            }
        }

        // 牧场操作脚本 - 增加点击次数参数
        private static async Task RanchOperation(WebView2 webView, CancellationToken token, int clickTimes = 6)
        {
            // 定期检查是否请求取消操作
            if (token.IsCancellationRequested) return;
            
            // 点击刷新按钮
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(200, token);

            // 按W键1.5秒
            await HoldKey(webView, "W", 1500);
            await Task.Delay(200, token);

            // 按D键0.9秒
            await HoldKey(webView, "D", 900);
            await Task.Delay(200, token);

            // 按W键0.65秒
            await HoldKey(webView, "W", 650);
            await Task.Delay(200, token);

            // 拖动转向回正
            await SimulateMouseDrag(webView, 419, 49, 574, 49, 1300);
            await Task.Delay(200, token);

            // 第一列牧场
            for (int i = 0; i < 3; i++)
            {
                await ClickPoint(webView, JumpButtonPoint);
                await HoldKey(webView, "W", 700);
                await Task.Delay(200, token);
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);//自己农场牧场设置500没问题，但是偷持续较长，设置1s
            }

            // 第二列牧场
            await ClickPoint(webView, JumpButtonPoint);
            await HoldKey(webView, "A", 700);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(1000, token);

            for (int i = 0; i < 2; i++)
            {
                await ClickPoint(webView, JumpButtonPoint);
                await HoldKey(webView, "S", 700);
                await Task.Delay(200, token);
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
            }

            // 第三列牧场
            await ClickPoint(webView, JumpButtonPoint);
            await HoldKey(webView, "A", 700);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(1000, token);

            for (int i = 0; i < 2; i++)
            {
                await ClickPoint(webView, JumpButtonPoint);
                await HoldKey(webView, "W", 700);
                await Task.Delay(200, token);
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
            }

            // 第四列牧场
            await ClickPoint(webView, JumpButtonPoint);
            await HoldKey(webView, "A", 700);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(1000, token);

            for (int i = 0; i < 2; i++)
            {
                await ClickPoint(webView, JumpButtonPoint);
                await HoldKey(webView, "S", 700);
                await Task.Delay(200, token);
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
            }

            // 第五列牧场
            await ClickPoint(webView, JumpButtonPoint);
            await HoldKey(webView, "A", 700);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(1000, token);

            for (int i = 0; i < 2; i++)
            {
                await ClickPoint(webView, JumpButtonPoint);
                await HoldKey(webView, "W", 700);
                await Task.Delay(200, token);
                await ClickUniversalButton(webView, clickTimes, token);
                await Task.Delay(1000, token);
            }
        }

        // 加工坊操作脚本
        private static async Task WorkshopOperation(WebView2 webView, CancellationToken token, int clickTimes = 6)
        {
            // 点击刷新按钮
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(200, token);

            // 按D键1秒
            await HoldKey(webView, "D", 1000);
            await Task.Delay(200, token);

            // 按Q键
            await PressKey(webView, "Q");
            await Task.Delay(200, token);

            // 拖动校准发射方向
            // await SimulateMouseDrag(webView, 480, 270, 347, 226, 1300);
            await PressKey(webView, "A");
            await Task.Delay(200, token);
            await PressKey(webView, "A");
            await Task.Delay(200, token);
            await PressKey(webView, "A");
            await Task.Delay(200, token);
            await PressKey(webView, "W");
            await Task.Delay(200, token);

            // 点击万用键（发射）
            await ClickPoint(webView, UniversalButtonPoint);
            await Task.Delay(3000, token);

            // 按W键0.3秒，走到门口
            await HoldKey(webView, "W", 300);
            await Task.Delay(200, token);


            // 点击万用键（防止没自动进入加工坊）
            await ClickPoint(webView, UniversalButtonPoint);
            await Task.Delay(10000, token);

            // 按W键1.9秒
            await HoldKey(webView, "W", 1900);
            await Task.Delay(20, token);

            // 第一排的两个加工器
            // 第一个加工器
            await HoldKey(webView, "A", 300);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(500, token);
            await PressKey(webView, "Q");
            await Task.Delay(500, token);

            // 第二个加工器
            await HoldKey(webView, "D", 300);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(500, token);
            await PressKey(webView, "Q");
            await Task.Delay(500, token);

            // 第二排的两个加工器
            // 第一个加工器
            await HoldKey(webView, "S", 350);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(500, token);
            await PressKey(webView, "Q");
            await Task.Delay(500, token);

            // 第二个加工器
            await HoldKey(webView, "A", 300);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(500, token);
            await PressKey(webView, "Q");
            await Task.Delay(500, token);

            // 第三排的两个加工器
            // 第一个加工器
            await HoldKey(webView, "S", 350);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(500, token);
            await PressKey(webView, "Q");
            await Task.Delay(500, token);

            // 第二个加工器
            await HoldKey(webView, "D", 300);
            await Task.Delay(200, token);
            await ClickUniversalButton(webView, clickTimes, token);
            await Task.Delay(500, token);
            await PressKey(webView, "Q");
            await Task.Delay(500, token);

            // 出门回农场
            await HoldKey(webView, "S", 1500);
            await Task.Delay(20, token);
            await ClickPoint(webView, UniversalButtonPoint);
            await Task.Delay(15000, token);
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(500, token);
        }

        // 休息操作脚本（每10秒点击一次拒绝好友拉取按钮）
        private static async Task RestOperation(WebView2 webView, CancellationToken token, int restTimeSeconds)
        {
            if (restTimeSeconds <= 0) return; // 如果不休息，直接返回
            
            int clickCount = restTimeSeconds / 10; // 计算需要点击的次数
            for (int i = 0; i < clickCount; i++)
            {
                if (token.IsCancellationRequested) break;
                await ClickPoint(webView, RejectFriendPullPoint);
                await Task.Delay(10000, token); // 每10秒点击一次
            }
        }

        // 手动操作循环脚本
        private static async Task RunManualLoop(WebView2 webView, CancellationToken token, int farmRanchTimes = 2, bool executeWorkshop = true, bool executeFishing = false, int fishingCount = 24, int restTimeSeconds = 0)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 新增步骤：激活网页（确保网页获得焦点）
                    await ActivateWebPage(webView);
                    await Task.Delay(100, token);

                    // 农场和牧场操作
                    for (int i = 0; i < farmRanchTimes; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        await FarmOperation(webView, token);
                        if (token.IsCancellationRequested) break;
                        await RanchOperation(webView, token);
                    }
                    if (token.IsCancellationRequested) break;

                    // 加工坊操作
                    if (executeWorkshop)
                    {
                        await WorkshopOperation(webView, token);
                    }
                    if (token.IsCancellationRequested) break;

                    // 钓鱼操作
                    if (executeFishing)
                    {
                        await FishingOperation(webView, token, fishingCount);
                    }
                    if (token.IsCancellationRequested) break;

                    // 休息操作
                    await RestOperation(webView, token, restTimeSeconds);
                    if (token.IsCancellationRequested) break;
                }
            }
            catch (TaskCanceledException)
            {
                // 正常取消时抛出的异常，无需处理
            }
        }

        // 鱼缸收获操作
        private static async Task FishTankOperation(WebView2 webView, CancellationToken token)
        {
            // 点击屏幕中心激活
            await ActivateWebPage(webView);
            await Task.Delay(100, token);

            // 按刷新键一次
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(100, token);

            // 按D键0.8秒
            await HoldKey(webView, "D", 800);
            await Task.Delay(100, token);

            // 按Q键一次
            await PressKey(webView, "Q");
            await Task.Delay(100, token);

            // 按A键一次
            await PressKey(webView, "A");
            await Task.Delay(100, token);

            // 按W键一次
            await PressKey(webView, "W");
            await Task.Delay(100, token);

            // 点击万用键一次
            await ClickPoint(webView, UniversalButtonPoint);
            await Task.Delay(6000, token);

            // 按E键一次
            await PressKey(webView, "E");
            await Task.Delay(200, token);

            // 按刷新一次
            await ClickPoint(webView, RefreshButtonPoint);

            await Task.Delay(1000, token);
            await ActivateWebPage(webView);
            await Task.Delay(100, token);
            await ActivateWebPage(webView);
            await Task.Delay(5000, token);
        }

        // 钓鱼操作脚本
        private static async Task FishingOperation(WebView2 webView, CancellationToken token, int fishingCount = 5)
        {
            // 定期检查是否请求取消操作
            if (token.IsCancellationRequested) return;
            
            // 点击刷新按钮
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(1000, token);

            if (token.IsCancellationRequested) return;
            
            // 按W键6.8秒（这一步是考虑碰撞标准稻草人的时间）
            await HoldKey(webView, "W", 6800);
            await Task.Delay(1000, token);

            // 重复钓鱼指定次数
            for (int i = 0; i < fishingCount && !token.IsCancellationRequested; i++)
            {
                // 钓鱼1次
                // 按下万用键一次（抛竿）
                await ClickPoint(webView, UniversalButtonPoint);
                await Task.Delay(3000, token);
                
                if (token.IsCancellationRequested) break;

                // 按下万用键一次（收竿）
                await ClickPoint(webView, UniversalButtonPoint);
                await Task.Delay(5500, token); // 拉扯时间认为5.5s
                
                if (token.IsCancellationRequested) break;

                // 连续三次点击万用键（处理鱼）
                await ClickPoint(webView, UniversalButtonPoint);
                await Task.Delay(100, token);
                
                if (token.IsCancellationRequested) break;
                
                await ClickPoint(webView, UniversalButtonPoint);
                await Task.Delay(100, token);
                
                if (token.IsCancellationRequested) break;
                
                await ClickPoint(webView, UniversalButtonPoint);
                await Task.Delay(3000, token);
            }

            if (token.IsCancellationRequested) return;
            
            // 钓鱼结束，按刷新键
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(5000, token);
        }

        // 偷鱼操作脚本
        private static async Task StealFishingOperation(WebView2 webView, CancellationToken token)
        {
            // 获取偷鱼玩家的UID/昵称
            var fishingTask = scheduledTasks.FirstOrDefault(t => t.TaskName == "Fishing");
            if (fishingTask == null || string.IsNullOrEmpty(fishingTask.ExtraParam))
            {
                Console.WriteLine("偷鱼任务缺少玩家UID/昵称参数");
                return;
            }
            
            string playerUidOrName = fishingTask.ExtraParam;
            
            // 点击刷新
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(200, token);
            
            // 点击社交按钮 (924,162)
            await ClickPoint(webView, new Point(924, 162));
            await Task.Delay(200, token);
            
            // 点击下面输入昵称或uid (625,516)
            await ClickPoint(webView, new Point(625, 516));
            await Task.Delay(200, token);
            
            // 输入要去的玩家uid或昵称
            await InputText(webView, playerUidOrName);
            // 在按Enter之前增加足够的延迟确保输入框获得焦点并且内容已填入
            await Task.Delay(1000, token);
            
            // 模拟按下enter键一次
            await DirectPressEnter(webView);
            // 等待搜索结果显示
            await Task.Delay(2000, token);
            
            // 点击拜访该玩家农场 (915,166)
            await ClickPoint(webView, new Point(915, 166));
            await Task.Delay(300, token);
            await ClickPoint(webView, new Point(581, 346));//如果当前无人机正在运行，会有提示窗口，这个点击能去掉该窗口
            await Task.Delay(8000, token);
            
            // 点击刷新按钮
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(1000, token);
            
            // 执行钓鱼操作，默认5次
            await FishingOperation(webView, token, 5);
            
            // 点击回家 (868,32)
            await ClickPoint(webView, new Point(868, 32));
            await Task.Delay(6000, token);
            
            // 点击刷新
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(600, token);
        }

        // 泡温泉喝茶脚本
        private static async Task HotSpringOperation(WebView2 webView, CancellationToken token)
        {
            // 获取泡温泉玩家的UID/昵称
            var hotSpringTask = scheduledTasks.FirstOrDefault(t => t.TaskName == "HotSpring");
            if (hotSpringTask == null || string.IsNullOrEmpty(hotSpringTask.ExtraParam))
            {
                Console.WriteLine("泡温泉任务缺少玩家UID/昵称参数");
                return;
            }
            
            string playerUidOrName = hotSpringTask.ExtraParam;
            
            // 点击刷新
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(200, token);
            
            // 点击社交按钮 (924,162)
            await ClickPoint(webView, new Point(924, 162));
            await Task.Delay(200, token);
            
            // 点击下面输入昵称或uid (625,516)
            await ClickPoint(webView, new Point(625, 516));
            await Task.Delay(200, token);
            
            // 输入要去的玩家uid或昵称
            await InputText(webView, playerUidOrName);
            // 在按Enter之前增加足够的延迟确保输入框获得焦点并且内容已填入
            await Task.Delay(1000, token);
            
            // 模拟按下enter键一次 - 使用更直接的方法
            await DirectPressEnter(webView);
            // 等待搜索结果显示
            await Task.Delay(2000, token);
            
            // 点击拜访该玩家农场 (915,166)
            await ClickPoint(webView, new Point(915, 166));
            await Task.Delay(300, token);
            await ClickPoint(webView, new Point(581, 346));//如果当前无人机正在运行，会有提示窗口，这个点击能去掉该窗口
            await Task.Delay(8000, token);
            
            // 按下d键 900ms
            await HoldKey(webView, "D", 900);
            await Task.Delay(200, token);
            
            // 按下q键(PressKey)（进入发射方向调整）
            await PressKey(webView, "Q");
            await Task.Delay(200, token);
            
            // 按下d键一次
            await PressKey(webView, "D");
            await Task.Delay(200, token);
            
            // 按下d键一次
            await PressKey(webView, "D");
            await Task.Delay(200, token);
            
            // 按下w键一次
            await PressKey(webView, "W");
            await Task.Delay(200, token);
            
            // 按下w键一次
            await PressKey(webView, "W");
            await Task.Delay(200, token);
            
            // 按下w键一次
            await PressKey(webView, "W");
            await Task.Delay(200, token);
            
            // 按下万用键一次（发射）
            await ClickPoint(webView, UniversalButtonPoint);
            await Task.Delay(5000, token); // 等待发射过程
            await HoldKey(webView, "D", 400);
            await Task.Delay(200, token);
            await HoldKey(webView, "S", 400);
            await Task.Delay(200, token);
            await HoldKey(webView, "D", 500);
            await Task.Delay(200, token);
            await HoldKey(webView, "S", 500);
            await Task.Delay(200, token);
            
            // 按下万用键一次（泡温泉）
            await ClickPoint(webView, UniversalButtonPoint);
            await Task.Delay(500, token);
            
            // 按下q喝茶
            await PressKey(webView, "Q");
            await Task.Delay(500, token);
            
            // 点击温泉增益弹窗确定 (580,345)
            await ClickPoint(webView, new Point(580, 345));
            await Task.Delay(15000, token);
            
            // 点击回家 (868,32)
            await ClickPoint(webView, new Point(868, 32));
            await Task.Delay(6000, token);
            
            // 点击刷新
            await ClickPoint(webView, RefreshButtonPoint);
            await Task.Delay(600, token);
        }
        
        // 添加一个直接执行Enter键的方法，绕过可能的队列延迟问题
        private static async Task DirectPressEnter(WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                string script = @"
                    (function() {
                        // 直接模拟按下Enter键事件
                        var enterEvent = new KeyboardEvent('keydown', {
                            key: 'Enter',
                            code: 'Enter',
                            keyCode: 13,
                            which: 13,
                            bubbles: true,
                            cancelable: true
                        });
                        document.activeElement.dispatchEvent(enterEvent);
                        
                        // 模拟松开Enter键事件
                        var enterUpEvent = new KeyboardEvent('keyup', {
                            key: 'Enter',
                            code: 'Enter',
                            keyCode: 13,
                            which: 13,
                            bubbles: true,
                            cancelable: true
                        });
                        document.activeElement.dispatchEvent(enterUpEvent);
                    })();
                ";
                
                // 执行脚本并确保立即运行
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        /*以下为辅助方法*/
        // 辅助方法：点击指定坐标
        private static async Task ClickPoint(WebView2 webView, Point point)
        {
            // Point scaledPoint = ScalePoint(point);
            // 先显示点击效果
            await ShowClickEffect(webView, point.X, point.Y);
            
            if (webView.CoreWebView2 != null)
            {
                string script = $@"
                    (function() {{
                        const element = document.elementFromPoint({point.X}, {point.Y});
                        if (element) {{
                            // mousedown
                            element.dispatchEvent(new MouseEvent('mousedown', {{
                                bubbles: true,
                                cancelable: true,
                                view: window,
                                clientX: {point.X},
                                clientY: {point.Y}
                            }}));

                            // mouseup
                            element.dispatchEvent(new MouseEvent('mouseup', {{
                                bubbles: true,
                                cancelable: true,
                                view: window,
                                clientX: {point.X},
                                clientY: {point.Y}
                            }}));

                            // click
                            element.dispatchEvent(new MouseEvent('click', {{
                                bubbles: true,
                                cancelable: true,
                                view: window,
                                clientX: {point.X},
                                clientY: {point.Y}
                            }}));
                        }}
                    }})();
                ";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
                // 添加短暂延迟确保点击事件被处理
                await Task.Delay(50);
            }
        }

        // 辅助方法：模拟鼠标拖动
        private static async Task SimulateMouseDrag(WebView2 webView, int startX, int startY, int endX, int endY, int durationMs)
        {
            Point StartPoint = new(startX, startY);
            Point EndPoint = new(endX, endY);
            
            string script = $@"
                (function() {{
                    // 创建轨迹线
                    const line = document.createElement('div');
                    line.style.position = 'absolute';
                    line.style.left = '{StartPoint.X}px';
                    line.style.top = '{StartPoint.Y}px';
                    line.style.width = '{Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2))}px';
                    line.style.height = '2px';
                    line.style.backgroundColor = 'rgba(0, 255, 0, 0.5)';
                    line.style.transformOrigin = 'left';
                    line.style.transform = 'rotate(' + Math.atan2({EndPoint.Y - StartPoint.Y}, {EndPoint.X - StartPoint.X}) + 'rad)';
                    line.style.pointerEvents = 'none';
                    line.style.zIndex = '10000';
                    document.body.appendChild(line);

                    // 创建起点标记
                    const startDot = document.createElement('div');
                    startDot.style.position = 'absolute';
                    startDot.style.left = '{StartPoint.X}px';
                    startDot.style.top = '{StartPoint.Y}px';
                    startDot.style.width = '10px';
                    startDot.style.height = '10px';
                    startDot.style.backgroundColor = 'rgba(0, 255, 0, 0.8)';
                    startDot.style.borderRadius = '50%';
                    startDot.style.pointerEvents = 'none';
                    startDot.style.zIndex = '10000';
                    document.body.appendChild(startDot);

                    // 创建mousedown事件
                    var downEvt = new MouseEvent('mousedown', {{
                        bubbles: true,
                        cancelable: true,
                        view: window,
                        clientX: {StartPoint.X},
                        clientY: {StartPoint.Y}
                    }});
                    document.elementFromPoint({StartPoint.X}, {StartPoint.Y}).dispatchEvent(downEvt);

                    // 修改移动事件的坐标计算
                    const steps = 50;
                    const stepDuration = {durationMs} / steps;
                    const dx = ({EndPoint.X} - {StartPoint.X}) / steps;
                    const dy = ({EndPoint.Y} - {StartPoint.Y}) / steps;

                    for(let i = 1; i <= steps; i++) {{
                        setTimeout(() => {{
                            const x = {StartPoint.X} + dx * i;
                            const y = {StartPoint.Y} + dy * i;
                            const moveEvt = new MouseEvent('mousemove', {{
                                bubbles: true,
                                cancelable: true,
                                view: window,
                                clientX: x,
                                clientY: y
                            }});
                            document.elementFromPoint(x, y).dispatchEvent(moveEvt);
                        }}, stepDuration * i);
                    }}

                    // 创建mouseup事件
                    setTimeout(() => {{
                        const upEvt = new MouseEvent('mouseup', {{
                            bubbles: true,
                            cancelable: true,
                            view: window,
                            clientX: {EndPoint.X},
                            clientY: {EndPoint.Y}
                        }});
                        document.elementFromPoint({EndPoint.X}, {EndPoint.Y}).dispatchEvent(upEvt);
                        // window.chrome.webview.hostObjects.sync.dragComplete.set(true);
                    }}, {durationMs});

                    // 3秒后移除视觉效果
                    setTimeout(() => {{
                        line.remove();
                        startDot.remove();
                    }}, {durationMs + 3000});
                }})();
            ";

                // 注册回调
                // webView.CoreWebView2.AddHostObjectToScript("dragComplete", tcs);
            await webView.CoreWebView2.ExecuteScriptAsync(script);
                
                // 等待拖动完成
                // await tcs.Task;
            await Task.Delay(durationMs); // 额外等待以确保事件完全处理完成
        }

        // 辅助方法：点击万用键指定次数
        private static async Task ClickUniversalButton(WebView2 webView, int times, CancellationToken token)
        {
            for (int i = 0; i < times; i++)
            {
                if (token.IsCancellationRequested) break;
                await ClickPoint(webView, UniversalButtonPoint);
                await Task.Delay(500, token);
            }
        }

        // 辅助方法：激活网页，使用 IIFE 包装，模拟点击页面正中央
        private static async Task ActivateWebPage(WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                string script = @"
                    (function(){
                        var x = window.innerWidth / 2;
                        var y = window.innerHeight / 2;
                        var evt = new MouseEvent('click', {
                            bubbles: true,
                            cancelable: true,
                            view: window,
                            clientX: x,
                            clientY: y
                        });
                        document.elementFromPoint(x, y).dispatchEvent(evt);
                    })();
                ";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        // 辅助方法：模拟一次按键（包括 keydown 与 keyup）
        private static async Task PressKey(WebView2 webView, string key)
        {
            await SimulateKeyEvent(webView, key, "keydown");
            await SimulateKeyEvent(webView, key, "keyup");
        }

        // 辅助方法：模拟按住键（先发送 keydown，延时后发送 keyup）
        private static async Task HoldKey(WebView2 webView, string key, int durationMilliseconds)
        {
            await SimulateKeyEvent(webView, key, "keydown");
            await Task.Delay(durationMilliseconds);
            await SimulateKeyEvent(webView, key, "keyup");
        }

        // 辅助方法：通过注入 JavaScript 代码模拟键盘事件
        private static async Task SimulateKeyEvent(WebView2 webView, string key, string eventType)
        {
            if (webView.CoreWebView2 != null)
            {
                // 如果 key 是单个字符，则计算 keyCode，code 以 KeyX 格式（X为大写字母）
                int keyCode = key.Length == 1 ? (int)key[0] : 0;
                string code = key.Length == 1 ? $"Key{key.ToUpper()}" : "";
                string script = $@"
                    (function(){{
                        var evt = new KeyboardEvent('{eventType}', {{
                            key: '{key}',
                            keyCode: {keyCode},
                            code: '{code}',
                            bubbles: true
                        }});
                        document.dispatchEvent(evt);
                    }})();
                ";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        // 辅助方法：输入文本
        private static async Task InputText(WebView2 webView, string text)
        {
            if (webView.CoreWebView2 != null)
            {
                string escapedText = text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
                string script = $@"
                    (function() {{
                        var activeElement = document.activeElement;
                        if (activeElement) {{
                            // 清空当前内容
                            activeElement.value = '';
                            
                            // 设置新内容
                            activeElement.value = ""{escapedText}"";
                            
                            // 触发input事件
                            var event = new Event('input', {{ bubbles: true }});
                            activeElement.dispatchEvent(event);
                        }}
                    }})();
                ";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        // 辅助方法：添加显示点击效果的方法
        private static async Task ShowClickEffect(WebView2 webView, int x, int y)
        {
            string script = @"
                (function() {
                    const dot = document.createElement('div');
                    dot.style.position = 'absolute';
                    dot.style.left = '" + x + @"px';
                    dot.style.top = '" + y + @"px';
                    dot.style.width = '20px';
                    dot.style.height = '20px';
                    dot.style.backgroundColor = 'rgba(255, 0, 0, 0.5)';
                    dot.style.borderRadius = '50%';
                    dot.style.pointerEvents = 'none';
                    dot.style.zIndex = '10000';
                    document.body.appendChild(dot);
                    setTimeout(() => dot.remove(), 1000);
                })();
            ";
            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        // "刷新"按钮点击事件：刷新 WebView2 网页
        public static void BtnRefresh_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                Form form = btn.FindForm();
                if (form is Form1 form1)
                {
                    form1.webView2?.CoreWebView2?.Reload();
                }
            }
        }

        // "清理缓存"按钮点击事件：清理WebView2的缓存和Cookie，用于切换账号
        public static async void BtnClearCache_Click(object? sender, EventArgs e)
        {
            if (!(sender is Button btn)) return;
            Form form = btn.FindForm();
            if (!(form is Form1 form1) || form1.webView2?.CoreWebView2 == null) return;

            // 询问用户是否确定要清理缓存
            if (MessageBox.Show("清理缓存将退出当前账号，需要重新登录。\r\n确定要继续吗？", 
                              "清理缓存", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // 禁用按钮防止重复点击
                    btn.Enabled = false;
                    btn.Text = "清理中...";
                    
                    // 清理所有浏览器数据
                    await form1.webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(
                        Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.Cookies |
                        Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.AllSite);
                    
                    // 执行JavaScript清理本地存储
                    await form1.webView2.CoreWebView2.ExecuteScriptAsync(
                        "localStorage.clear(); sessionStorage.clear();");
                    
                    // 重新加载页面
                    form1.webView2.CoreWebView2.Reload();
                    
                    MessageBox.Show("缓存已清理完毕，请重新登录账号。", "提示", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清理缓存时发生错误: {ex.Message}", "错误", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // 恢复按钮状态
                    btn.Enabled = true;
                    btn.Text = "清理缓存";
                }
            }
        }

        // "偷菜"按钮点击事件
        public static async void BtnStealing_Click(object? sender, EventArgs e)
        {
            if (!(sender is Button btn)) return;
            Form form = btn.FindForm();
            if (!(form is Form1 form1)) return;

            // 如果当前有偷菜操作正在进行，则取消它
            if (stealingCts != null)
            {
                stealingCts.Cancel();
                stealingCts.Dispose();
                stealingCts = null;
                btn.Text = "偷菜";
                currentStealingOperation = "";
                return;
            }

            // 创建一个包含三个选项的弹窗
            Form stealOptionsForm = new Form()
            {
                Text = "偷菜选项",
                Size = new Size(200, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // 添加三个按钮
            Button btnFarm = new Button()
            {
                Text = "偷农场",
                Size = new Size(160, 30),
                Location = new Point(15, 10),
                FlatStyle = FlatStyle.Flat
            };
            btnFarm.FlatAppearance.BorderSize = 0;
            btnFarm.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnFarm.Click += async (s, args) => {
                stealOptionsForm.Close();
                try {
                    // 先检查webView2是否为空
                    if (form1.webView2 == null || form1.webView2.CoreWebView2 == null)
                    {
                        MessageBox.Show("WebView2控件未初始化完成，请等待页面加载完毕再试", "错误", 
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    // 创建新的取消令牌
                    stealingCts = new CancellationTokenSource();
                    // 更新按钮文本以反映当前操作
                    currentStealingOperation = "偷农场中";
                    btn.Text = currentStealingOperation;
                    
                    // 执行偷农场操作
                    await Task.Delay(500);
                    await FarmOperation(form1.webView2, stealingCts.Token, 4);
                }
                catch (TaskCanceledException) {
                    // 正常取消的异常，忽略
                }
                catch (Exception ex) {
                    // 记录详细错误信息
                    Console.WriteLine($"偷农场详细错误: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"偷农场操作出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally {
                    // 无论成功还是失败，都恢复按钮状态
                    if (!stealingCts?.IsCancellationRequested ?? false)
                    {
                        btn.Text = "偷菜";
                        stealingCts?.Dispose();
                        stealingCts = null;
                        currentStealingOperation = "";
                    }
                }
            };
            stealOptionsForm.Controls.Add(btnFarm);

            Button btnRanch = new Button()
            {
                Text = "偷牧场",
                Size = new Size(160, 30),
                Location = new Point(15, 50),
                FlatStyle = FlatStyle.Flat
            };
            btnRanch.FlatAppearance.BorderSize = 0;
            btnRanch.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnRanch.Click += async (s, args) => {
                stealOptionsForm.Close();
                try {
                    // 先检查webView2是否为空
                    if (form1.webView2 == null || form1.webView2.CoreWebView2 == null)
                    {
                        MessageBox.Show("WebView2控件未初始化完成，请等待页面加载完毕再试", "错误", 
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 创建新的取消令牌
                    stealingCts = new CancellationTokenSource();
                    // 更新按钮文本以反映当前操作
                    currentStealingOperation = "偷牧场中";
                    btn.Text = currentStealingOperation;
                    
                    // 执行偷牧场操作
                    await Task.Delay(500);
                    await RanchOperation(form1.webView2, stealingCts.Token, 4);
                }
                catch (TaskCanceledException) {
                    // 正常取消的异常，忽略
                }
                catch (Exception ex) {
                    // 发生错误时恢复按钮状态
                    // 记录详细错误信息
                    Console.WriteLine($"偷牧场详细错误: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"偷牧场操作出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally {
                    // 无论成功还是失败，都恢复按钮状态
                    if (!stealingCts?.IsCancellationRequested ?? false)
                    {
                        btn.Text = "偷菜";
                        stealingCts?.Dispose();
                        stealingCts = null;
                        currentStealingOperation = "";
                    }
                }
            };
            stealOptionsForm.Controls.Add(btnRanch);

            Button btnFishing = new Button()
            {
                Text = "钓鱼",
                Size = new Size(160, 30),
                Location = new Point(15, 90),
                FlatStyle = FlatStyle.Flat,
                Enabled = true
            };
            btnFishing.FlatAppearance.BorderSize = 0;
            btnFishing.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnFishing.Click += async (s, args) => {
                stealOptionsForm.Close();
                try {
                    // 先检查webView2是否为空
                    if (form1.webView2 == null || form1.webView2.CoreWebView2 == null)
                    {
                        MessageBox.Show("WebView2控件未初始化完成，请等待页面加载完毕再试", "错误", 
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 创建新的取消令牌
                    stealingCts = new CancellationTokenSource();
                    // 更新按钮文本以反映当前操作
                    currentStealingOperation = "钓鱼中";
                    btn.Text = currentStealingOperation;
                    
                    // 执行钓鱼操作
                    await Task.Delay(500);
                    await FishingOperation(form1.webView2, stealingCts.Token, 24);
                }
                catch (TaskCanceledException) {
                    // 正常取消的异常，忽略
                }
                catch (Exception ex) {
                    // 记录详细错误信息
                    Console.WriteLine($"钓鱼详细错误: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"钓鱼操作出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally {
                    // 无论成功还是失败，都恢复按钮状态
                    if (!stealingCts?.IsCancellationRequested ?? false)
                    {
                        btn.Text = "偷菜";
                        stealingCts?.Dispose();
                        stealingCts = null;
                        currentStealingOperation = "";
                    }
                }
            };
            stealOptionsForm.Controls.Add(btnFishing);

            stealOptionsForm.ShowDialog();
        }

        // "必读"按钮点击事件
        public static void BtnMustRead_Click(object? sender, EventArgs e)
        {
            string message = "必读事项：\r\n1.若无按键映射，请点击右侧一键重置或重进游戏；\r\n2.请调整最低画质，关闭画质增强，关闭声音；(减少GPU消耗）\r\n3.更多-设置-游戏-镜头辅助关闭（务必）；\r\n4.月卡循环流程：R复位，A走向无人机，Q启动无人机，R消除钓鱼和对话弹窗，2分一周期；；\r\n5. 无月卡流程：农牧*自选次数+加工坊+鱼塘+休息2分为一周期";
            MessageBox.Show(message, "必读", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // "一键设置"按钮点击事件
        public static async void BtnOneClickSettings_Click(object? sender, EventArgs e)
        {
            if (mainForm == null || mainForm.webView2 == null || mainForm.webView2.CoreWebView2 == null)
            {
                MessageBox.Show("WebView2控件未初始化完成，请等待页面加载完毕再试", "错误", 
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 创建一个包含两个选项的弹窗
            Form settingsOptionsForm = new Form()
            {
                Text = "一键设置选项",
                Size = new Size(200, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // 添加"登录界面"按钮
            Button btnLoginSettings = new Button()
            {
                Text = "登录界面",
                Size = new Size(160, 30),
                Location = new Point(15, 10),
                FlatStyle = FlatStyle.Flat
            };
            btnLoginSettings.FlatAppearance.BorderSize = 0;
            btnLoginSettings.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnLoginSettings.Click += async (s, args) => {
                settingsOptionsForm.Close();
                try
                {
                    using var cts = new CancellationTokenSource();
                    await ApplyGameSettings(mainForm.webView2, cts.Token);
                    MessageBox.Show("登录界面设置完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"登录界面设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            settingsOptionsForm.Controls.Add(btnLoginSettings);

            // 添加"农场界面"按钮
            Button btnFarmSettings = new Button()
            {
                Text = "农场界面",
                Size = new Size(160, 30),
                Location = new Point(15, 50),
                FlatStyle = FlatStyle.Flat
            };
            btnFarmSettings.FlatAppearance.BorderSize = 0;
            btnFarmSettings.FlatAppearance.MouseOverBackColor = Color.LightBlue;
            btnFarmSettings.Click += async (s, args) => {
                settingsOptionsForm.Close();
                try
                {
                    using var cts = new CancellationTokenSource();
                    
                    // 点击更多按钮
                    await ClickPoint(mainForm.webView2, new Point(580, 506));
                    await Task.Delay(500, cts.Token);
                    
                    // 点击设置
                    await ClickPoint(mainForm.webView2, new Point(550, 393));
                    await Task.Delay(500, cts.Token);
                    
                    // 点击帧率选择标准
                    await ClickPoint(mainForm.webView2, new Point(425, 300));
                    await Task.Delay(500, cts.Token);
                    
                    // 点击分辨率选择低
                    await ClickPoint(mainForm.webView2, new Point(425, 360));
                    await Task.Delay(500, cts.Token);
                    
                    // 点击游戏
                    await ClickPoint(mainForm.webView2, new Point(75, 220));
                    await Task.Delay(500, cts.Token);
                    
                    // 关闭镜头辅助
                    await ClickPoint(mainForm.webView2, new Point(398, 229));
                    await Task.Delay(500, cts.Token);
                    
                    // 点击返回
                    await ClickPoint(mainForm.webView2, new Point(36, 44));
                    
                    MessageBox.Show("农场界面设置完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"农场界面设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            settingsOptionsForm.Controls.Add(btnFarmSettings);

            settingsOptionsForm.ShowDialog();
        }
        

        // "定时任务"按钮点击事件：打开定时任务设置窗口
        public static void BtnScheduledTasks_Click(object? sender, EventArgs e)
        {
            using (var form = new ScheduledTasksForm())
            {
                form.ShowDialog();
            }
        }


        //关闭画质增强，设置标清，关闭音量操作
        public static async Task ApplyGameSettings(WebView2 webView, CancellationToken token)
        {
            try
            {
                // 确保WebView2已初始化
                if (webView == null || webView.CoreWebView2 == null)
                {
                    Console.WriteLine("WebView2 is not initialized");
                    return;
                }

                // 1. 设置标清模式
                string setLowQualityJs = @"
                    document.querySelector('#cloudGameMenu > div > div.system-menu-top > div:nth-child(2) > div > div > ul:nth-child(2) > li:nth-child(2)').click();
                ";
                
                // 2. 关闭音量
                string muteVolumeJs = @"
                    document.querySelector('#cloudGameMenu > div > div.system-menu-top > div.status-set-div.menu-voice-status > span').click();
                ";
                
                // 3. 关闭画质增强
                string disableQualityEnhancementJs = @"
                    document.querySelector('#cloudGameMenu > div > div.system-menu-top > div:nth-child(2) > div > div > ul:nth-child(4) > li:nth-child(2)').click();
                ";

                // 执行JavaScript代码
                await webView.ExecuteScriptAsync(setLowQualityJs);
                await Task.Delay(500, token); // 短暂延迟确保操作完成
                
                await webView.ExecuteScriptAsync(muteVolumeJs);
                await Task.Delay(500, token);
                
                await webView.ExecuteScriptAsync(disableQualityEnhancementJs);
                await Task.Delay(500, token);
                
                Console.WriteLine("Game settings applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying game settings: {ex.Message}");
            }
        }

    }
}
