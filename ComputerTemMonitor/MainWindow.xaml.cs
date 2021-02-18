using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ComputerTemMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public SeriesCollection seriesCollection { get; set; }
        private LineSeries templineseries { get; set; }
        private LineSeries loadlineseries { get; set; }
        private NotifyIcon _notifyIcon = null;
        public bool chartVisibility = true;
        private System.Timers.Timer timer = new System.Timers.Timer();
        private delegate void SetTextCallback(List<float?> text);
        public MainWindow()
        {
            timer.Interval = 3000;
            timer.Enabled = true;
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            InitializeComponent();
            //var compositor = new WindowAccentCompositor(this);
            //compositor.Color = System.Windows.Media.Color.FromArgb(0x32, 0xff, 0xff, 0xff);
            //compositor.IsEnabled = true;
            InitialTray();
            timer.Start();
            InitChart();
        }

        private void InitChart() {
            loadlineseries = new LineSeries();
            templineseries = new LineSeries();
            templineseries.LineSmoothness = 0;
            loadlineseries.LineSmoothness = 0;
            templineseries.PointGeometry = null;
            loadlineseries.PointGeometry = null;
            templineseries.Values = new ChartValues<float>();
            loadlineseries.Values = new ChartValues<float>();
            templineseries.Title = "温度";
            loadlineseries.Title = "负载";
            seriesCollection = new SeriesCollection();
            seriesCollection.Add(loadlineseries);
            seriesCollection.Add(templineseries);
            DataContext = this;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(()=> {
                IntPtr windowHandle = new WindowInteropHelper(this).EnsureHandle();
                var handleList = getFrontWindow(windowHandle);
                foreach (var handle in handleList)
                {
                    //Window overlapWindow = (Window)System.Windows.Interop.HwndSource.FromHwnd(handle).RootVisual;
                    if (IsZoomed(handle))
                    {
                        //timer.Stop();
                        return;
                    }
                }
                var Info = GetComputerInfo();
                Load.Text = string.Format("{0:f0}", Info[0]);
                Temperature.Text = string.Format("{0:f0}", Info[1]);
                seriesCollection[0].Values.Add(Info[0]);
                if (seriesCollection[0].Values.Count > 6)
                {
                    seriesCollection[0].Values.RemoveAt(0);
                }
                seriesCollection[1].Values.Add(Info[1]);
                if (seriesCollection[1].Values.Count > 6)
                {
                    seriesCollection[1].Values.RemoveAt(0);
                }
            });
        }

        private List<float?> GetComputerInfo()
        {
            var result = new List<float?>() { 0,0};
            UpdateVisitor updateVisitor = new UpdateVisitor();
            Computer computer = new Computer();
            computer.Open();
            while (computer==null){}
            computer.CPUEnabled = true;
            //computer.GPUEnabled = true;
            //computer.HDDEnabled = true;
            //computer.FanControllerEnabled = true;
            //computer.MainboardEnabled = true;
            //computer.RAMEnabled = true;
            computer.Accept(updateVisitor);
            foreach (var item in computer.Hardware)
            {
                if (item.HardwareType == HardwareType.CPU) {
                    foreach (var sensor in item.Sensors)
                    {
                        switch (sensor.SensorType)
                        {
                            case SensorType.Temperature:
                                if (sensor.Name== "CPU Package") {
                                    result[1] = sensor.Value;
                                    //result += sensor.Name + "平均温度：" + sensor.Value + "\n";
                                }
                                break;
                            case SensorType.Load:
                                if (sensor.Name == "CPU Total")
                                {
                                    result[0] = sensor.Value;
                                    //result += sensor.Name + "负载：" + sensor.Value + "\n";
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                #region
                //switch (item.HardwareType)
                //{
                //    case HardwareType.Mainboard:
                //        result += "主板信息：\n";
                //        break;
                //    case HardwareType.SuperIO:
                //        result += "SuperIO信息：\n";
                //        break;
                //    case HardwareType.CPU:
                //        result += "CPU信息：\n";
                //        break;
                //    //case HardwareType.RAM:
                //    //    result += "RAM信息：\n";
                //    //    break;
                //    //case HardwareType.GpuNvidia:
                //    //    result += "显卡信息：\n";
                //    //    break;
                //    //case HardwareType.GpuAti:
                //    //    result += "显卡信息：\n";
                //    //    break;
                //    //case HardwareType.TBalancer:
                //    //    //result += "主板信息：\n";
                //    //    break;
                //    //case HardwareType.Heatmaster:
                //    //    //result += "主板信息：\n";
                //    //    break;
                //    //case HardwareType.HDD:
                //    //    result += "硬盘信息：\n";
                //    //    break;
                //    default:
                //        break;
                //}
                //result += "设备名称："+item.Name + "\n";
                #endregion
                
            }
            computer.Close();
            return result;
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void InitialTray()
        {
            //隐藏主窗体
            //this.Visibility = Visibility.Hidden;
            //设置托盘的各个属性
            _notifyIcon = new NotifyIcon();
            //_notifyIcon.BalloonTipText = "TD自动侦测服务后台运行中...";//托盘气泡显示内容
           // _notifyIcon.Text = "TDDetector";
            _notifyIcon.Visible = true;//托盘按钮是否可见
            _notifyIcon.Icon = new Icon(@"ico.ico");//托盘中显示的图标
            //_notifyIcon.ShowBalloonTip(1000);//托盘气泡显示时间
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            var showChartItem = new System.Windows.Forms.ToolStripMenuItem();
            showChartItem.Text = "显示详细图表";
            showChartItem.CheckOnClick = true;
            showChartItem.Checked = true;
            showChartItem.CheckState = CheckState.Checked;
            showChartItem.CheckedChanged += ShowChartItem_CheckedChanged;

            var checkSpeedItem = new ToolStripMenuItem();
            checkSpeedItem.Text = "数据间隔时长";

            var speedItem_1s = new ToolStripMenuItem();
            var speedItem_3s = new ToolStripMenuItem();
            var speedItem_5s = new ToolStripMenuItem();
            var speedItem_10s = new ToolStripMenuItem();
            speedItem_1s.Text = "1s";
            speedItem_3s.Text = "3s";
            speedItem_5s.Text = "5s";
            speedItem_10s.Text = "10s";
            speedItem_1s.CheckOnClick = true;
            speedItem_5s.CheckOnClick = true;
            speedItem_10s.CheckOnClick = true;
            speedItem_3s.CheckOnClick = true;
            speedItem_3s.Checked = true;
            speedItem_3s.CheckState = CheckState.Checked;
            speedItem_1s.Click += SpeedItem_1s_Click; ;
            speedItem_3s.Click += SpeedItem_3s_Click; ;
            speedItem_5s.Click += SpeedItem_5s_Click; ;
            speedItem_10s.Click += SpeedItem_10s_Click; ;

            checkSpeedItem.DropDownItems.Add(speedItem_1s);
            checkSpeedItem.DropDownItems.Add(speedItem_3s);
            checkSpeedItem.DropDownItems.Add(speedItem_5s);
            checkSpeedItem.DropDownItems.Add(speedItem_10s);

            var alwaysTopItem = new ToolStripMenuItem();
            alwaysTopItem.CheckOnClick = true;
            alwaysTopItem.CheckedChanged += AlwaysTopItem_CheckedChanged;
            alwaysTopItem.Text = "窗口置顶";

            ToolStripMenuItem exitItem = new ToolStripMenuItem();
            exitItem.Text = "退出";
            exitItem.Click += new EventHandler(Exit_Click);
            contextMenu.Items.Add(checkSpeedItem);
            contextMenu.Items.Add(showChartItem);
            contextMenu.Items.Add(alwaysTopItem);
            contextMenu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            //窗体状态改变时触发
            //this.StateChanged += MainWindow_StateChanged;
        }

        private void AlwaysTopItem_CheckedChanged(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Checked)
            {
                this.Topmost = true;
            }
            else {
                this.Topmost = false;
            }
        }

        private void SpeedItem_10s_Click(object sender, EventArgs e)
        {
            setSpeedMenuItemCheckStateAndTimer((ToolStripMenuItem)sender,10000);
        }

        private void SpeedItem_5s_Click(object sender, EventArgs e)
        {
            setSpeedMenuItemCheckStateAndTimer((ToolStripMenuItem)sender, 5000);
        }

        private void SpeedItem_3s_Click(object sender, EventArgs e)
        {
            setSpeedMenuItemCheckStateAndTimer((ToolStripMenuItem)sender, 3000);
        }

        private void SpeedItem_1s_Click(object sender, EventArgs e)
        {
            setSpeedMenuItemCheckStateAndTimer((ToolStripMenuItem)sender, 1000);
        }

        private void setSpeedMenuItemCheckStateAndTimer(ToolStripMenuItem menuItem,int interval) {
            var parent = menuItem.GetCurrentParent();
            foreach (ToolStripMenuItem item in parent.Items)
            {
                item.Checked = false;
                item.CheckState = CheckState.Unchecked;
            }
            menuItem.Checked = true;
            menuItem.CheckState = CheckState.Checked;
            timer.Interval = interval;
        }
        private void ShowChartItem_CheckedChanged(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Checked)
            {
                chartGrid.Visibility = Visibility.Visible;
                this.Height = 100;
                this.Width = 320;
                Load.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                Load.FontSize = 25;
                Temperature.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                Temperature.FontSize = 15;
            }
            else {
                chartGrid.Visibility = Visibility.Collapsed;
                this.Height = 100;
                this.Width = 100;
                Load.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                Load.FontSize = 35;
                Temperature.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                Temperature.FontSize = 25;
            }
        }

        private void Setting_Click(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            timer.Stop();
            Environment.Exit(0);
        }

        private void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Activate();
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!this.IsActive)
            {
                timer.Stop();
            }
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!timer.Enabled)
            {
                timer.Start();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            //IntPtr windowHandle = new WindowInteropHelper(this).EnsureHandle();
            //var handleList = getFrontWindow(windowHandle);
            //foreach (var handle in handleList)
            //{
            //    //Window overlapWindow = (Window)System.Windows.Interop.HwndSource.FromHwnd(handle).RootVisual;
            //    if (IsZoomed(handle))
            //    {
            //        timer.Stop();
            //        return;
            //    }
            //}
        }
        private List<IntPtr> getFrontWindow(IntPtr windowHandle) {
            var IntPtrList = new List<IntPtr>();
            IntPtr overlapWindow = GetWindow(windowHandle, 3);
            while (overlapWindow != null && overlapWindow != IntPtr.Zero)
            {
                if (IsWindowVisible(overlapWindow)) {
                    IntPtrList.Add(overlapWindow);
                }
                overlapWindow = GetWindow(overlapWindow, 3);
            }
            return IntPtrList;
        }
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint wWcmd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);
    }
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
