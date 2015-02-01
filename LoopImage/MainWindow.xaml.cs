using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
using Timer = System.Timers.Timer;

namespace LoopImage
{
    ///Thanks for code from 
    ///begin
    ///http://stackoverflow.com/questions/14488677/looping-through-a-folder-of-images-in-c-sharp-wpf
    ///http://social.msdn.microsoft.com/forums/windowsazure/tr-tr/b8dc4211-4001-4cab-8497-44ba6f1a7d4b/wpf
    ///end
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public delegate void AsyncMethodCaller();

        private static NamedPipeServerStream stream;

        private Thread thread;
        int currentAward = 10;
        bool isRanding;
        private List<string> files = new List<string>();
        private Timer timer;

        private int counter;

        BitmapImage _MainImageSource = null;
        public BitmapImage MainImageSource  // Using Uri in the binding was no possible because the Source property of an Image is of type ImageSource. (Yes it is possible to write directly the path in the XAML to define the source, but it is a feature of XAML (called a TypeConverter), not WPF)
        {
            get
            {
                return _MainImageSource;
            }
            set
            {
                _MainImageSource = value;
                OnPropertyChanged("MainImageSource");  // Don't forget this line to notify WPF the value has changed.
            }
        }

        string _DevHelper = null;
        public string DevHelper
        {
            get
            {
                return _DevHelper;
            }
            set
            {
                _DevHelper = value;
                OnPropertyChanged("DevHelper");
            }
        }
        
        public MainWindow()
        {
            InitializeComponent();
            PageFrame.IsEnabled = false;
            DataContext = this;  
            ImageBrush b3 = new ImageBrush();
            b3.ImageSource = new BitmapImage(new Uri(@"C:\Loop\background.jpg", UriKind.RelativeOrAbsolute));
            this.Background = b3;
            ClearBlackList();
            
            // The DataContext allow WPF to know the initial object the binding is applied on. Here, in the Binding, you have written "Path=MainImageSource", OK, the "MainImageSource" of which object? Of the object defined by the DataContext.

            Loaded += MainWindow_Loaded;

            Action delegateBeginOrStop = () =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.beginOrStop();
                });
            };

            Action threadThread = () =>
            {
                startNewIDServer(delegateBeginOrStop);
            };



            thread = new Thread(new ThreadStart(threadThread));
            thread.Start();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            thread.Abort();

            base.OnClosing(e);
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            stream.Close();
            thread.Abort();
        }

        private static void startNewIDServer(Action func)
        {
            PipeSecurity ps = new PipeSecurity();
            //Is this okay to do?  Everyone Read/Write?
            PipeAccessRule psRule = new PipeAccessRule(@"Everyone", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            
            ps.AddAccessRule(psRule);

            stream = null;

            stream = new NamedPipeServerStream("testpipe",
                 PipeDirection.InOut,
                 1,
                 PipeTransmissionMode.Message,
                 PipeOptions.None,1,1,ps);

            while (true)
            {
                if (stream != null)
                {
                    stream.WaitForConnection();

                    func();



                    stream.Disconnect();
                }

            }
        }

        //private void startNamedPipe()
        //{
        //    using (NamedPipeServerStream pipeServer =
        //    new NamedPipeServerStream("testpipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
        //    {
        //        //AsyncMethodCaller caller = new AsyncMethodCaller(this.beginOrStop);
        //        //IAsyncResult asyncResult = caller.BeginInvoke(null, null);
        //        //AsyncCallback callBack = new AsyncCallback(asyncResult);
        //        Action<IAsyncResult> beginAction = (IAsyncResult val) =>
        //        {
        //            this.beginOrStop();
        //        };
        //        AsyncCallback beginAsyncCallbackcallBack = new AsyncCallback(beginAction);

        //        Action endAction = () =>
        //        {
        //            this.startNamedPipe();
        //        };
        //        //AsyncMethodCaller caller = new AsyncMethodCaller(endAction);
                
        //        pipeServer.BeginWaitForConnection(beginAsyncCallbackcallBack, null);
        //        pipeServer.EndWaitForConnection(endAction.BeginInvoke(null,null));

        //    }
        //}

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            setupPics();
        }

        private void setupPics()
        {
            timer = new Timer();
            //timer.Elapsed += timer_Tick;
            timer.Interval = 200;

            // Initialize "files", "Imagecounter", "counter" before starting the timer because the timer is not working in the same thread and it accesses these fields.
            files = Directory.GetFiles(@"C:\Loop\AwardPool\").ToList();
            shuffleList(files);
            counter = 0;

            timer.Start();  // timer.Start() and timer.Enabled are equivalent, only one is necessary
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            // WPF requires all the function that modify (or even read sometimes) the visual interface to be called in a WPF dedicated thread.
            // IntroScreen() and MainWindow_Loaded(...) are executed by this thread
            // But, as I have said before, the Tick event of the Timer is called in another thread (a thread from the thread pool), then you can't directly modify the MainImageSource in this thread
            // Why? Because a modification of its value calls OnPropertyChanged that raise the event PropertyChanged that will try to update the Binding (that is directly linked with WPF)
            Dispatcher.Invoke(new Action(() =>  // Call a special portion of your code from the WPF thread (called dispatcher)
            {
                // Now that I have changed the type of MainImageSource, we have to load the bitmap ourselves.
                //Thread.Sleep(1000);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(files[counter], UriKind.Relative);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;  // Don't know why. Found here (http://stackoverflow.com/questions/569561/dynamic-loading-of-images-in-wpf)
                bitmapImage.EndInit();
                MainImageSource = bitmapImage;  // Set the property (because if you set the field "_MainImageSource", there will be no call to OnPropertyChanged("MainImageSource"), then, no update of the binding.
            }));
            if (++counter == files.Count)
            {
                shuffleList(files);
                counter = 0;
            }
            if (!this.isRanding)
            {
                this.MainImageSource = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        bool isFullScreen;
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Space)
            {
                beginOrStop();
            }
            if(e.Key == Key.F11)
            {
                if(isFullScreen)
                {
                    NomalScreen();
                    isFullScreen = false;
                }
                else
                {
                     FullScreen(); 
                    isFullScreen = true;
                }
            }
            if(e.Key == Key.Escape)
            {
                minScreen();
            }
        }

        private void minScreen()
        {
            PageFrame.IsEnabled = false;
            PageFrame.Content = null;
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void beginOrStop()
        {
            if (isRanding)
            {
                isRanding = false;
                this.MusicPlayer.Source = null;
                timer.Elapsed -= timer_Tick;
                PageFrame.IsEnabled = true;
                MainImageSource = null;
                //PageFrame.Source = new Uri("Award.xaml", UriKind.Relative);
                ShowAward();


            }
            else
            {
                isRanding = true;
                DevHelper = "当前播放的音乐是" + string.Format(@"C:\Loop\Music\{0}.mp3", currentAward);
                this.MusicPlayer.Source = new Uri(string.Format(@"C:\Loop\Music\{0}.mp3", currentAward));
                timer.Elapsed += timer_Tick;
                PageFrame.IsEnabled = false;
                PageFrame.Content = null;
                //PageFrame.Source = new Uri("Award.xaml", UriKind.Relative);
            }
        }
        private void FullScreen()
        {
            TopPanel.Height = 0;
            DevTip.Height = 0;
            this.WindowState = System.Windows.WindowState.Normal;
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.Topmost = true;

            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
        }

        private void NomalScreen()
        {
            TopPanel.Height = 20;
            DevTip.Height = 30;
            this.WindowState = System.Windows.WindowState.Normal;
            this.WindowStyle = System.Windows.WindowStyle.ToolWindow;
            this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;
            this.Topmost = true;

            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
        }

        private void ShowAward()
        {
            MainImageSource = null;
            List<string> l = new List<string>();
            if(currentAward == 10)//5等奖
            {
                DevHelper = "当前5等奖1次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 9)//5等奖
            {
                DevHelper = "当前5等奖2次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 8)//4等奖
            {
                DevHelper = "当前4等奖1次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 7)//4等奖
            {
                DevHelper = "当前4等奖2次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 6)//3等奖
            {
                DevHelper = "当前3等奖1次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 5)//3等奖
            {
                DevHelper = "当前3等奖2次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 4)//2等奖
            {
                DevHelper = "当前2等奖1次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 3)//2等奖
            {
                DevHelper = "当前2等奖2次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 2)//1等奖
            {
                DevHelper = "当前1等奖1次";
                l.Add(GetAwardAndGoNext(counter));
            }
            if (currentAward == 1)//special等奖
            {
                DevHelper = "当前1等奖2次";
                l.Add(GetAwardAndGoNext(counter));
            }
            if(currentAward == 0)
            {
                DevHelper = "当前top等奖1次";
                l.Add(GetAwardAndGoNext(counter));
            }
            if(currentAward == -1)
            {
                DevHelper = "当前sepcial等奖1次";
                l.Add(GetAwardAndGoNext(counter));
                l.Add(GetAwardAndGoNext(counter));
            }
            PageFrame.Content = new Award(l) {  ShowsNavigationUI=false};

            DevHelper = DevHelper + "现在的奖项是" + currentAward;
            DevHelper = DevHelper + "当前奖池中有效人数为" + files.Count();
            currentAward = currentAward - 1;
        }

        private string GetAwardAndGoNext(int value)
        {
            string result;
            if (value < files.Count())
            {
                result = files[value];
                Log(files[value]);
            }
            else
            {
                shuffleList(files);
                counter = 0;
                result = files[counter];
                Log(files[counter]);
            }
            counter = counter + 1;
            return result;

        }

        private void Log(string file)
        {
            files.Remove(file);
            FileStream fs1 = new FileStream(@"C:\Loop\loop.txt", FileMode.Append, FileAccess.Write);//创建写入文件 
            StreamWriter sw = new StreamWriter(fs1);
            sw.WriteLine("currentAwardLevel: " + currentAward + "; AwardPerson: " + System.IO.Path.GetFileNameWithoutExtension(file));
            sw.Close();
            fs1.Close();


            fs1 = new FileStream(@"C:\Loop\blacklist.txt", FileMode.Append, FileAccess.Write);//创建写入文件 
            sw = new StreamWriter(fs1);
            sw.WriteLine(file);
            sw.Close();
            fs1.Close();


            fs1 = new FileStream(@"C:\Loop\remember.txt", FileMode.Create, FileAccess.Write);//创建写入文件 
            sw = new StreamWriter(fs1);
            sw.WriteLine(currentAward.ToString());
            sw.Close();
            fs1.Close();
        }

        private void shuffleList(IList<string> value)
        {
            files = value.OrderBy(t => Guid.NewGuid()).ToList();
            DevHelper = "列表被shuffle了！";
        }

        private void ClearBlackList()
        {
            FileStream fs1 = new FileStream(@"C:\Loop\blacklist.txt", FileMode.Create, FileAccess.Write);//创建写入文件 
            fs1.Close();
        }

        private void GetAllBlackList()
        {

        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            List<string> _blackList = new List<string>();

            string result;

            int i = 0;

            using (StreamReader sr = new StreamReader(@"C:\Loop\blacklist.txt"))
            {
                while (sr.Peek() >= 0)
                {
                    string _s = sr.ReadLine();
                    _blackList.Add(_s);

                    if (files.Contains(_s))
                    {
                        files.Remove(_s);
                        i++;
                    }
                }
            }


            result = string.Format("{0} item(s) have been removed. ", i) + Environment.NewLine;

            result += string.Join(Environment.NewLine, _blackList);

            MessageBox.Show(result);

           int lastRemember = 0;

            using (StreamReader sr = new StreamReader(@"C:\Loop\remember.txt"))
            {
                while (sr.Peek() >= 0)
                {
                    string _s = sr.ReadLine();
                    int.TryParse(_s, out lastRemember);
                }
            }

            string message = "记录到上次执行到第" + lastRemember+"次, 是否继续上次进度？";
            string caption = "Alert";
            //DialogResult result;

            // Displays the MessageBox.

            var result1 = System.Windows.MessageBox.Show(message, caption, MessageBoxButton.YesNo);

            if (result1 == MessageBoxResult.Yes)
            {
                currentAward = lastRemember - 1;
            }
            
        }

        private void MusicPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.MusicPlayer.Source = new Uri(string.Format(@"C:\Loop\Music\{0}.mp3", currentAward));
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                beginOrStop();
            if (e.RightButton == MouseButtonState.Pressed)
                minScreen();
        }
    }
}
