using ImageMagick;
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
        //generate json for server

        class ALL_SITE
        {
            public List<_FolderJSON> siteAll = new List<_FolderJSON>();
        };

       

        class _FolderJSON
        {
            public string Name;
            public string Home;
            public int Count;
        };

       
        static List<_FolderJSON> _lst_site_json = null;//new List<_FolderJSON>();
        static List<_FolderJSON> _lst_series_json = new List<_FolderJSON>();
        static List<BitmapImage> _lst_bitmap = new List<BitmapImage>();

        public MainWindow()
        {
            InitializeComponent();
            txtLog.AppendText("正在获取站点信息...\n");
            GetSite();
            
            try
            {
                List<System.Windows.Controls.Image> imagelist = new List<System.Windows.Controls.Image>();
                for (int i = 0; i < 10; i++)
                {
                    System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                    image.Source = new BitmapImage(new Uri("http://picturelol.qiniudn.com/0000c4e85588f4ea6d2665ff3db7c602.jpg"));
                    imagelist.Add(image);
                }
                lvPictures.ItemsSource = imagelist;

            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
            }


            //lvPictures.ItemsSource = 
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

        private async void GetSeries(string strSeriesName)
        {
            string strSiteName = selectSite.SelectedItem.ToString();
            int nSiteCount = getSiteCount(strSiteName);
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
                string url = strSeriesName + "_" + nSiteCount.ToString() + ".json";
                string json = await hc.GetStringAsync(url);
                List<_FolderJSON> lstSeries = JsonConvert.DeserializeObject<List<_FolderJSON>>(json);
                _lst_series_json.AddRange(lstSeries);
            }

        }

        private void selectSeries_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            
        }

        private int getSiteCount(string siteName)
        {
            foreach (_FolderJSON n in _lst_site_json)
                if (n.Name == siteName)
                    return n.Count;
            return 0;
        }

        private void selectSite_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            GetSeries(selectSite.SelectedItem.ToString());
        }

    }
}
