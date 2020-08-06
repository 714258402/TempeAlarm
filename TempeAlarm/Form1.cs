using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Timers;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor;
using System.Runtime.InteropServices;

namespace TempeAlarm
{

    public partial class Form1 : Form
    {
        static Double CPUTempe = 0;
        static string TopCore = "Core?";
        UpdateVisitor updateVisitor = new UpdateVisitor();
        Computer computer = new Computer();
        int i = 0, j = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                //notifyIcon1.Visible = true; //托盘图标
            }
            if (this.WindowState == FormWindowState.Minimized)//最小化事件
            {
                //notifyIcon1.Visible = true;//托盘图标
                Invoke((MethodInvoker)delegate {
                    this.Hide();//最小化时窗体隐藏
                });
                }
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Invoke((MethodInvoker)delegate {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal; //还原窗体
                }
            });
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 10000; //执行间隔时间,单位为毫秒; 这里实际间隔为10s  
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(CheckTempe);
            System.Timers.Timer tm = new System.Timers.Timer(1000);
            tm.AutoReset = false;
            tm.Enabled = true;
            tm.Elapsed += new ElapsedEventHandler(Miniwindow);
        }
        private void Miniwindow(object source, ElapsedEventArgs e)
        {
            Invoke((MethodInvoker)delegate {this.WindowState = FormWindowState.Minimized; });
            ((System.Timers.Timer)source).Enabled = false;
        }

        private void CheckTempe(object source, ElapsedEventArgs e)
        {
            CPUTempe = 0;

            computer.Open();
            computer.Accept(updateVisitor);

            for (i = 0; i < computer.Hardware.Length; i++)
            {
                //循环找到HardwareType为cpu
                if ((computer.Hardware[i].HardwareType == HardwareType.CPU) || (computer.Hardware[i].HardwareType == HardwareType.GpuNvidia) || (computer.Hardware[i].HardwareType == HardwareType.GpuAti))   //(computer.Hardware[i].HardwareType == HardwareType.CPU) ||
                {
                    for (j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {

                        //找到温度
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {
                            if (Convert.ToDouble(computer.Hardware[i].Sensors[j].Value.ToString()) > CPUTempe)
                            {
                                CPUTempe = Convert.ToDouble(computer.Hardware[i].Sensors[j].Value.ToString());
                                TopCore = computer.Hardware[i].Sensors[j].Name;
                            }
                        }
                    }
                }
            }
            Invoke(
                (MethodInvoker)delegate
                { label3.Text = TopCore + "温度：" + CPUTempe.ToString() + "℃"; });
            if (CPUTempe > Convert.ToDouble(numericUpDown1.Value))
            {
                Console.Beep(350, 2000);
                MessageBoxTimeOut mb = new MessageBoxTimeOut();
                mb.Show(9000, TopCore + "温度：" + CPUTempe.ToString() + "℃", "警告(窗体9秒后自动关闭...)");
            }
            
        }


        private class UpdateVisitor : IVisitor
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

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate {
                this.Show();
                this.WindowState = FormWindowState.Normal;//显示窗口
            });
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate {
                this.WindowState = FormWindowState.Minimized;//最小化
                this.Hide();
            });
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Application.Exit();//退出程序
        }

        private class MessageBoxTimeOut
        {
            private string _caption;

            public void Show(string text, string caption)
            {
                Show(3000, text, caption);
            }
            public void Show(int timeout, string text, string caption)
            {
                Show(timeout, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            public void Show(int timeout, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
            {
                this._caption = caption;
                StartTimer(timeout);
                MessageBox.Show(text, caption, buttons, icon);
            }
            private void StartTimer(int interval)
            {
                System.Timers.Timer ktimer = new System.Timers.Timer();
                ktimer.Interval = interval;
                ktimer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Tick);
                ktimer.Enabled = true;
            }
            private void Timer_Tick(object sender, EventArgs e)
            {
                KillMessageBox();
                //停止计时器
                ((System.Timers.Timer)sender).Enabled = false;
            }
            [DllImport("User32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
            private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
            public const int WM_CLOSE = 0x10;
            private void KillMessageBox()
            {
                //查找MessageBox的弹出窗口,注意对应标题
                IntPtr ptr = FindWindow(null, this._caption);
                if (ptr != IntPtr.Zero)
                {
                    //查找到窗口则关闭
                    PostMessage(ptr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }


    }



}
