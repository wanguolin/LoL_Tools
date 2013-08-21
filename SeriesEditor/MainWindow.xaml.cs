
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using System.Windows.Controls;
using Newtonsoft.Json;

namespace SeriesEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string _qiniu_url = "http://picturelol.qiniudn.com/";
        const string _site_json = "all_site.json";
        const string _qiniu_url_all_site = _qiniu_url + _site_json;
        const int _series__factor = 18;
        const string _qiniu_image_processing = "?imageView/2/w/200/quality/30";
        //generate json for server

        class ALL_SITE
        {
            public List<_FolderJSON> siteAll = new List<_FolderJSON>();
        };

        public class _Pic
        {
            public _Pic(string strPicName, string strMD5) { picName = strPicName; picMD5 = strMD5; }
            public string picName;
            public string picMD5;
        }

        class _FolderJSON
        {
            public string Name;
            public string Home;
            public int Count;
        };

       
        static List<_FolderJSON> _lst_site_json = null;//new List<_FolderJSON>();
        static List<_FolderJSON> _lst_series_json = new List<_FolderJSON>();
        static List<_Pic> _lst_pic_json = new List<_Pic>();
        static List<BitmapImage> _lst_bitmap = new List<BitmapImage>();
        

        public MainWindow()
        {
            InitializeComponent();
            txtLog.AppendText("正在获取站点信息...\n");
            GetSite();
             
           
        }

        private void AddLogInThread(string l)
        {
            this.Dispatcher.Invoke(new Action(() => { txtLog.AppendText(l + "\n"); }));
        }

        private async void GetSite()
        {
            HttpClient hc = new HttpClient();
            string jsonSite = await hc.GetStringAsync(_qiniu_url_all_site);
            _lst_site_json = JsonConvert.DeserializeObject<List<_FolderJSON>>(jsonSite);
            foreach(_FolderJSON f in _lst_site_json)
                selectSite.Items.Add(f.Name);
            AddLogInThread("获取成功");
            hc.Dispose();
        }

        private async void GetPic(string pic_json_file)
        {

            List<System.Windows.Controls.Image> imagelist = new List<System.Windows.Controls.Image>();
            HttpClient hc = new HttpClient();
            string json = await hc.GetStringAsync(pic_json_file);
            List<_Pic> lstPic = JsonConvert.DeserializeObject<List<_Pic>>(json);

            foreach ( _Pic p in lstPic)
            {
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Source = new BitmapImage(new Uri(_qiniu_url + p.picMD5 + _qiniu_image_processing));
                imagelist.Add(image);
            }
            lvPictures.ItemsSource = imagelist;
            lvPictures.Items.Refresh();

            hc.Dispose();
        }

        private async void GetSeries(string strSeriesName)
        {
            _lst_series_json.Clear();
            string strSiteName = selectSite.SelectedItem.ToString();
            int nSiteCount = GetCountFromList(strSiteName, _lst_site_json);
            HttpClient hc = new HttpClient();
            for (int i = 2; i * _series__factor < nSiteCount; ++i)
            {
                string url = _qiniu_url + strSeriesName + "_" + (i * _series__factor).ToString() + ".json";
                string json = await hc.GetStringAsync(url);
                List<_FolderJSON> lstSeries = JsonConvert.DeserializeObject<List<_FolderJSON>>(json);
                _lst_series_json.AddRange( lstSeries);//.Join<_FolderJSON>(lstSeries);
                AddLogInThread("正在添加 " + url);
            }
            if (nSiteCount % _series__factor != 0)
            {
                string url = _qiniu_url + strSeriesName + "_" + nSiteCount.ToString() + ".json";
                string json = await hc.GetStringAsync(url);
                List<_FolderJSON> lstSeries = JsonConvert.DeserializeObject<List<_FolderJSON>>(json);
                _lst_series_json.AddRange(lstSeries);
            }
            foreach (_FolderJSON f in _lst_series_json)
                selectSeries.Items.Add(f.Name);
        }

        private void selectSeries_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string url = selectSite.SelectedItem.ToString() + "_" + (selectSeries.SelectedIndex + 1).ToString() + "_pic.json";
            AddLogInThread(url);
            GetPic(_qiniu_url + url);

        }

        private int GetCountFromList(string siteName, List<_FolderJSON> lstSearchFrom)
        {
            foreach (_FolderJSON n in lstSearchFrom)
                if (n.Name == siteName)
                    return n.Count;
            return 0;
        }

        private void selectSite_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            GetSeries(selectSite.SelectedItem.ToString());
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

    }
}
