using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace LoopImage
{
    /// <summary>
    /// Award.xaml 的交互逻辑
    /// </summary>
    public partial class Award : Page
    {
        ObservableCollection<LVData> LVDatas = new ObservableCollection<LVData>();

        public void BindFileManager()
        {
            lstFileManager.ItemsSource = LVDatas;
        }
        public Award()
        {
            InitializeComponent();
        }

        public Award(IList<string> images):this()
        {
           // List<Image> imagelist = new List<Image>();
            
            BindFileManager();
            foreach(var i in images)
            {
                Image image = new Image();
                image.Width = 400;
                image.Height = 300;
                image.Source = new BitmapImage(new Uri(i));
                //imagelist.Add(image);
                LVDatas.Add(new LVData { 
                    Name = System.IO.Path.GetFileNameWithoutExtension(i)
                    ,Pic = i 
                    , imageHeight=200
                    , imageWidth = 200
                });
            }
            //this.listView.ItemsSource = imagelist;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double _h;
            double _w;
            _h = e.NewSize.Height-100;
            _w = e.NewSize.Width - 100;
            
            double h = e.NewSize.Height;
            double w = e.NewSize.Width;
            if (LVDatas.Count<4)
            {
                h = _h;
                w = _w / LVDatas.Count;
            }
            else if (LVDatas.Count == 4)
            {
                h = _h / 2;
                w = _w / 2;
            }
            else if (LVDatas.Count == 5)
            {
                h = _h / 2;
                w = _w / 3;
            }
            foreach (var i in LVDatas)
            {
                i.imageHeight = h;
                i.imageWidth = w;
            }
            lstFileManager.ItemsSource = null;
            lstFileManager.ItemsSource = LVDatas;
        }
    }

    public class LVData
        {
            public string Name { get; set; }
            public string Pic { get; set; }
            public double imageWidth { get; set; }
            public double imageHeight { get; set; }
        }
    
}
