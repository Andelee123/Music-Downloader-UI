﻿using MusicDownloader.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static MusicDownloader.Library.Tool;

namespace MusicDownloader.Library
{
    public class Music
    {
        public List<int> version = new List<int> { 1, 1, 1 };
        const string NeteaseApiUrl = "";//自行搭建接口
        const string QQApiUrl = "";//自行搭建接口
        public Setting setting;
        public List<DownloadList> downloadlist = new List<DownloadList>();
        string cookie = "";
        Thread th_Download;
        public delegate void UpdateDownloadPageEventHandler();
        public delegate void NotifyUpdateEventHandler();
        public delegate void NotifyConnectErrorEventHandler();
        public event UpdateDownloadPageEventHandler UpdateDownloadPage;
        public event NotifyUpdateEventHandler NotifyUpdate;
        public event NotifyConnectErrorEventHandler NotifyConnectError;
        bool wait = false;

        /// <summary>
        /// 获取更新数据 这个方法是获取程序更新信息 二次开发请修改
        /// </summary>
        /// <returns></returns>
        public void Update()
        {
            WebClientPro wc = new WebClientPro();
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(wc.OpenRead(""));
                // 读取一个在线文件判断接口状态获取网易云音乐Cookie,可以写死
            }
            catch
            {
                NotifyConnectError();
            }
            Update update = JsonConvert.DeserializeObject<Update>(sr.ReadToEnd());
            cookie = update.Cookie;
            bool needupdate = true;

            if (update.Version[0] < version[0])
            {
                needupdate = false;
            }
            else if (update.Version[0] == version[0])
            {
                if (update.Version[1] < version[1])
                {
                    needupdate = false;
                }
                else if (update.Version[1] == version[1])
                {
                    if (update.Version[2] < version[2])
                    {
                        needupdate = false;
                    }
                    else if (update.Version[2] == version[2])
                    { 
                        needupdate = false; 
                    }
                }
            }
            if (needupdate)
            {
                NotifyUpdate();
            }
        }

        /// <summary>
        /// 构造函数 需要提供设置参数
        /// </summary>
        /// <param name="setting"></param>
        public Music(Setting setting, NotifyConnectErrorEventHandler ConnectErrorEventHandler, NotifyUpdateEventHandler UpdateEventHandler)
        {
            this.setting = setting;
            NotifyConnectError += ConnectErrorEventHandler;
            NotifyUpdate += UpdateEventHandler;
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="api">1.网易云 2.QQ</param>
        /// <returns></returns>
        public List<MusicInfo> Search(string Key, int api)
        {
            if (api == 1)
            {
                try
                {
                    List<MusicInfo> searchItem = new List<MusicInfo>();
                    string key = Key;
                    int quantity = Int32.Parse(setting.SearchQuantity);
                    int pagequantity = quantity / 100;
                    int remainder = quantity % 100;

                    if (remainder == 0)
                    {
                        remainder = 100;
                    }
                    if (pagequantity == 0)
                    {
                        pagequantity = 1;
                    }
                    for (int i = 0; i < pagequantity; i++)
                    {
                        if (i == pagequantity - 1 && pagequantity >= 1)
                        {
                            List<MusicInfo> Mi = NeteaseSearch(key, i + 1, remainder);
                            if (Mi != null)
                            {
                                searchItem.AddRange(Mi);
                            }
                        }
                        else
                        {
                            List<MusicInfo> Mi = NeteaseSearch(key, i + 1, 100);
                            if (Mi != null)
                            {
                                searchItem.AddRange(Mi);
                            }
                        }
                    }
                    return searchItem;
                }
                catch { return null; }
            }
            if (api == 2)
            {
                try
                {
                    List<MusicInfo> searchItem = new List<MusicInfo>();
                    searchItem = QQSearch(Key);
                    return searchItem;
                }
                catch { return null; }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 网易云音乐带cookie访问
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string GetHTML(string url)
        {
            try
            {
                WebClientPro wc = new WebClientPro();
                wc.Headers.Add(HttpRequestHeader.Cookie, cookie);
                Stream s = wc.OpenRead(url);
                StreamReader sr = new StreamReader(s);
                return sr.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 网易云音乐搜索歌曲
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Page"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        List<MusicInfo> NeteaseSearch(string Key, int Page = 1, int limit = 100)
        {
            if (Key == null || Key == "")
            {
                return null;
            }
            WebClientPro wc = new WebClientPro();
            string offset = ((Page - 1) * 100).ToString();
            string url = NeteaseApiUrl + "search?keywords=" + Key + "&limit=" + limit.ToString() + "&offset=" + offset;
            string json = GetHTML(url);
            if (json == null || json == "")
            {
                return null;
            }
            Json.SearchResultJson.Root srj = JsonConvert.DeserializeObject<Json.SearchResultJson.Root>(json);
            List<Json.MusicInfo> ret = new List<Json.MusicInfo>();
            if (srj.result.songs == null)
            {
                return null;
            }
            string ids = "";
            for (int i = 0; i < srj.result.songs.Count; i++)
            {
                ids += srj.result.songs[i].id + ",";
            }
            string _u = NeteaseApiUrl + "song/detail?ids=" + ids.Substring(0, ids.Length - 1);
            string j = GetHTML(_u);
            Json.NeteaseMusicDetails.Root mdr = JsonConvert.DeserializeObject<Json.NeteaseMusicDetails.Root>(j);
            for (int i = 0; i < mdr.songs.Count; i++)
            {
                string singer = "";
                for (int x = 0; x < mdr.songs[i].ar.Count; x++)
                {
                    singer += mdr.songs[i].ar[x].name + "、";
                    //singerid.Add(mdr.songs[i].ar[x].id.ToString());
                }
                Json.MusicInfo mi = new Json.MusicInfo()
                {
                    Album = mdr.songs[i].al.name,
                    Id = mdr.songs[i].id.ToString(),
                    LrcUrl = NeteaseApiUrl + "lyric?id=" + mdr.songs[i].id.ToString(),
                    PicUrl = mdr.songs[i].al.picUrl + "?param=300y300",
                    Singer = singer.Substring(0, singer.Length - 1),
                    Title = mdr.songs[i].name,
                    Api = 1
                };
                ret.Add(mi);
            }
            return ret;
        }

        /// <summary>
        /// QQ音乐搜索歌曲
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        List<MusicInfo> QQSearch(string Key)
        {
            List<MusicInfo> res = new List<MusicInfo>();
            string url = QQApiUrl + "search?key=" + Key + "&pageSize=60";
            string resjson = "";
            using (WebClientPro wc = new WebClientPro())
            {
                StreamReader sr = new StreamReader(wc.OpenRead(url));
                resjson = sr.ReadToEnd();
            }
            QQMusicDetails.Root json = JsonConvert.DeserializeObject<QQMusicDetails.Root>(resjson);
            for (int i = 0; i < json.data.list.Count; i++)
            {
                string singers = "";
                foreach (QQMusicDetails.singer singer in json.data.list[i].singer)
                {
                    singers += singer.name + "、";
                }
                singers = singers.Substring(0, singers.Length - 1);
                res.Add(
                    new MusicInfo
                    {
                        Album = json.data.list[i].albumname,
                        Id = json.data.list[i].songmid,
                        Title = json.data.list[i].songname,
                        LrcUrl = QQApiUrl + "lyric?songmid=" + json.data.list[i].songmid,
                        PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.data.list[i].albummid + ".jpg",
                        Singer = singers,
                        Api = 2,
                        strMediaMid = json.data.list[i].strMediaMid
                    });
            }
            return res;
        }

        /// <summary>
        /// 下载方法
        /// </summary>
        /// <param name="dl"></param>
        public void Download(List<DownloadList> dl, int api)
        {
            string ids = "";

            if (api == 1)
            {
                int times = dl.Count / 500;
                int remainder = dl.Count % 500;
                if (remainder == 0)
                {
                    remainder = 500;
                }
                if (times == 0)
                {
                    times = 1;
                }
                for (int i = 0; i < times; i++)
                {
                    if (i == times - 1 && times >= 1)
                    {
                        ids = "";
                        for (int x = 0; x < remainder; x++)
                        {
                            ids += dl[i * 500 + x].Id + ",";
                        }
                        ids = ids.Substring(0, ids.Length - 1);
                        string u = NeteaseApiUrl + "song/url?id=" + ids + "&br=" + dl[0].Quality;
                        Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
                        for (int x = 0; x < remainder; x++)
                        {
                            for (int y = 0; y < dl.Count; y++)
                            {
                                if (urls.data[x].id.ToString() == dl[y].Id)
                                {
                                    dl[y].Url = urls.data[x].url;
                                    dl[y].State = "准备下载";
                                }
                            }
                        }
                    }
                    else
                    {
                        ids = "";
                        for (int x = 0; x < 500; x++)
                        {
                            ids += dl[i * 500 + x].Id + ",";
                        }
                        ids = ids.Substring(0, ids.Length - 1);
                        string u = NeteaseApiUrl + "song/url?id=" + ids + "&br=" + dl[0].Quality;
                        Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
                        for (int x = 0; x < 100; x++)
                        {
                            for (int y = 0; y < dl.Count; y++)
                            {
                                if (urls.data[x].id.ToString() == dl[y].Id)
                                {
                                    dl[y].Url = urls.data[x].url;
                                    dl[y].State = "准备下载";
                                }
                            }
                        }
                    }
                }
            }
            else if (api == 2)
            {
                for (int i = 0; i < dl.Count; i++)
                {
                    string url = QQApiUrl + "song/url?id=" + dl[i].Id + "&type=" + dl[i].Quality.Replace("128000", "128").Replace("320000", "320").Replace("999000", "flac") + "&mediaId=" + dl[i].strMediaMid;
                    using (WebClientPro wc = new WebClientPro())
                    {
                        StreamReader sr = new StreamReader(wc.OpenRead(url));
                        string httpjson = sr.ReadToEnd();
                        QQmusicdetails json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
                        if (json.result != 100)
                        {
                            url = url.Replace("flac", "320").Replace("128", "320");
                            sr = new StreamReader(wc.OpenRead(url));
                            httpjson = sr.ReadToEnd();
                            json = JsonConvert.DeserializeObject<QQmusicdetails>(httpjson);
                            if (url.IndexOf("flac") != -1 && json.result == 100)
                            {
                                dl[i].Quality = "320000";
                            }
                        }
                        dl[i].Url = json.data;
                        dl[i].State = "准备下载";
                    }
                }
            }
            downloadlist.AddRange(dl);
            UpdateDownloadPage();
            if (th_Download == null || th_Download?.ThreadState == System.Threading.ThreadState.Stopped)
            {
                th_Download = new Thread(_Download);
                th_Download.Start();
            }
        }

        /// <summary>
        /// 文件名检查
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string NameCheck(string name)
        {
            string re = name.Replace("*", " ");
            re = re.Replace("\\", " ");
            re = re.Replace("\"", " ");
            re = re.Replace("<", " ");
            re = re.Replace(">", " ");
            re = re.Replace("|", " ");
            re = re.Replace("?", " ");
            re = re.Replace("/", ",");
            re = re.Replace(":", "：");
            //re = re.Replace("-", "_");
            return re;
        }

        private void DownloadProgressUpdate(object sender, DownloadProgressChangedEventArgs e)
        {
            downloadlist[0].State = e.ProgressPercentage.ToString() + "%";
            UpdateDownloadPage();
        }

        /// <summary>
        /// 下载线程
        /// </summary>
        private void _Download()
        {
            while (downloadlist.Count != 0)
            {
                if (wait)
                {
                    continue;
                }
                downloadlist[0].State = "正在下载音乐";
                if (downloadlist[0].Url == null)
                {
                    downloadlist[0].State = "无版权";
                    UpdateDownloadPage();
                    downloadlist.RemoveAt(0);
                    wait = false;
                    continue;
                }
                UpdateDownloadPage();
                string savepath = "";
                string filename = ""; ;
                switch (setting.SaveNameStyle)
                {
                    case 0:
                        if (downloadlist[0].Url.IndexOf("flac") != -1)
                            filename = NameCheck(downloadlist[0].Title) + " - " + NameCheck(downloadlist[0].Singer) + ".flac";
                        else
                            filename = NameCheck(downloadlist[0].Title) + " - " + NameCheck(downloadlist[0].Singer) + ".mp3";
                        break;
                    case 1:
                        if (downloadlist[0].Url.IndexOf("flac") != -1)
                            filename = NameCheck(downloadlist[0].Singer) + " - " + NameCheck(downloadlist[0].Title) + ".flac";
                        else
                            filename = NameCheck(downloadlist[0].Singer) + " - " + NameCheck(downloadlist[0].Title) + ".mp3";
                        break;
                }
                switch (setting.SavePathStyle)
                {
                    case 0:
                        savepath = setting.SavePath;
                        break;
                    case 1:
                        savepath = setting.SavePath + "\\" + NameCheck(downloadlist[0].Singer);
                        break;
                    case 2:
                        savepath = setting.SavePath + "\\" + NameCheck(downloadlist[0].Singer) + "\\" + NameCheck(downloadlist[0].Album);
                        break;
                }
                if (!Directory.Exists(savepath))
                    Directory.CreateDirectory(savepath);

                if (downloadlist[0].IfDownloadMusic)
                {
                    if (System.IO.File.Exists(savepath + "\\" + filename))
                    {
                        downloadlist[0].State = "音乐已存在";
                        UpdateDownloadPage();
                        downloadlist.RemoveAt(0);
                        wait = false;
                        continue;
                    }
                    else
                    {
                        using (WebClientPro wc = new WebClientPro())
                        {
                            try
                            {
                                wc.DownloadProgressChanged += DownloadProgressUpdate;
                                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                                wc.DownloadFileAsync(new Uri(downloadlist[0].Url), savepath + "\\" + filename);
                                downloadlist[0].IsDownloading = true;
                                wait = true;
                            }
                            catch
                            {
                                downloadlist[0].State = "音乐下载错误";
                                downloadlist.RemoveAt(0);
                                wait = false;
                                UpdateDownloadPage();
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    string Lrc = "";
                    if (downloadlist[0].IfDownloadLrc)
                    {
                        downloadlist[0].State = "正在下载歌词";
                        UpdateDownloadPage();
                        using (WebClientPro wc = new WebClientPro())
                        {
                            try
                            {
                                if (downloadlist[0].Api == 1)
                                {
                                    string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                                    StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                                    string json = sr.ReadToEnd();
                                    NeteaseLrc.Root lrc = JsonConvert.DeserializeObject<NeteaseLrc.Root>(json);
                                    Lrc = lrc.lrc.lyric ?? "";
                                    if (Lrc != "")
                                    {
                                        StreamWriter sw = new StreamWriter(savename);
                                        sw.Write(Lrc);
                                        sw.Flush();
                                        sw.Close();
                                    }
                                    else
                                    {
                                        downloadlist[0].State = "歌词下载错误";
                                        UpdateDownloadPage();
                                    }
                                }
                                else if (downloadlist[0].Api == 2)
                                {
                                    string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                                    StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                                    string json = sr.ReadToEnd();
                                    QQLrc.Root lrc = JsonConvert.DeserializeObject<QQLrc.Root>(json);
                                    Lrc = lrc.data.lyric ?? "";
                                    if (Lrc != "")
                                    {
                                        StreamWriter sw = new StreamWriter(savename);
                                        sw.Write(Lrc);
                                        sw.Flush();
                                        sw.Close();
                                    }
                                    else
                                    {
                                        downloadlist[0].State = "歌词下载错误";
                                        UpdateDownloadPage();
                                    }
                                }
                            }
                            catch
                            {
                                downloadlist[0].State = "歌词下载错误";
                                UpdateDownloadPage();
                            }
                        }
                    }
                    if (downloadlist[0].IfDownloadPic)
                    {
                        downloadlist[0].State = "正在下载图片";
                        UpdateDownloadPage();
                        using (WebClientPro wc = new WebClientPro())
                        {
                            try
                            {
                                wc.DownloadFile(downloadlist[0].PicUrl, savepath + "\\" + filename.Replace(".flac", ".jpg").Replace(".mp3", ".jpg"));
                            }
                            catch
                            {
                                downloadlist[0].State = "图片下载错误";
                                UpdateDownloadPage();
                            }
                        }
                    }
                    if (File.Exists(savepath + "\\" + filename))
                    {
                        if (filename.IndexOf(".mp3") != -1)
                        {
                            using (var tfile = TagLib.File.Create(savepath + "\\" + filename))
                            {
                                //tfile.Tag.Title = downloadlist[0].Title;
                                //tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                                //tfile.Tag.Album = downloadlist[0].Album;
                                //if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                                //{
                                //    tfile.Tag.Lyrics = Lrc;
                                //}
                                if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                                {
                                    Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                                    TagLib.Picture pic = new TagLib.Picture();
                                    pic.Type = TagLib.PictureType.FrontCover;
                                    pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                                    pic.Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                                    tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                                }
                                tfile.Save();
                            }
                        }
                        else
                        {
                            using (var tfile = TagLib.Flac.File.Create(savepath + "\\" + filename))
                            {
                                tfile.Tag.Title = downloadlist[0].Title;
                                tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                                tfile.Tag.Album = downloadlist[0].Album;
                                if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                                {
                                    tfile.Tag.Lyrics = Lrc;
                                }
                                if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                                {
                                    Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                                    TagLib.Picture pic = new TagLib.Picture();
                                    pic.Type = TagLib.PictureType.FrontCover;
                                    pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                                    pic.Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                                    tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                                }
                                tfile.Save();
                            }
                        }
                    }
                    downloadlist[0].State = "下载完成";
                    UpdateDownloadPage();
                    downloadlist.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 下载完成后
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            string Lrc = "";
            string savepath = "";
            string filename = ""; ;
            switch (setting.SaveNameStyle)
            {
                case 0:
                    if (downloadlist[0].Url.IndexOf("flac") != -1)
                        filename = NameCheck(downloadlist[0].Title) + " - " + NameCheck(downloadlist[0].Singer) + ".flac";
                    else
                        filename = NameCheck(downloadlist[0].Title) + " - " + NameCheck(downloadlist[0].Singer) + ".mp3";
                    break;
                case 1:
                    if (downloadlist[0].Url.IndexOf("flac") != -1)
                        filename = NameCheck(downloadlist[0].Singer) + " - " + NameCheck(downloadlist[0].Title) + ".flac";
                    else
                        filename = NameCheck(downloadlist[0].Singer) + " - " + NameCheck(downloadlist[0].Title) + ".mp3";
                    break;
            }
            switch (setting.SavePathStyle)
            {
                case 0:
                    savepath = setting.SavePath;
                    break;
                case 1:
                    savepath = setting.SavePath + "\\" + NameCheck(downloadlist[0].Singer);
                    break;
                case 2:
                    savepath = setting.SavePath + "\\" + NameCheck(downloadlist[0].Singer) + "\\" + NameCheck(downloadlist[0].Album);
                    break;
            }
            if (!Directory.Exists(savepath))
                Directory.CreateDirectory(savepath);
            if (downloadlist[0].IfDownloadLrc)
            {
                downloadlist[0].State = "正在下载歌词";
                UpdateDownloadPage();
                using (WebClientPro wc = new WebClientPro())
                {
                    try
                    {
                        if (downloadlist[0].Api == 1)
                        {
                            string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                            StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                            string json = sr.ReadToEnd();
                            NeteaseLrc.Root lrc = JsonConvert.DeserializeObject<NeteaseLrc.Root>(json);
                            Lrc = lrc.lrc.lyric ?? "";
                            if (Lrc != "")
                            {
                                StreamWriter sw = new StreamWriter(savename);
                                sw.Write(Lrc);
                                sw.Flush();
                                sw.Close();
                            }
                            else
                            {
                                downloadlist[0].State = "歌词下载错误";
                                UpdateDownloadPage();
                            }
                        }
                        else if (downloadlist[0].Api == 2)
                        {
                            string savename = savepath + "\\" + filename.Replace(".flac", ".lrc").Replace(".mp3", ".lrc");
                            StreamReader sr = new StreamReader(wc.OpenRead(downloadlist[0].LrcUrl));
                            string json = sr.ReadToEnd();
                            QQLrc.Root lrc = JsonConvert.DeserializeObject<QQLrc.Root>(json);
                            Lrc = lrc.data.lyric ?? "";
                            if (Lrc != "")
                            {
                                StreamWriter sw = new StreamWriter(savename);
                                sw.Write(Lrc);
                                sw.Flush();
                                sw.Close();
                            }
                            else
                            {
                                downloadlist[0].State = "歌词下载错误";
                                UpdateDownloadPage();
                            }
                        }
                    }
                    catch
                    {
                        downloadlist[0].State = "歌词下载错误";
                        UpdateDownloadPage();
                    }
                }
            }
            if (downloadlist[0].IfDownloadPic)
            {
                downloadlist[0].State = "正在下载图片";
                UpdateDownloadPage();
                using (WebClientPro wc = new WebClientPro())
                {
                    try
                    {
                        wc.DownloadFile(downloadlist[0].PicUrl, savepath + "\\" + filename.Replace(".flac", ".jpg").Replace(".mp3", ".jpg"));
                    }
                    catch
                    {
                        downloadlist[0].State = "图片下载错误";
                        UpdateDownloadPage();
                    }
                }
            }
            if (filename.IndexOf(".mp3") != -1)
            {
                using (var tfile = TagLib.File.Create(savepath + "\\" + filename))
                {
                    //tfile.Tag.Title = downloadlist[0].Title;
                    //tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                    //tfile.Tag.Album = downloadlist[0].Album;
                    //if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                    //{
                    //    tfile.Tag.Lyrics = Lrc;
                    //}
                    if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                    {
                        Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                        TagLib.Picture pic = new TagLib.Picture();
                        pic.Type = TagLib.PictureType.FrontCover;
                        pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                        pic.Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                        tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                    }
                    tfile.Save();
                }
            }
            else
            {
                using (var tfile = TagLib.Flac.File.Create(savepath + "\\" + filename))
                {
                    tfile.Tag.Title = downloadlist[0].Title;
                    tfile.Tag.Performers = new string[] { downloadlist[0].Singer };
                    tfile.Tag.Album = downloadlist[0].Album;
                    if (downloadlist[0].IfDownloadLrc && Lrc != "" && Lrc != null)
                    {
                        tfile.Tag.Lyrics = Lrc;
                    }
                    if (downloadlist[0].IfDownloadPic && System.IO.File.Exists(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg"))
                    {
                        Tool.PngToJpg(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                        TagLib.Picture pic = new TagLib.Picture();
                        pic.Type = TagLib.PictureType.FrontCover;
                        pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                        pic.Data = TagLib.ByteVector.FromPath(savepath + "\\" + filename.Replace(".flac", "").Replace(".mp3", "") + ".jpg");
                        tfile.Tag.Pictures = new TagLib.IPicture[] { pic };
                    }
                    tfile.Save();
                }
            }
            downloadlist[0].State = "下载完成";
            UpdateDownloadPage();
            downloadlist.RemoveAt(0);
            wait = false;
        }

        /// <summary>
        ///解析歌单，为了稳定每次请求100歌曲信息，所以解析歌单的方法分为两部分，这个方法根据歌曲数量分解请求
        /// </summary>
        public List<MusicInfo> GetMusicList(string Id, int api)
        {
            if (api == 1)
            {
                Musiclist.Root musiclistjson = new Musiclist.Root();
                try
                {
                    musiclistjson = JsonConvert.DeserializeObject<Musiclist.Root>(GetHTML(NeteaseApiUrl + "playlist/detail?id=" + Id));
                }
                catch
                {
                    return null;
                }
                string ids = "";
                for (int i = 0; i < musiclistjson.playlist.trackIds.Count; i++)
                {
                    ids += musiclistjson.playlist.trackIds[i].id.ToString() + ",";
                }
                ids = ids.Substring(0, ids.Length - 1);

                if (musiclistjson.playlist.trackIds.Count > 100)
                {
                    string[] _id = ids.Split(',');

                    int times = musiclistjson.playlist.trackIds.Count / 100;
                    int remainder = musiclistjson.playlist.trackIds.Count % 100;
                    if (remainder == 0)
                    {
                        times--;
                        remainder = 100;
                    }
                    List<MusicInfo> re = new List<MusicInfo>();
                    for (int i = 0; i < times; i++)
                    {
                        string _ids = "";
                        if (i != times - 1)
                        {
                            for (int x = 0; x < 100; x++)
                            {
                                _ids += _id[i * 100 + x] + ",";
                            }
                        }
                        else
                        {
                            for (int x = 0; x < remainder; x++)
                            {
                                _ids += _id[i * 100 + x] + ",";
                            }
                        }
                        re.AddRange(_GetNeteaseMusicList(_ids.Substring(0, _ids.Length - 1)));
                    }
                    return re;
                }
                else
                {
                    return _GetNeteaseMusicList(ids);
                }
            }
            else if (api == 2)
            {
                string url = QQApiUrl + "songlist?id=" + Id;
                using (WebClientPro wc = new WebClientPro())
                {
                    StreamReader sr = new StreamReader(wc.OpenRead(url));
                    string httpres = sr.ReadToEnd();
                    if (httpres == null)
                    {
                        return null;
                    }
                    QQmusiclist.Root json = JsonConvert.DeserializeObject<QQmusiclist.Root>(httpres);
                    List<MusicInfo> re = new List<MusicInfo>();
                    if (json.data.songlist == null)
                    {
                        return null;
                    }
                    for (int i = 0; i < json.data.songlist.Count; i++)
                    {
                        string singers = "";
                        foreach (QQmusiclist.singer singer in json.data.songlist[i].singer)
                        {
                            singers += singer.name + "、";
                        }
                        singers = singers.Substring(0, singers.Length - 1);
                        re.Add(new MusicInfo()
                        {
                            Album = json.data.songlist[i].albumname,
                            Api = 2,
                            Id = json.data.songlist[i].songmid,
                            LrcUrl = QQApiUrl + "lyric?songmid=" + json.data.songlist[i].songmid,
                            PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.data.songlist[i].albummid + ".jpg",
                            Singer = singers,
                            strMediaMid = json.data.songlist[i].strMediaMid,
                            Title = json.data.songlist[i].songname
                        }
                            );
                    }
                    return re;
                }
            }
            return null;
        }

        /// <summary>
        /// 解析网易云音乐歌单的内部方法
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        private List<MusicInfo> _GetNeteaseMusicList(string ids)
        {
            List<Json.MusicInfo> ret = new List<Json.MusicInfo>();
            string _u = NeteaseApiUrl + "song/detail?ids=" + ids;
            string j = GetHTML(_u);
            Json.NeteaseMusicDetails.Root mdr = JsonConvert.DeserializeObject<Json.NeteaseMusicDetails.Root>(j);
            string u = NeteaseApiUrl + "song/url?id=" + ids + "&br=" + setting.DownloadQuality;
            Json.GetUrl.Root urls = JsonConvert.DeserializeObject<Json.GetUrl.Root>(GetHTML(u));
            for (int i = 0; i < mdr.songs.Count; i++)
            {
                string singer = "";
                List<string> singerid = new List<string>();
                string _url = "";

                for (int x = 0; x < mdr.songs[i].ar.Count; x++)
                {
                    singer += mdr.songs[i].ar[x].name + "、";
                    singerid.Add(mdr.songs[i].ar[x].id.ToString());
                }

                for (int x = 0; x < urls.data.Count; x++)
                {
                    if (urls.data[x].id == mdr.songs[i].id)
                    {
                        _url = urls.data[x].url;
                    }
                }

                MusicInfo mi = new MusicInfo()
                {
                    Album = mdr.songs[i].al.name,
                    Id = mdr.songs[i].id.ToString(),
                    LrcUrl = NeteaseApiUrl + "lyric?id=" + mdr.songs[i].id.ToString(),
                    PicUrl = mdr.songs[i].al.picUrl + "?param=300y300",
                    Singer = singer.Substring(0, singer.Length - 1),
                    Title = mdr.songs[i].name,
                    Api = 1
                };
                ret.Add(mi);
            }
            return ret;
        }

        /// <summary>
        /// 解析专辑
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<MusicInfo> GetAlbum(string id, int api)
        {
            if (api == 1)
            {
                List<MusicInfo> res = new List<MusicInfo>();
                string url = NeteaseApiUrl + "album?id=" + id;
                NeteaseAlbum.Root json;
                try
                {
                    json = JsonConvert.DeserializeObject<NeteaseAlbum.Root>(GetHTML(url));
                }
                catch
                {
                    return null;
                }
                for (int i = 0; i < json.songs.Count; i++)
                {
                    string singer = "";
                    for (int x = 0; x < json.songs[i].ar.Count; x++)
                    {
                        singer += json.songs[i].ar[x].name + "、";
                    }

                    MusicInfo mi = new MusicInfo()
                    {
                        Title = json.songs[i].name,
                        Album = json.album.name,
                        Id = json.songs[i].id.ToString(),
                        LrcUrl = NeteaseApiUrl + "lyric?id=" + json.songs[i].id.ToString(),
                        PicUrl = json.songs[i].al.picUrl + "?param=300y300",
                        Singer = singer.Substring(0, singer.Length - 1),
                        Api = 1
                    };

                    res.Add(mi);
                }
                return res;
            }
            if (api == 2)
            {
                string url = QQApiUrl + "album/songs?albummid=" + id;
                using (WebClientPro wc = new WebClientPro())
                {
                    StreamReader sr = new StreamReader(wc.OpenRead(url));
                    string httpres = sr.ReadToEnd();
                    QQAlbum.Root json = null;
                    try
                    {
                        json = JsonConvert.DeserializeObject<QQAlbum.Root>(httpres);
                    }
                    catch
                    {
                        return null;
                    }
                    List<MusicInfo> res = new List<MusicInfo>();
                    if (json.data.list == null || json.data.list.Count == 0)
                    {
                        return null;
                    }
                    for (int i = 0; i < json.data.list.Count; i++)
                    {
                        string singers = "";
                        foreach (QQAlbum.singer singer in json.data.list[i].singer)
                        {
                            singers += singer.title + "、";
                        }
                        singers = singers.Substring(0, singers.Length - 1);
                        MusicInfo mi = new MusicInfo()
                        {
                            Title = json.data.list[i].title,
                            Album = json.data.list[i].album.title,
                            Id = json.data.list[i].mid,
                            LrcUrl = QQApiUrl + "lyric?songmid=" + json.data.list[i].mid,
                            PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.data.list[i].album.mid + ".jpg",
                            Singer = singers,
                            Api = 2,
                            strMediaMid = json.data.list[i].ksong.mid
                        };
                        res.Add(mi);
                    }
                    return res;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取qq音乐榜单
        /// </summary>
        /// <param name="id">参考接口 /top/category </param>
        /// <returns></returns>
        public List<MusicInfo> GetQQTopList(string id)
        {
            string url = QQApiUrl + "top?id=" + id;
            using (WebClientPro wc = new WebClientPro())
            {
                StreamReader sr = new StreamReader(wc.OpenRead(url));
                QQTopList.Root json = JsonConvert.DeserializeObject<QQTopList.Root>(sr.ReadToEnd());
                List<MusicInfo> re = new List<MusicInfo>();
                for (int i = 0; i < json.data.list.Count; i++)
                {
                    re.Add(new MusicInfo
                    {
                        Album = json.data.list[i].album.title,
                        Api = 2,
                        Id = json.data.list[i].mid,
                        Singer = json.data.list[i].singerName,
                        strMediaMid = json.data.list[i].file.media_mid,
                        Title = json.data.list[i].title,
                        LrcUrl = QQApiUrl + "lyric?songmid=" + json.data.list[i].mid,
                        PicUrl = "https://y.gtimg.cn/music/photo_new/T002R500x500M000" + json.data.list[i].album.mid + ".jpg",
                    });
                }
                return re;
            }
        }
    }
}
