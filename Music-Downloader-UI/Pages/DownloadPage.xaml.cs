using MusicDownloader.Json;
using MusicDownloader.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace MusicDownloader
{
    /// <summary>
    /// DownloadPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadPage : Page
    {
        List<ListModel> listitem = new List<ListModel>();
        Music music = null;

        class ListModel : INotifyPropertyChanged
        {
            [DisplayName("标题")]
            public string Title { get; set; }
            [DisplayName("歌手")]
            public string Singer { get; set; }
            [DisplayName("专辑")]
            public string Album { get; set; }
            [DisplayName("状态")]
            public string State { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public DownloadPage(Music m)
        {
            InitializeComponent();
            //Thread th_Update = new Thread(UpdateList);
            //th_Update.Start();
            music = m;
            music.UpdateDownloadPage += UpdateList;
        }

        public void UpdateList()
        {
            bool isadd = false;
            for (int i = 0; i < music.downloadlist.Count; i++)
            {
                bool exist = false;
                foreach (ListModel l in listitem)
                {
                    if (l.Title == music.downloadlist[i].Title && l.Singer == music.downloadlist[i].Singer && l.Album == music.downloadlist[i].Album)
                    {
                        l.State = music.downloadlist[i].State;
                        l.OnPropertyChanged("State");
                        exist = true;
                    }
                }
                if (!exist)
                {
                    isadd = true;
                    listitem.Add(new ListModel
                    {
                        Album = music.downloadlist[i].Album,
                        Singer = music.downloadlist[i].Singer,
                        State = music.downloadlist[i].State,
                        Title = music.downloadlist[i].Title
                    }
                    );
                }
            }
            if (isadd)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    List.ItemsSource = listitem;
                    List.Items.Refresh();
                }));
            }
        }

        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(music.setting.SavePath);
        }

        private void Label_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            for (int x = 0; x < 10; x++)
            {
                for (int i = 0; i < listitem.Count; i++)
                {
                    if (listitem[i].State == "下载完成" || listitem[i].State == "无版权" || listitem[i].State == "下载错误" || listitem[i].State == "音乐已存在")
                    {
                        listitem.RemoveAt(i);
                    }
                }
            }
            List.ItemsSource = listitem;
            List.Items.Refresh();
        }
    }
}
