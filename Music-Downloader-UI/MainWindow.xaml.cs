using MusicDownloader.Json;
using MusicDownloader.Library;
using MusicDownloader.Pages;
using Panuon.UI.Silver;
using Panuon.UI.Silver.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MusicDownloader
{
    public partial class MainWindow : WindowX
    {
        Music music = null;
        Setting setting;
        List<DownloadList> downloadlist = new List<DownloadList>();
        Page HomePage;
        Page DownloadPage;
        Page SettingPage;
        Page Donate = new Donate();

        #region 界面
        private void BlogButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://www.nitian1207.cn/");
        private void LeftMenu_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (frame != null)
            {
                switch (((System.Windows.Controls.HeaderedItemsControl)e.NewValue).Header)
                {
                    case "主页":
                        frame.Content = HomePage;
                        break;
                    case "下载":
                        frame.Content = DownloadPage;
                        break;
                    case "设置":
                        frame.Content = SettingPage;
                        break;
                    case "赞助":
                        frame.Content = Donate;
                        break;
                    case "反馈":
                        Process.Start("https://docs.qq.com/form/edit/DT0RraHhRZXRmYlVY");
                        break;
                }
            }
        }
        #endregion

        #region 事件
        private void NotifyUpdate()
        {
            var result = MessageBoxX.Show("检测到新版,是否更新", "提示:", Application.Current.MainWindow, MessageBoxButton.YesNo, new MessageBoxXConfigurations()
            {
                MessageBoxIcon = MessageBoxIcon.Warning
            });
            if (result == MessageBoxResult.Yes)
            {
                Process.Start("https://www.nitian1207.cn/archives/496");
            }
        }

        private void NotifyError()
        {
            var result = MessageBoxX.Show("连接服务器错误", "提示:", Application.Current.MainWindow, MessageBoxButton.OK, new MessageBoxXConfigurations()
            {
                MessageBoxIcon = MessageBoxIcon.Error
            });
            Environment.Exit(0);
        }
        #endregion

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            setting = new Setting()
            {
                SavePath = Tool.Config.Read("SavePath") ?? Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                DownloadQuality = Tool.Config.Read("DownloadQuality") ?? "999000",
                IfDownloadLrc = Boolean.Parse(Tool.Config.Read("IfDownloadLrc") ?? "true"),
                IfDownloadPic = Boolean.Parse(Tool.Config.Read("IfDownloadPic") ?? "true"),
                SaveNameStyle = int.Parse(Tool.Config.Read("SaveNameStyle") ?? "0"),
                SavePathStyle = int.Parse(Tool.Config.Read("SavePathStyle") ?? "0"),
                SearchQuantity = Tool.Config.Read("SearchQuantity") ?? "100"
            };
            music = new Music(setting, NotifyError, NotifyUpdate);
            HomePage = new SearchPage(music, setting);
            DownloadPage = new DownloadPage(music);
            SettingPage = new SettingPage(setting);
            InitializeComponent();
            frame.Content = HomePage;
            string ver = "";
            foreach (int s in music.version)
            {
                ver += s.ToString() + ".";
            }
            VerTextblock.Text = ver.Substring(0, ver.Length - 1);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (File.Exists("Error.log"))
            {
                StreamReader sr = new StreamReader("Error.log");
                StreamWriter sw = new StreamWriter("Error.log");
                sw.WriteLine(sr.ReadToEnd() + "\r\n" + e.ExceptionObject.ToString());
                sw.Flush();
                sw.Close();
            }
            else
            {
                StreamWriter sw = new StreamWriter("Error.log");
                sw.WriteLine(e.ExceptionObject.ToString());
                sw.Flush();
                sw.Close();
            }
            MessageBox.Show("遇到未知错误，具体信息查看 " + Environment.CurrentDirectory + "\\Error.log");
        }

        private void WindowX_ContentRendered(object sender, EventArgs e)
        {
            music.Update();
        }
    }
}
