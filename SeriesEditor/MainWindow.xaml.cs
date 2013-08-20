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

namespace SeriesEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string _qiniu_url = "";

        static List<BitmapImage> _lst_bitmap = new List<BitmapImage>();

        public MainWindow()
        {
            

            InitializeComponent();

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

    }
}
