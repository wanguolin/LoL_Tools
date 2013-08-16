using System;
using System.Collections.Generic;
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
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Collections;

namespace DBMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string _path_fix_compressed_ready = "已压缩";
        const string _path_fix_name_upload      = "待上传";
        static string _str_compressed_dir          = System.Windows.Forms.Application.StartupPath + "\\" + _path_fix_compressed_ready;
        static string _str_output_dir              = System.Windows.Forms.Application.StartupPath + "\\" + _path_fix_name_upload;
        static string _json_path_upload = _str_output_dir + "\\json_upload";
        static string _pic_upload_path = _str_output_dir + "\\pic_upload";

        public MainWindow()
        {
            InitializeComponent();
        }

        //generate json for server
        class _FolderJSON
        {
            public string Name;
            public string Home;
            public int Count;
        }



        public class _Copy_From_To
        {
            public _Copy_From_To(string strFrom, string strTo) { From = strFrom; To = strTo; }
            public string From;
            public string To;
        }

        public class _Pic
        {
            public _Pic(string strPicName, string strMD5) { picName = strPicName; picMD5 = strMD5; }
            public string picName;
            public string picMD5;
        }

        class _Series
        {
            public _Series(string strSeries) { seriesName = strSeries; }
            public string seriesName;
            public List<_Pic> picInSeries = new List<_Pic>();
            public _Pic mainPage;
        }

        class _Site
        {
            public _Site(string strSite) { siteName = strSite; }
            public string siteName;
            public List<_Series> seriesInSite = new List<_Series>();
        }

        class DB_JSON{
            public List<_Site> siteAll = new List<_Site>();
        }

        DB_JSON _compressed_dir_info = new DB_JSON();
        int _total_compressed_file_count = 0;
        DB_JSON _current_json_file = new DB_JSON();
        List<_Copy_From_To> _copy_from_to = new List<_Copy_From_To>();
        HashSet<string> _existing_output_md5_files = new HashSet<string>();
        Dictionary<string,string> _existing_output_md5_from_db = new Dictionary<string,string>();
        Dictionary<string, string> _file_need_to_copy = new Dictionary<string, string>();
        Dictionary<string,string> _all_md5_rst_checking_duplicated = new Dictionary<string,string>();
    
        private void CrossChecking()
        {
            LogToShowInfoToLabelInThread("正在扫描现有的文件。");
            ScanExsitingOutputFile();
            LoadCurrentJSON();
            LogToShowInfoToLabelInThread("正在扫描已经压缩好待处理的文件。");
            ScanCompressedFolder();
            LogToShowInfoToLabelInThread("正在进行交叉比对。");
            SetProgressMaxInThread(_total_compressed_file_count);
            int nCurrentProgress = 0;
            foreach (_Site forSite in _compressed_dir_info.siteAll)
                foreach (_Series forSeries in forSite.seriesInSite)
                    foreach (_Pic forPic in forSeries.picInSeries)
                        forPic.picMD5 = GetMD5( forSite.siteName, forSeries.seriesName, forPic.picName, ++nCurrentProgress);
         
            SaveJSON();

            this.Dispatcher.Invoke(new Action(() => { btnIns.IsEnabled = true; }));
        }

        private void SaveJSON()
        {
            if (!Directory.Exists(_json_path_upload))
                Directory.CreateDirectory(_json_path_upload);

            List<_FolderJSON> lstAllSiteJSON = new List<_FolderJSON>();
            LogToShowInfoToLabelInThread("正在存储JSON");
            foreach (_Site siteInfo in _compressed_dir_info.siteAll)
            {
                LogToShowInfoToLabelInThread("正在处理：" + siteInfo.siteName);
                _FolderJSON folderSiteJSON = new _FolderJSON();
                folderSiteJSON.Name = siteInfo.siteName;
                folderSiteJSON.Count = siteInfo.seriesInSite.Count;
                folderSiteJSON.Home = siteInfo.seriesInSite[0].picInSeries[0].picMD5;
                lstAllSiteJSON.Add(folderSiteJSON);

                List<_FolderJSON> lstSeriesJSON = new List<_FolderJSON>();
                
                int nCount = 1;
                foreach ( _Series seriesInfo in siteInfo.seriesInSite)
                {
                    _FolderJSON seriesJSON = new _FolderJSON();
                    seriesJSON.Name = seriesInfo.seriesName;
                    seriesJSON.Home = seriesInfo.mainPage.picMD5;
                    seriesJSON.Count = seriesInfo.picInSeries.Count;
                    lstSeriesJSON.Add(seriesJSON);
                    if ( nCount%18 == 0 || nCount == (siteInfo.seriesInSite.Count)) // nGroup - (nGroup%18)
                    {
                        string json_series_in_server = JsonConvert.SerializeObject(lstSeriesJSON);
                        string saveSeriesToJSONFileInServer = _json_path_upload + "\\" + siteInfo.siteName + "_" + nCount.ToString() + ".json";
                        File.WriteAllText(saveSeriesToJSONFileInServer, json_series_in_server);
                        lstSeriesJSON.Clear();
                    }
                  
                    string json_pics_in_server = JsonConvert.SerializeObject(seriesInfo.picInSeries);
                    string savePicsToJSONFileInServer = _json_path_upload + "\\" + siteInfo.siteName + "_" + nCount.ToString() + "_pic.json";
                    File.WriteAllText(savePicsToJSONFileInServer, json_pics_in_server);
                    ++nCount;
                }
           }
            string json_site_all = JsonConvert.SerializeObject(_compressed_dir_info);
            File.WriteAllText(_json_path_upload + "\\" + "ori_json_data.json", json_site_all);

            string json_site_in_server = JsonConvert.SerializeObject(lstAllSiteJSON);
            File.WriteAllText(_json_path_upload + "\\" + "all_site.json", json_site_in_server);

            LogToShowInfoToLabelInThread("json已经生成。");
        }

        private string GetMD5( string strSiteName, string strSeriesName, string strPicName, int nProgreess)
        {
            string picFileFullPath = _str_compressed_dir + "\\" + strSiteName + "\\" + strSeriesName + "\\" + strPicName;
            string strMD5 = GetMD5FromCurrentJSON(strSiteName, strSeriesName, strPicName);
            if (strMD5 == "")
                strMD5 = GetFileMD5(picFileFullPath);
            SetProgressCurrentInThread(nProgreess);

            if ( !_all_md5_rst_checking_duplicated.ContainsKey(strMD5))
                _all_md5_rst_checking_duplicated.Add(strMD5,picFileFullPath);
            else
                LogToInsTextBoxInThread(picFileFullPath + "和" + _all_md5_rst_checking_duplicated[strMD5] + " 是重复的，请确认是一下原始图片。");
          
            return strMD5.ToLower();
        }

        private void ScanExsitingOutputFile()
        {
            foreach (string strFileName in Directory.GetFiles(_pic_upload_path))
            {
                FileInfo fi = new FileInfo(strFileName);
                _existing_output_md5_files.Add(fi.Name);
            }
        }

        private void LoadCurrentJSON()
        {
            try
            {
                string strJSON = File.ReadAllText(_json_path_upload + "\\ori_json_data.json");
                _current_json_file = JsonConvert.DeserializeObject<DB_JSON>(strJSON);
            }
            catch (System.Exception ex)
            {
                LogToDelTextBoxInThread("加载:Current.JSON出错，请检查" + _str_output_dir + ex.ToString());
            }
        }


        private void SetProgressMaxInThread(int nMax)
        {
            this.Dispatcher.Invoke(new Action(() => { progressBar.Maximum = nMax; }));
        }

        private void SetProgressCurrentInThread(int nCurrent)
        {
            this.Dispatcher.Invoke(new Action(() => { progressBar.Value = nCurrent; }));
        }

        private void LogToShowInfoToLabelInThread(string strLog)
        {
            this.Dispatcher.Invoke(new Action(() => { labShowInfo.Content = strLog; }));
        }

        private void LogToInsTextBoxInThread(string strLog)
        {
            this.Dispatcher.Invoke(new Action(() => { txtBoxIns.AppendText(strLog + "\n"); }));
        }

        private void LogToDelTextBoxInThread(string strLog)
        {
            this.Dispatcher.Invoke(new Action(() => { txtBoxDel.AppendText(strLog + "\n"); }));
        }

        private void ScanCompressedFolder()
        {
            LogToShowInfoToLabelInThread("正在扫描已压缩好的路径,生成数据结构。");

            foreach (string SiteFullPath in Directory.GetDirectories(_str_compressed_dir))
            {
                DirectoryInfo siteDirInfo = new DirectoryInfo(SiteFullPath);
                _Site siteInfo = new _Site(siteDirInfo.Name);
                foreach (string SeriesFullPath in Directory.GetDirectories(SiteFullPath))
                {
                    DirectoryInfo seriesDirInfo = new DirectoryInfo(SeriesFullPath);
                    _Series seriesInfo = new _Series(seriesDirInfo.Name);
                    foreach (string PicFullPath in Directory.GetFiles(SeriesFullPath))
                    {
                        DirectoryInfo picFileInfo = new DirectoryInfo(PicFullPath);
                        if (picFileInfo.Extension.ToLower() == ".webp")
                        {
                            seriesInfo.picInSeries.Add(new _Pic(picFileInfo.Name, ""));
                            _total_compressed_file_count++;
                        }
                    }
                    seriesInfo.mainPage = seriesInfo.picInSeries[0];
                    siteInfo.seriesInSite.Add(seriesInfo);
                }
                _compressed_dir_info.siteAll.Add(siteInfo);
            }

        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(CrossChecking));
            thread.IsBackground = true;
            thread.Start();
        }

        private void ScanOutputExistingFile()
        {
            LogToShowInfoToLabelInThread("正在扫描输出路径中已经存在的文件.");
            
        }

   

        private string GetMD5FromCurrentJSON(string strSite, string strSeries, string strPicName)
        {
            foreach (_Site forSite in _current_json_file.siteAll)
                if (forSite.siteName == strSite)
                    foreach (_Series forSeries in forSite.seriesInSite)
                        if (forSeries.seriesName == strSeries)
                            foreach (_Pic forPic in forSeries.picInSeries)
                                if (forPic.picName == strPicName)
                                    return forPic.picMD5;
            return "";
        }

        private string GetFileMD5(string strFullPath)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(File.OpenRead(strFullPath));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            string strMD5 = sBuilder.ToString() + (new DirectoryInfo(strFullPath)).Extension;
            LogToShowInfoToLabelInThread("MD5计算中：" + strFullPath + "=>" + strMD5);
            return strMD5;
       }

     

        private void btnIns_Click(object sender, RoutedEventArgs e)
        {
         
            Thread thread = new Thread(new ThreadStart(DoCopy));
            thread.IsBackground = true;
            thread.Start();
        }

        private void DoCopy()
        {
            this.Dispatcher.Invoke(new Action(() => { btnIns.IsEnabled = false; }));
            this.Dispatcher.Invoke(new Action(() => { _all_md5_rst_checking_duplicated.Count(); }));
            int nCurProgress = 0;
            SetProgressMaxInThread(_all_md5_rst_checking_duplicated.Count());
            IDictionaryEnumerator itAllCopy= _all_md5_rst_checking_duplicated.GetEnumerator();

            if (!Directory.Exists(_pic_upload_path))
                Directory.CreateDirectory(_pic_upload_path);

            while(itAllCopy.MoveNext()){
                if (_existing_output_md5_files.Contains(itAllCopy.Key.ToString()))
                    LogToInsTextBoxInThread(itAllCopy.Key.ToString() + "已存在，不拷贝。");
                else
                    File.Copy(itAllCopy.Value.ToString(), _pic_upload_path + "\\" + itAllCopy.Key.ToString());
                SetProgressCurrentInThread(++nCurProgress);
            }
            this.Dispatcher.Invoke(new Action(() => { btnIns.IsEnabled = true; }));
        }
    }
}
