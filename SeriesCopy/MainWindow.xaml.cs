using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Noesis.Drawing.Imaging.WebP;

namespace SeriesCopy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string _type_jpeg = "JPEG";
        const string _type_webp = "web_p";
        const int _n_pic_in_path_no_less_than = 7;
        const string _str_logfile_in_dest_dir = "from.txt";
        static string _str_app_base_path = System.Windows.Forms.Application.StartupPath;
        const string _str_organized_name = "已压缩";
        string _str_path_from = "";
        string _str_path_to = "";//_str_app_base_path + "\\" + _str_organized_name;
        string _str_site_name = "";
        string _output_type = "";
        long _compress_qualiry = 0;

        HashSet<string> _set_log_in_dest_dir = new HashSet<string>();
        List<string> _lst_all_path_from = new List<string>();
        List<string> _lst_all_path_to = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void labFromDir_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FolderBrowserDialog diagFolder = new FolderBrowserDialog();
            if (diagFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                labFromDir.Content = _str_path_from = diagFolder.SelectedPath;
            
          
        }

        private void selectPath_changed(object sender, SelectionChangedEventArgs e)
        {
            _str_path_to = _str_app_base_path + "\\" + _str_organized_name + "\\" + selectPath.SelectedItem;
        }

        private void btnStartScan_Click(object sender, RoutedEventArgs e)
        {
            if (_str_path_to == "")
            {
                txtLog.AppendText("请选择要整理到的目录");
            }
            else
            {
                Thread thread = new Thread(new ThreadStart(() => StartScan(_str_path_from)));
                thread.IsBackground = true;
                thread.Start();
            }
            btnStartCopy.IsEnabled = true;
        }

        private void btnStartCopy_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(DoCopy));
            thread.IsBackground = true;
            thread.Start();
        }

        private void AppendLog(string l)
        {
            this.Dispatcher.Invoke(new Action(() => { txtLog.AppendText(l + "\n"); }));
        }

        private void StartScan(string strDirName)
        {
            AppendLog("开始扫描目录");
            ScanLogFileInDestDir();
            ScanDir(strDirName);
            AppendLog("扫描完毕");
            ConvertSrcDirToDest();
            int i = 0;
            foreach (string l in _lst_all_path_from)
                AppendLog(l + " => " +_lst_all_path_to[i++]);
            AppendLog("一共有:" + _lst_all_path_from.Count.ToString() + "个目录待整理");
        }

        private void ConvertSrcDirToDest()
        {
            foreach ( string from in _lst_all_path_from)
            {
                string to = _str_path_to + "\\" + from.Replace(_str_path_from + "\\", "").Replace("\\", "_");
                _lst_all_path_to.Add(to);
            }
        }

        private void CompressPicture()
        {

        }
       
        private void DoCopy()
        {
            if ( _compress_qualiry == 0 || _output_type == "")
            {
                AppendLog("请选择输出文件信息或是压缩质量");
                return;
            }

            AppendLog("正在创建目录");
            foreach (string strCreateDir in _lst_all_path_to)
                Directory.CreateDirectory(strCreateDir);
            AppendLog("创建完毕，开始拷贝");
            SetProgressMaxInThread(_lst_all_path_to.Count);

            for (int i = 0; i < _lst_all_path_from.Count; ++i )
            {
                int nPicCount = 0;
                long lPicTotalSize = 0;
                foreach ( string strFileName in Directory.GetFiles(_lst_all_path_from[i]))
                {
                    if (ChkIsPicVaild(strFileName))
                    {
                        try
                        {
                            FileInfo di = new FileInfo(strFileName);
                            if ( _output_type == _type_jpeg)
                            {
                                File.Copy(strFileName, _lst_all_path_to[i] + "\\" + di.Name);
                            }
                            else {
                                using (System.Drawing.Image image = System.Drawing.Image.FromFile(strFileName))
                                {
                                    Bitmap bitmap = new Bitmap(image);
                                    WebPFormat.SaveToFile(_lst_all_path_to[i] + "\\" + di.Name.Replace(di.Extension, ".webp"), bitmap);
                                    bitmap.Dispose();
                                    
                                }
                            }
                           
                            nPicCount++;
                            lPicTotalSize += di.Length;
                        }
                        catch (System.Exception ex)
                        {
                        	AppendLog("拷贝文件" + strFileName + "异常：" + ex.Message);
                        }
                       
                    }
                    else
                    {
                        AppendLog(strFileName + " 不予拷贝");
                    }
                }
                if (lPicTotalSize == 0)
                {
                    AppendLog("拷贝数目为0,删除.." + _lst_all_path_to[i]);
                    Directory.Delete(_lst_all_path_to[i]);
                    continue;
                }
                string[] split = _lst_all_path_to[i].Split('\\');
                string strLastPathName = split[split.Length - 1];
                string strRenameTo = _lst_all_path_to[i].Replace(strLastPathName, (_set_log_in_dest_dir.Count + i).ToString() + "_PIC[" + nPicCount.ToString() + "]_" + (lPicTotalSize / 1024 / 1024).ToString() + "M");
                try
                {
                    Directory.Move(_lst_all_path_to[i], strRenameTo);
                    AppendLog("重命名：" + _lst_all_path_to[i] + " => " + strRenameTo);
                }
                catch (System.Exception ex)
                {
                    AppendLog("移动目录失败." + _lst_all_path_to[i] + " " + ex.Message);
                }
                File.WriteAllText(strRenameTo + "\\" + _str_logfile_in_dest_dir, _lst_all_path_from[i].Replace(_str_path_from, "") + "\n" + _compress_qualiry.ToString() + "%");
                SetProgressCurrentInThread(i);
            }

        }

        private bool ChkIsPicVaild(string strFileName)
        {
             if (!ChkIsExtPic(strFileName))
                return false;
            try
            {
                System.Drawing.Image img = new Bitmap(strFileName);
                bool blnIsBigEnough = img.Width >= 600 && img.Height >= 600 ? true : false;
                img.Dispose();
                return blnIsBigEnough;
                
            }
            catch (System.Exception ex)
            {
                AppendLog("解析图片异常" + ex.Message);
                return false;
            }
        }

        private void SetProgressCurrentInThread(int nCurrent)
        {
            this.Dispatcher.Invoke(new Action(() => { progressBar.Value = nCurrent; }));
        }

        private void SetProgressMaxInThread(int nMax)
        {
            this.Dispatcher.Invoke(new Action(() => { progressBar.Maximum = nMax; }));
        }

        private void ScanDir(string strDirName)
        {
            string[] arrDir = Directory.GetDirectories(strDirName);

            if (arrDir.Length == 0 && IsCopySrcDir(strDirName) && ChkLogIfCopied(strDirName) == false)
                _lst_all_path_from.Add(strDirName);
            else
                AppendLog(strDirName + "图片太少，或是根据配置文件记录，已经被处理过，不予以拷贝");
 
            foreach (string d in arrDir)
                ScanDir(d);
        }

        private bool ChkLogIfCopied(string strPath)
        {
            string strLogFullPath = strPath + "\\" + _str_logfile_in_dest_dir;
            string strKey = strPath.Replace(_str_path_from, "");
            return _set_log_in_dest_dir.Contains(strKey);
        }

        private void ScanLogFileInDestDir()
        {
            foreach (string strPath in Directory.GetDirectories(_str_path_to))
                if (File.Exists(strPath + "\\" + _str_logfile_in_dest_dir))
                    _set_log_in_dest_dir.Add(File.ReadAllLines(strPath + "\\" + _str_logfile_in_dest_dir)[0]);
        }

        private bool ChkIsThisDirCopyedBefore(string strDir)
        {
            string strLogFullPath = strDir + "\\" + _str_logfile_in_dest_dir;
            if (File.Exists(strLogFullPath))
                return _lst_all_path_from.Exists((string p) => { return p == strLogFullPath; });
            else 
                return false;
        }

        private bool IsCopySrcDir(string strDirName)
        {
            int nPic = 0;
            foreach (string f in Directory.GetFiles(strDirName))
                if (ChkIsExtPic(f))
                    nPic++;
            return nPic > _n_pic_in_path_no_less_than ? true : false;
        }

        private bool ChkIsExtPic(string strFullName)
        {
            string lower = strFullName.ToLower();
            return lower.EndsWith("jpg") || lower.EndsWith("jpeg") ||　lower.EndsWith("png");
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            for (int i = 5; i < 95; i+=5  )
                selectQuality.Items.Add(i.ToString());
            selectOutputType.Items.Add(_type_jpeg);
            selectOutputType.Items.Add(_type_webp);
             
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        private void selectPath_Drop(object sender, System.Windows.DragEventArgs e)
        {
          
        }

        private void selectPath_DropDownOpened(object sender, EventArgs e)
        {
            selectPath.Items.Clear();
            foreach (string i in Directory.GetDirectories(_str_app_base_path + "\\" + _str_organized_name))
                selectPath.Items.Add(new DirectoryInfo(i).Name);
        }

        private void selectOutputType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _output_type = selectOutputType.SelectedItem.ToString();
        }

        private void selectQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _compress_qualiry = Convert.ToInt64(selectQuality.SelectedItem.ToString());
        }
    }
}
