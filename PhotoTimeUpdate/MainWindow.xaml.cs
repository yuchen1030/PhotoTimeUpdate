using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing.Imaging;
//using Ookii.Dialogs.Wpf;
using PhotoTimeUpdate.Services;
using System.Windows.Threading;
using System.Threading;

namespace PhotoTimeUpdate
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TextBox_StartT.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            TextBox_EndT.Text = DateTime.Now.AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss");
            TextBox_Gap.Text = "-1";
            Loaded += new RoutedEventHandler(Window1_Loaded);
        }


        private void Button_Compute_Click(object sender, RoutedEventArgs e)
        {
            string computePath = TextBox_Compute.Text;
            if (!string.IsNullOrEmpty(computePath))
            {
                TextBox_StartT.Text = "";
                TextBox_EndT.Text = "";
                TextBox_Gap.Text = "";
                if (!Directory.Exists(computePath))
                {
                    MessageBox.Show("您输入的文件夹【" + computePath + "】不存在"  );
                    return;
                }
                else
                {
                    var files = Directory.GetFiles(computePath).Take(2).ToList();
                    if (files.Count == 2)
                    {
                        try
                        {
                            TextBox_StartT.Text = ExifFix.GetTakePicDate(files[0]);
                            TextBox_EndT.Text = ExifFix.GetTakePicDate(files[1]);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("请确保您的文件是图片格式:" + ex.Message);
                            return;
                        }
                    }

                }
            }
            try
            {
                TextBox_Gap.Text = (Convert.ToDateTime(TextBox_StartT.Text) - Convert.ToDateTime(TextBox_EndT.Text)).TotalSeconds.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("请确保时间格式正确:" + ex.Message);
            }


        }



        int cnt = 3;
        private void Button_UpdateTime_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBox_PicFolder.Text))
            {
                TextBox_PicFolder.Text = @"D:\BaiduYunDownload\2022.10.2婚照原片";
            }
            Label_UpdateResult.Text = "尚未有执行结果。";
            UpdatePictureTime();
            cnt++;
        }

        private void UpdatePictureTime()
        {
            string folder = TextBox_PicFolder.Text;
            if (!string.IsNullOrWhiteSpace(folder))
            {
                if (!Directory.Exists(folder))
                {
                    MessageBox.Show("您输入的文件夹【" + folder + "】不存在，请确认");
                    return;
                }
                Int64 secs = 0;
                try
                {
                    secs = Convert.ToInt64(TextBox_Gap.Text.Trim());
                }
                catch
                {

                }
                if (secs == 0)
                {
                    MessageBox.Show("时间差值为0，无需修改，请确保您输入了正确的差值秒数");
                    return;
                }
                var files = Directory.GetFiles(folder, "*.*").Count();

                if (System.Windows.MessageBox.Show("您确认要对该目录下的【" + files + "】张照片的拍摄时间加上【" + secs + "】秒嘛？", "提示：", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    timer.Start();
                    ThreadPool.QueueUserWorkItem(_ => UpdatePhotoInfo(files, folder, secs));
                }
            }
        }


        void UpdatePhotoInfo(int files, string folder, Int64 secs)
        {
            UpdateUiText(Label_UpdateResult, "执行中，请稍候......");
            //Label_UpdateResult.Text = "执行中，请稍候......";
            DateTime st = DateTime.Now;
            List<String> returnMsg = ExifFix.FixExifDateTime(folder, secs);
            DateTime et = DateTime.Now;
            var spanStr = (et - st).TotalMinutes.ToString("f2");
            UpdateUiText(Label_UpdateResult, "耗时" + spanStr + "分，执行完成，请参见下方文本框的结果。");
            //Label_UpdateResult.Text = "耗时" + spanStr + "分，执行完成，请参见下方文本框的结果。";
            string msg = files + "张原始图片的拍摄时间保存于【" + returnMsg[0] + "】路径下，已完成了【" + returnMsg[1] + "】张图片的时间更新，总耗时" + spanStr + "分(" + st.ToString("HH:mm:ss") + "~" + et.ToString("HH:mm:ss") + ")";
            //TextBox_UpdateResult.Text = msg + Environment.NewLine + Environment.NewLine + returnMsg[2];
            UpdateUiTextBox(TextBox_UpdateResult, msg + Environment.NewLine + Environment.NewLine + returnMsg[2]);
            timer.Stop();
            timerCnt = 0;
        }




        private delegate void UpdateUiTextDelegate(TextBlock control, string text);
        private void UpdateUiText(TextBlock control, string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(UpdateUiText), control, text);
                return;
            }
            control.Text = text;
        }


        private delegate void UpdateUiTextBoxDelegate(TextBox control, string text);
        private void UpdateUiTextBox(TextBox control, string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextBoxDelegate(UpdateUiTextBox), control, text);
                return;
            }
            control.Text = text;
        }


        private DispatcherTimer timer;



        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer1_Tick;

        }
        int timerCnt = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            timerCnt++;
            UpdateUiText(Label_UpdateResult, "执行中，请稍候......已执行 " + timerCnt + " 秒");

        }



    }
}
