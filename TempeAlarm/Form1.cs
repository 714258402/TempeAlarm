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
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);
        float CPUTempe = 0;
        string TopCore = "Core?";
        UpdateVisitor updateVisitor = new UpdateVisitor();
        Computer computer = new Computer();
        System.Drawing.Icon iconTemp = null;
        MessageBoxTimeOut mb = new MessageBoxTimeOut();
        string Wordstr ="00";
        Int32 Red_color_F = 0, Green_color_F = 0, SizeOfFontbit = 32;
        Size TextSize;
        Bitmap bTemp;
        System.IntPtr iconHandle;
        Font Myfont;



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
                Invoke((MethodInvoker)delegate
                {
                    this.Hide();//最小化时窗体隐藏
                });
            }
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal; //还原窗体
                }
                else if (this.WindowState == FormWindowState.Normal)
                {
                    this.WindowState = FormWindowState.Minimized;//最小化
                    this.Hide();
                }
            });
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Myfont = new Font(new FontFamily("Arial"), SizeOfFontbit, FontStyle.Bold);
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 1000; //执行间隔时间,单位为毫秒; 这里实际间隔为1s  
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(CheckTempe);
            System.Timers.Timer tm = new System.Timers.Timer(1000);
            tm.AutoReset = false;
            tm.Enabled = true;
            tm.Elapsed += new ElapsedEventHandler(Miniwindow);

        }
        private void Miniwindow(object source, ElapsedEventArgs e)
        {
            Invoke((MethodInvoker)delegate { this.WindowState = FormWindowState.Minimized; });
            ((System.Timers.Timer)source).Enabled = false;
        }

        private void CheckTempe(object source, ElapsedEventArgs e)//定时器回调函数
        {
            ((System.Timers.Timer)source).Enabled = false;


            CPUTempe = 0;

            computer.Open();
            computer.Accept(updateVisitor);
            int i = 0, j = 0;
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
                            if (computer.Hardware[i].Sensors[j].Value.Value > CPUTempe)
                            {
                                CPUTempe = computer.Hardware[i].Sensors[j].Value.Value;
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
                mb.Show(9000, TopCore + "温度：" + CPUTempe.ToString() + "℃", "警告(窗体9秒后自动关闭...)");
            }

            Wordstr = ((Int32)CPUTempe).ToString();
            Red_color_F = Convert.ToInt32(CPUTempe * 15 - 660.0f);
            Green_color_F = Convert.ToInt32(1140.0f - CPUTempe * 15);
            if (Red_color_F > 254) Red_color_F = 254;
            if (Red_color_F < 1) Red_color_F = 1;
            if (Green_color_F > 254) Green_color_F = 254;
            if (Green_color_F < 1) Green_color_F = 1;

            /*try
            {
                TextSize = TextRenderer.MeasureText(Wordstr, Myfont);
                bTemp = Form1.DrawTextBmp(Wordstr, Myfont, Color.FromArgb(Red_color_F, Green_color_F, 0), TextSize, 0, 0, 0, 0);
                bTemp = Form1.ClearWhite(bTemp);
                iconHandle = bTemp.GetHicon();
                iconTemp = Icon.FromHandle(iconHandle);
            }
            catch { } */
            TextSize = TextRenderer.MeasureText(Wordstr, Myfont);
            bTemp = Form1.DrawTextBmp(Wordstr, Myfont, Color.FromArgb(Red_color_F, Green_color_F, 0), TextSize, 0, 0, 0, 0);
            bTemp = Form1.ClearWhite(bTemp);
            iconHandle = bTemp.GetHicon();
            iconTemp = Icon.FromHandle(iconHandle);
            Invoke(
                (MethodInvoker)delegate
                {
                    this.notifyIcon1.Icon = iconTemp;
                });
            DestroyIcon(iconTemp.Handle);
            ((System.Timers.Timer)source).Enabled = true;

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
            Invoke((MethodInvoker)delegate
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;//显示窗口
            });
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
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

        private static Bitmap DrawTextBmp(string ch, Font font, Color color, Size TextSize, int x, int y, int w, int h)
        {
            //创建此大小的图片
            Bitmap bmp = new Bitmap(TextSize.Width - x, TextSize.Height - y);
            //使用GDI+绘制
            Graphics g = Graphics.FromImage(bmp);
            bmp = new Bitmap(TextSize.Width - x, TextSize.Height - y, System.Drawing.Imaging.PixelFormat.Format64bppArgb);//PixelFormat.Format64bppArgb);
            g = Graphics.FromImage(bmp);
            g.Clear(Color.Black);


            g.DrawString(ch, font, new SolidBrush(color), new PointF(w, h));
            g.Save();
            g.Dispose();
            //返回图像
            return bmp;
        }

        //去白边
        private static Bitmap ClearWhite(Bitmap bm)
        {
            int y_l = 0;//左边
            int y_r = 0;//右边
            int i_h = 0;//上边
            int i_d = 0;//下边
            #region 计算----
            for (int i = 0; i < bm.Width; i++)
            {
                for (int y = 0; y < bm.Height; y++)
                {
                    if (bm.GetPixel(i, y).R != 0 || bm.GetPixel(i, y).B != 0 || bm.GetPixel(i, y).G != 0)
                    {
                        y_l = i;
                        goto yl;
                    }
                }
            }
        yl:
            for (int i = 0; i < bm.Width; i++)
            {
                for (int y = 0; y < bm.Height; y++)
                {
                    if (bm.GetPixel(bm.Width - i - 1, y).R != 0 || bm.GetPixel(bm.Width - i - 1, y).B != 0 || bm.GetPixel(bm.Width - i - 1, y).G != 0)
                    {
                        y_r = i;
                        goto yr;
                    }
                }
            }
        yr:
            for (int i = 0; i < bm.Height; i++)
            {
                for (int y = 0; y < bm.Width; y++)
                {
                    if (bm.GetPixel(y, i).R != 255 || bm.GetPixel(y, i).B != 255 || bm.GetPixel(y, i).G != 255)
                    {
                        i_h = i;
                        goto ih;
                    }
                }
            }
        ih:
            for (int i = 0; i < bm.Height; i++)
            {
                for (int y = 0; y < bm.Width; y++)
                {
                    if (bm.GetPixel(y, bm.Height - i - 1).R != 255 || bm.GetPixel(y, bm.Height - i - 1).B != 255 || bm.GetPixel(y, bm.Height - i - 1).G != 255)
                    {
                        i_d = i;
                        goto id;
                    }
                }
            }
        id:
            #endregion


            //创建此大小的图片
            Bitmap bmp = new Bitmap(bm.Width - y_l - y_r, bm.Height - i_h - i_d);
            Graphics g = Graphics.FromImage(bmp);
            //(new Point(y_l, i_h), new Point(0, 0), new Size(bm.Width - y_l - y_r, bm.Height - i_h - i_d));
            Rectangle sourceRectangle = new Rectangle(y_l, i_h, bm.Width - y_l - y_r, bm.Height - i_h - i_d);
            Rectangle resultRectangle = new Rectangle(0, 0, bm.Width - y_l - y_r, bm.Height - i_h - i_d);
            g.DrawImage(bm, resultRectangle, sourceRectangle, GraphicsUnit.Pixel);
            g.Dispose();
            return bmp;
        }

    }



}







