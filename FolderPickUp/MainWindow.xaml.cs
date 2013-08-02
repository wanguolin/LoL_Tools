using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Forms;
using System.IO;

using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;



namespace Generator
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string _path_name_ready =  "已整理";
        const string _path_name_compressed = "已压缩";
        string _path_name_full_ready = System.Windows.Forms.Application.StartupPath + "\\" + _path_name_ready;
        string _path_name_full_compressed = System.Windows.Forms.Application.StartupPath + "\\" + _path_name_compressed;

        string _str_start_path = "";
        string _str_dir_pick_from = "";
        string _str_dir_compress_to = "";

   
        int _n_pick_depth = -1;
        int _n_depth_from_picked = 0;
        long _compress_quality = 0;


        List<string> _all_image_to_be_check = new List<string>();
        List<string> _to_be_deleted = new List<string>();
        List<string> _list_to_be_pick = new List<string>();
        List<string> _list_pick_to = new List<string>();
        List<string> _list_to_be_compressed =  new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Init()
        {
           _n_pick_depth = -1;
           _n_depth_from_picked = 0;

           _all_image_to_be_check.Clear();
           _to_be_deleted.Clear();
           _list_to_be_pick.Clear();
           _list_pick_to.Clear();
            txtboxLog.Clear();
            _list_to_be_compressed.Clear();
            _str_start_path = _path_name_full_ready + "\\" + selectPath.SelectedItem.ToString();
        }

        private void btnInsertDirList_Click(object sender, RoutedEventArgs e)
        {
           
            FolderBrowserDialog diagFolder = new FolderBrowserDialog();
            if (diagFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtboxLog.AppendText("待处理路径已设置为：" + (_str_start_path = diagFolder.SelectedPath) + "\n");
        }

        private void btnStartCheck_Click(object sender, RoutedEventArgs e)
        {
            if (selectPath.SelectedItem == null)
            {
                txtboxLog.AppendText("请设置待处理路径！\n");
                return;
            }
            Init();
         
            StartByPath(_str_start_path);
        }
        
        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtboxLog.Clear();
        }

        private void StartByPath( string strPath)
        {
           
            Thread thread = new Thread(new ThreadStart(_ThreadGetPicStructure));
            thread.IsBackground = true;
            thread.Start();
        }



        private void MoveAll()
        {
            Thread thread = new Thread(new ThreadStart(_ThreadMoveAllToBe));
            thread.IsBackground = true;
            thread.Start();
        }

        private void _ThreadMoveAllToBe()
        {

            DirectoryInfo dirToBeMove = new DirectoryInfo(_str_start_path);
            
            string strMoveTo = dirToBeMove.Parent.FullName + "\\" + dirToBeMove.Name + "_to_be_del\\";
            if (!Directory.Exists(strMoveTo))
                Directory.CreateDirectory(strMoveTo);
            foreach (string singleFile in _to_be_deleted)
            {
                try
                {
                    DirectoryInfo dirGetName = new DirectoryInfo(singleFile);
                    string strMoveFileToFullName =  strMoveTo + dirGetName.Parent.Name + "_" + dirGetName.Name;
                    File.Move(singleFile, strMoveFileToFullName);
                    InsNewLineIntoTextBoxInThread(singleFile, "移动到了：" + strMoveFileToFullName);
                }
                catch (System.Exception ex)
                {
                    InsNewLineIntoTextBoxInThread(singleFile, "该文件删除失败。" + ex.Message);
                }
            }
            InsNewLineIntoTextBoxInThread("总计：", _to_be_deleted.Count.ToString());
            _to_be_deleted.Clear();

            this.Dispatcher.Invoke(new Action(() =>
            {
                btnMove.IsEnabled = false;
            }));
            
        }

        private bool ChkIsRightDepth(string strPath, bool blnIsChkPath) 
        {
            string[] arrName = blnIsChkPath ? Directory.GetDirectories(strPath) : Directory.GetFiles( strPath);

            foreach (string strFileName in arrName)
                InsNewLineIntoTextBoxInThread(strFileName, "分类不当.\n");

            return arrName.Length == 0;
        }

        private void InsNewLineIntoTextBoxInThread(string strFileInfo, string strWarningTxt)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txtboxLog.AppendText(strFileInfo + " " + strWarningTxt + "\n");//.Text += strFileInfo + " " + strWarningTxt + "\n";
            }));
           
        }

        private string GetPathNameFromFullName(string strPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(strPath);
            return dirInfo.Name;
        }

        private void InitProgressInThread(int nMax, int nMin)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                progressBar.Maximum = nMax;
                progressBar.Minimum = nMin;
            }));
        }

        private void RefreshProgressInThread( int nCurrent)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                progressBar.Value = nCurrent;
            }));
        }

        private void _ThreadGetPicStructure() 
        {
            _to_be_deleted.Clear();
            this.Dispatcher.Invoke(new Action(() =>
            {
                btnStartCheck.IsEnabled = false;
            }));
            bool blnIsRightDepth = true;
            //blnIsRightDepth |= ChkIsRightDepth(_str_start_path, false);
          
            //foreach (string strSiteFullName in Directory.GetDirectories(_str_start_path))
            {
                blnIsRightDepth &= ChkIsRightDepth(_str_start_path, false);

                foreach (string strSeriesFullName in Directory.GetDirectories(_str_start_path))
                {
                    
                    blnIsRightDepth &= ChkIsRightDepth(strSeriesFullName, true);
       
                    foreach (string strPic in Directory.GetFiles(strSeriesFullName))
                    {
                        DirectoryInfo dinfoFile = new DirectoryInfo(strPic);
                        string strFileExt = dinfoFile.Extension.ToLower();
                        if (strFileExt != ".jpg" && strFileExt != ".jpeg" && strFileExt != ".png" && strFileExt != ".gif" && strFileExt != ".bmp")
                        {
                            InsNewLineIntoTextBoxInThread(strPic, "文件扩展名不对，应当删除，请查看");
                            _to_be_deleted.Add(strPic);
                        }
                        else
                        {
                            _all_image_to_be_check.Add(strPic);
                        }
                    }
                }
            }
            InsNewLineIntoTextBoxInThread(_str_start_path,  blnIsRightDepth ? "分类信息初始化完毕。" : "仍然存在分类不正确的地方，请检查！");

            if (blnIsRightDepth== false)
                return;
            
            CheckImage();

            if (_to_be_deleted.Count != 0)
                this.Dispatcher.Invoke(new Action(() =>
                {
                    btnStartCheck.IsEnabled = btnMove.IsEnabled = btnCompress.IsEnabled = true;
                }));

        }

      

        private void CheckImage()
        {
            int cntProgress = 0;
            InitProgressInThread(_all_image_to_be_check.Count, 0);

            foreach ( string singleFile in _all_image_to_be_check)
            {
                try
                {
                    System.Drawing.Image imageDraw = System.Drawing.Image.FromFile(singleFile);
                    if (imageDraw.Width <= 600 || imageDraw.Height <= 600)
                    {
                        InsNewLineIntoTextBoxInThread(singleFile, "因为过小而疑似广告。");
                        _to_be_deleted.Add(singleFile);
                    }
                    imageDraw.Dispose();
                    _list_to_be_compressed.Add(singleFile);
                }
                catch (System.Exception ex)
                {
                    InsNewLineIntoTextBoxInThread(singleFile, "解析图片格式异常。" + ex.Message);
                    _to_be_deleted.Add(singleFile);
                }
                RefreshProgressInThread(++cntProgress);
            }

            InsNewLineIntoTextBoxInThread("检查了：", _all_image_to_be_check.Count.ToString() + "个图片的大小和格式.");
        }

        private void PickDir()
        {
            Thread thread = new Thread(new ThreadStart(PickDirInThread));
            thread.IsBackground = true;
            thread.Start();
        }

        private void GetDirListFromDepth(string strFromPath, int nDepth)
        {
            if (nDepth == -1)
                return;
            foreach (string strPath in Directory.GetDirectories(strFromPath))
            {
                if (nDepth == 0)
                    foreach (string strFileName in Directory.GetFiles(strPath))
                        _list_to_be_pick.Add(strFileName);
                else
                    GetDirListFromDepth(strPath, --nDepth);
            }
         }

        private void PickDirInThread()
        {
            GetDirListFromDepth(_str_dir_pick_from, _n_pick_depth);

            foreach( string strFileToBeMove in _list_to_be_pick) 
            {
                Stack<string> reversePrefix = new Stack<string>();
                DirectoryInfo dir = new DirectoryInfo(strFileToBeMove);
                //string strParentPrefix = Directory.GetParent(strMove).Name;
                string strParentPrefix = "";
                reversePrefix.Push(Directory.GetParent(strFileToBeMove).Name);
                string strFullName = Directory.GetParent(strFileToBeMove).FullName;
                for (int i = 0; i < _n_depth_from_picked; ++i )
                {
                    reversePrefix.Push(Directory.GetParent(strFullName).Name);
                    strFullName = Directory.GetParent(strFullName).FullName;
                }
                while (reversePrefix.Count != 0)
                {
                    strParentPrefix += reversePrefix.Pop() + "_";
                    if (chkLastName.IsChecked == true)
                        break;
                }
                string strMoveTo = strFullName + "\\" + strParentPrefix + "_" + dir.Name;
                _list_pick_to.Add(strMoveTo);
                InsNewLineIntoTextBoxInThread(strFileToBeMove, "将会被移动到：" + strMoveTo);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    btnStartPick.IsEnabled = true;
                }));
                //Directory.Move(strMove, _str_dir_pick_to + "\\" + strParentPrefix + dir.Name);
            }
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void labFromDir_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FolderBrowserDialog diagFolder = new FolderBrowserDialog();
            if (diagFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                labFromDir.Content = _str_dir_pick_from = diagFolder.SelectedPath;
                dropListDepth.Items.Clear();
                for (int i = 1; i <= 7; ++i)
                    dropListDepth.Items.Add(i.ToString());
            }
      
        }

      
        private void btnStartPickToLog_Click(object sender, RoutedEventArgs e)
        {
            PickDir();
        }

        private void dropListDepth_Changed(object sender, SelectionChangedEventArgs e)
        {
            _n_pick_depth = dropListDepth.SelectedIndex + 1;
            for (int i = 1; i < 7; ++i)
                dropListDepthToPickedFile.Items.Add(i.ToString());
        }

        private void btnStartToLog_Click(object sender, RoutedEventArgs e)
        {
            chkLastName.IsEnabled = false;
            Thread thread = new Thread(new ThreadStart(PickAction));
            thread.IsBackground = true;
            thread.Start();
            chkLastName.IsEnabled = true;
        }

        private void PickAction()
        {
            if (_list_pick_to.Count != _list_to_be_pick.Count)
                InsNewLineIntoTextBoxInThread("出现逻辑性bug，无法移动", "原因是待移动和移动目标数目不对");
            for (int i = 0; i < _list_to_be_pick.Count; ++i )
            {
                try
                {
                    File.Move(_list_to_be_pick[i], _list_pick_to[i]);
                }
                catch (System.Exception ex)
                {
                    InsNewLineIntoTextBoxInThread(_list_to_be_pick[i], "移动到：" + _list_pick_to[i] + ex.ToString());
                }
            }
            _list_pick_to.Clear();
            _list_to_be_pick.Clear();
        }

        private void dropListDepthToPickedFile_Changed(object sender, RoutedEventArgs e)
        {
            _n_depth_from_picked = dropListDepthToPickedFile.SelectedIndex + 1;
            btnStartPickToLog.IsEnabled = true;
        }

        private void btnMove_Click(object sender, RoutedEventArgs e)
        {
            MoveAll();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            foreach (string i in Directory.GetDirectories(_path_name_full_ready))
                selectPath.Items.Add(new DirectoryInfo(i).Name);
            for (int i = 5; i < 100; i += 5)
                selectQuality.Items.Add(i.ToString());
        }

        private void btnCompress_Click(object sender, RoutedEventArgs e)
        {
            if (selectQuality.SelectedItem == null)
            {
                txtboxLog.AppendText("请选择压缩质量\n");
                return;
            }

            _str_dir_compress_to = _path_name_full_compressed + "\\" + selectPath.SelectedItem.ToString();
            if (Directory.Exists(_str_dir_compress_to))
            {
                txtboxLog.AppendText(_str_dir_compress_to + "已经存在了，请检查是否已经压缩过");
                return;
            }
            Directory.CreateDirectory(_str_dir_compress_to);
            _compress_quality = Convert.ToInt64(selectQuality.SelectedItem.ToString());

            Thread thread = new Thread(new ThreadStart(CompressFile));
            thread.IsBackground = true;
            thread.Start();

        }

        
        private void CompressFile()
        {
            long lCompressSourceSize, lCompressDestinateSize = lCompressSourceSize = 0;
            InitProgressInThread(_list_to_be_compressed.Count, 0);
            RefreshProgressInThread(0);
            int i = 0;

            InsNewLineIntoTextBoxInThread("","开始压缩");
            foreach (string fileToBeCompressed in _list_to_be_compressed)
            {
                string fileCompressedToFullPath = fileToBeCompressed.Replace("已整理", "已压缩");
                string strCompressToPath = Directory.GetParent(fileCompressedToFullPath).FullName;
                if (!Directory.Exists(strCompressToPath))
                    Directory.CreateDirectory(strCompressToPath);
                FileInfo fiOriFile = new FileInfo(fileToBeCompressed);
                fileCompressedToFullPath = strCompressToPath + "\\" + fiOriFile.Name + "_" + _compress_quality.ToString() + "_compressed" + fiOriFile.Extension;
                try
                {
                    Bitmap bmpCompress = new Bitmap(fileToBeCompressed);
                    ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

                    System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;

                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, _compress_quality);
                    myEncoderParameters.Param[0] = myEncoderParameter;
                    bmpCompress.Save(fileCompressedToFullPath, jgpEncoder, myEncoderParameters);

                    FileInfo fCompressSourceInfo = new FileInfo(fileToBeCompressed);
                    lCompressSourceSize += fCompressSourceInfo.Length;

                    FileInfo fCompressDestinateInfo = new FileInfo(fileCompressedToFullPath);
                    lCompressDestinateSize += fCompressDestinateInfo.Length;
                }
                catch (System.Exception ex)
                {
                    InsNewLineIntoTextBoxInThread("压缩文件时出现异常，请检查：", fileToBeCompressed + " 异常信息：" + ex.Message);
                }
                RefreshProgressInThread(++i);
            }
            InsNewLineIntoTextBoxInThread("压缩完成：", ((lCompressSourceSize / 1024) / 1024).ToString() + "MB => " + ((lCompressDestinateSize / 1024) / 1024).ToString() + "MB");
        }

        private void CreateFolderIfNotExist(string chkFullPath)
        {
            if (!Directory.GetParent(chkFullPath).Exists)
                Directory.GetParent(chkFullPath).Create();
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void selectQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnCompress.IsEnabled = true;
        }
    
    }
}
