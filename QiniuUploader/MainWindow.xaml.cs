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

using QBox.Auth;
using QBox.RS;
using QBox.FileOp;
using QBox.RPC;
using QBox.Util;


namespace QiniuUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        const string _path_fix_upload = "待上传";
        const string _path_post_fix_for_logfile = "_log\\uploaded.log";

        const string _bkt_name_pic = "picturelol";
        const string _bkt_name_json = "jsonlol";

        static string _str_wait_upload_root_path = System.Windows.Forms.Application.StartupPath + "\\" + _path_fix_upload;

        static string _str_upload_pic_path = _str_wait_upload_root_path + "\\" + "pic_upload";
        static string _str_upload_json_path = _str_wait_upload_root_path + "\\" + "json_upload";

        //int _thread_num = 0;
        int _thread_count = 0;
        int _n_all_file_should_be_uploaded = 0;

        volatile HashSet<string> _set_uploaded_from_logfile = new HashSet<string>();
        volatile HashSet<string> _set_is_uploading_in_buffer = new HashSet<string>();
        volatile HashSet<string> _set_uploaded_in_buffer = new HashSet<string>();
        List<string> _lst_file_to_be_uploaded = new List<string>();

        private void selectThreadNum_Changed(object sender, SelectionChangedEventArgs e)
        {
            _thread_count = Convert.ToInt32(selectThreadNum.SelectedItem.ToString());
        }

        private void btnUploadPic_Click(object sender, RoutedEventArgs e)
        {
            Thread thdCheck = new Thread(() => ScanToBeUploadedFiles(_str_upload_pic_path));
            thdCheck.IsBackground = true;
            thdCheck.Start();

           
           
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            

            Config.ACCESS_KEY = "6TBboVqOisPW6vEuQOZPDqZVxzbDlsWeAk7JzYIe";
            Config.SECRET_KEY = "P877YbAMwccCGgosRjTvP-kUDEhN25B7LRsS7A6U";

           
        }

        private void btnDeletePic_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeleteDB_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnUploadDB_Click(object sender, RoutedEventArgs e)
        {

        }

        

        private void LogToTextBoxInThread(string strLog)
        {
            this.Dispatcher.Invoke(new Action(() => { txtLog.AppendText(strLog + "\n"); }));
        }


        private void ScanToBeUploadedFiles(string scanPath)
        {
           
            LogToTextBoxInThread("正在录入待扫描目录" + _str_upload_pic_path);
            string strLogPath = scanPath + _path_post_fix_for_logfile;
            if (!File.Exists(strLogPath))
                File.Create(strLogPath);
            else
            {
                try
                {
                    foreach (string strUploadedFileName in File.ReadAllLines(strLogPath))
                        _set_uploaded_from_logfile.Add(strUploadedFileName);
                }
                catch (System.Exception ex)
                {
                    LogToTextBoxInThread(strLogPath + " 读取日志文件异常");
                }
               
            }
            foreach (string picFileName in Directory.GetFiles(scanPath))
                _lst_file_to_be_uploaded.Add(picFileName);
            
            LogToTextBoxInThread("目录：" + scanPath + " 中有文件：" + _lst_file_to_be_uploaded.Count.ToString());
            LogToTextBoxInThread("其中已上传:" + _set_uploaded_from_logfile.Count.ToString());
            LogToTextBoxInThread("待上传：" + (_n_all_file_should_be_uploaded = _lst_file_to_be_uploaded.Count - _set_uploaded_from_logfile.Count).ToString());

            if (_thread_count == 0)
                LogToTextBoxInThread("请选择上传线程数目");

            for (int i = 0; i < _thread_count; ++i)
            {
                Thread thdUpload = new Thread(() => PutFileToCDN(_bkt_name_pic, i.ToString()));
                thdUpload.IsBackground = true;
                thdUpload.Start();
            }
            
        
        }
        
        private void PutFileToCDN(string bktName, string strIndex)
        {
            string strMyName = "线程" + strIndex;
            while (_n_all_file_should_be_uploaded > _set_uploaded_in_buffer.Count)
            {
                string strBktName = bktName.ToString();
                var authPolicy = new AuthPolicy(strBktName, 3600L);
                string upToken = authPolicy.MakeAuthTokenString();

                LogToTextBoxInThread("申请上传授权..." + upToken);

                foreach (string localFile in _lst_file_to_be_uploaded)
                {
                    if (_set_uploaded_from_logfile.Contains(localFile) || _set_is_uploading_in_buffer.Contains(localFile))
                        continue;
                    _set_is_uploading_in_buffer.Add(localFile);
                    string key = (new FileInfo(localFile)).Name;

                    PutFileRet putFileRet = RSClient.PutFileWithUpToken(upToken, strBktName, key, null, localFile, null, "key=<key>");

                    if (putFileRet.OK)
                    {
                        _set_is_uploading_in_buffer.Remove(localFile);
                        _set_uploaded_in_buffer.Add(localFile);
                        LogToTextBoxInThread(key + "上传成功！ ...by" + strMyName);
                        File.AppendAllText(_str_upload_pic_path + _path_post_fix_for_logfile,localFile + "\n");
                    }
                    else
                    {
                        LogToTextBoxInThread(key + "失败！ ...by" + strMyName);
                    }
                }

                LogToTextBoxInThread("正在复查是否还有漏网之鱼");
            }
    
        }

        private void selectThreadNum_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i <= 10; ++i)
                selectThreadNum.Items.Add(i.ToString());
        }
    }
}
