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
using System.Collections.ObjectModel;
using Gurock.SmartInspect;
//using VidGrab;
using System.Diagnostics;
using WPFMediaKit;
using f_in_box__lib;
using System.Web;
using SmartInspectHelper;

namespace ConferenceClient
{
    /// <summary>
    /// InteractAion logic for UserControla.xaml
    /// </summary>
    public partial class UserControlContent : UserControl
    {
        public  ObservableCollection<ServiceReference1.Content> contentQ;
        private ObservableCollection<ServiceReference1.Content> contentQStore = new ObservableCollection<ServiceReference1.Content>();
        private int MediaStateA = 0; //0 inactive, 1 playing, 2 mediaended
        private int MediaStateB = 0; //0 inactive, 1 playing, 2 mediaended
        private int TargetID = 0;
        private int UserControlState = 0; //not started, becomes 1 if busy, & 2 if completed
        private string MediaType = "video";
        public string ControlDimensions = "";

        //Ticker Background
        public static string TickerBackgroundColour = "Red";
        public static double TickerBackgroundOpacity = 0;
        public Boolean IsTickerBackground = false;

        public ObservableCollection<Asset> UserControlAssetCollection;
        private ObservableCollection<InstanceContentItem> UserControlActiveInstanceContentCollection;
        private Queue<ServiceReference1.Content> ContentQueue = new Queue<ServiceReference1.Content>();

   //     f_in_box__control f_in_box__control1;
   //     f_in_box__control f_in_box__control2;
        int flashstate = 1;

        System.Windows.Threading.DispatcherTimer manualContentEndTimer;
        System.Windows.Threading.DispatcherTimer manualPPTEndTimer;
        System.Windows.Threading.DispatcherTimer manualPPTRestartTimer;
        System.Windows.Threading.DispatcherTimer flashTimer;
        System.Windows.Threading.DispatcherTimer stopFlashTimer;
        System.Windows.Threading.DispatcherTimer imageTimer;

        private System.ComponentModel.Container components = null;

        public UserControlContent()
        {
            InitializeComponent();

            //MainWindow.KeepAliveTicker++;

            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            //WPFMediaKit.DirectShow.MediaPlayers.MediaUriPlayer URIPLAYERA = new WPFMediaKit.DirectShow.MediaPlayers.MediaUriPlayer();
            
            //flashTimer = new System.Windows.Threading.DispatcherTimer();
            //flashTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            //flashTimer.Tick += new EventHandler(flashTimer_Tick);

            //stopFlashTimer = new System.Windows.Threading.DispatcherTimer();
            //stopFlashTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            //stopFlashTimer.Tick += new EventHandler(stopFlashTimer_Tick);

            imageTimer = new System.Windows.Threading.DispatcherTimer();
            imageTimer.Interval = new TimeSpan(0, 0, 10);
            imageTimer.Tick += new EventHandler(imageTimer_Tick);

            //rootWindow = Application.Current.MainWindow as Window1;
            UserControlAssetCollection = new ObservableCollection<Asset>();
            UserControlActiveInstanceContentCollection = new ObservableCollection<InstanceContentItem>();
            // ContentQueue = new Queue<InstanceContentItem>();

            manualContentEndTimer = new System.Windows.Threading.DispatcherTimer();
            manualContentEndTimer.Interval = new TimeSpan(0, 0, 2);
            manualContentEndTimer.Tick += new EventHandler(manualContentEndTimer_Tick);

            manualPPTEndTimer = new System.Windows.Threading.DispatcherTimer();
            manualPPTEndTimer.Interval = new TimeSpan(0, 0, 1);
            manualPPTEndTimer.Tick += new EventHandler(manualPPTEndTimer_Tick);

            manualPPTRestartTimer = new System.Windows.Threading.DispatcherTimer();
            manualPPTRestartTimer.Interval = new TimeSpan(0, 0, 1);
            manualPPTRestartTimer.Tick += new EventHandler(manualPPTRestartTimer_Tick);

           
        }

        //void stopFlashTimer_Tick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (flashstate == 2)
        //        {
        //            f_in_box__control1.FlashMethod_Stop();
        //            f_in_box__control1.FlashMethod_StopPlay();
        //            f_in_box__control1.Dispose();
        //            winformsHost1.Visibility = Visibility.Hidden;
        //        }
        //        else
        //        {
        //            f_in_box__control2.FlashMethod_Stop();
        //            f_in_box__control2.FlashMethod_StopPlay();
        //            f_in_box__control2.Dispose();
        //            winformsHost2.Visibility = Visibility.Hidden;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                Exception e = (Exception)args.ExceptionObject;
                //Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown();
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
            }
        }

        void imageTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                imageTimer.Stop();
                imageA.Visibility = Visibility.Collapsed;
                DequeueContent();
            }
            catch (Exception ex)
            {
            }
        }

        //void flashTimer_Tick(object sender, EventArgs e)
        //{
        //    if (flashstate == 1)
        //    {
        //        try
        //        {
        //            if (f_in_box__control1.FlashMethod_CurrentFrame() > (f_in_box__control1.FlashProperty_TotalFrames - 2))
        //            {
        //                flashstate = 2;
        //                flashTimer.Stop();
        //                SiAuto.Main.LogMessage("Flash 1 End");
        //                DequeueContent();
        //                stopFlashTimer.Start();
        //            }
        //            else
        //            {
        //                MainWindow.KeepAliveTicker++;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            f_in_box__control1.Dispose();
        //            winformsHost1.Visibility = Visibility.Collapsed;
        //            DequeueContent();
        //        }
        //    } else
        //    if (flashstate == 2)
        //    {
        //        try
        //        {
        //            if (f_in_box__control2.FlashMethod_CurrentFrame() > (f_in_box__control2.FlashProperty_TotalFrames - 2))
        //            {
        //                flashstate = 1;
        //                flashTimer.Stop();
        //                SiAuto.Main.LogMessage("Flash 2 End");
        //                DequeueContent();
        //                stopFlashTimer.Start();
        //            }
        //            else
        //            {
        //                MainWindow.KeepAliveTicker++;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            f_in_box__control2.Dispose();
        //            winformsHost2.Visibility = Visibility.Collapsed;
        //            DequeueContent();
        //        }
        //    }
        //}


        //public Boolean PlayFlashOn1(string FlashFilePath)
        //{
        //    try
        //    {
        //        //String FlashFilePath = openFileDialogFlashFile.FileName;

        //        //f_in_box__control1.FlashProperty_Movie = null;
        //        //if (f_in_box__control1 == null)
        //        //{
        //            f_in_box__control1 = new f_in_box__control();
        //            winformsHost1.Child = f_in_box__control1;
        //        //}
        //        f_in_box__control1.FlashMethod_StopPlay();
        //        System.Windows.Forms.Cursor.Hide();
        //        winformsHost1.Cursor = Cursors.None;
                

        //        if (FlashFilePath.EndsWith(".swf"))
        //            f_in_box__control1.FlashProperty_Movie = FlashFilePath;
        //        else
        //        {
        //            // First way -- provide FLV content using ContentProvider
        //            // Additionally, see Sample2_SWF_FLV_Embedding
        //            // This way is useful when you provide FLV from memory, stream, database and so on
        //            // When you should play FLV from URL / local file, use the Second way (see below)
        //            /*
        //                ContentProvider ContentProvider = 
        //                    new ContentProvider(f_in_box__control1.AxCode, FlashFilePath);
					
        //                f_in_box__control1.FlashProperty_FlashVars = "FLVPath=http://FLV/FlashVideo.flv";
        //                f_in_box__control1.PutMovieFromStream(this.GetType().Assembly.GetManifestResourceStream("Sample01_SWF_And_FLV_Player.Embedded_Movies.FLVPlayer.swf"));
        //            */
        //            // Second way -- just provide URL for Flash
        //            //f_in_box__control1.FlashProperty_ScaleMode = 
        //            f_in_box__control1.FlashProperty_FlashVars = "FLVPath=" + System.Web.HttpUtility.HtmlEncode("file://" + FlashFilePath);
        //            f_in_box__control1.PutMovieFromStream(this.GetType().Assembly.GetManifestResourceStream("Sample01_SWF_And_FLV_Player.Embedded_Movies.FLVPlayer.swf"));
        //        }
        //        winformsHost1.Visibility = Visibility.Visible;
        //        f_in_box__control1.FlashMethod_Play();
        //        winformsHost2.Visibility = Visibility.Collapsed;
        //        flashTimer.Start();
        //    }
        //    catch (Exception ex)
        //    {
        //        winformsHost1.Visibility = Visibility.Collapsed;
        //        DequeueContent();
        //    }
        //    return true;
        //}

        //public Boolean PlayFlashOn2(string FlashFilePath)
        //{
        //    try
        //    {
        //        //String FlashFilePath = openFileDialogFlashFile.FileName;

        //        //f_in_box__control1.FlashProperty_Movie = null;
        //        //if (f_in_box__control1 == null)
        //        //{
        //        f_in_box__control2 = new f_in_box__control();
        //        winformsHost2.Child = f_in_box__control2;
        //        //}
        //        f_in_box__control2.FlashMethod_StopPlay();
        //        System.Windows.Forms.Cursor.Hide();
        //        winformsHost2.Cursor = Cursors.None;

        //        if (FlashFilePath.EndsWith(".swf"))
        //            f_in_box__control2.FlashProperty_Movie = FlashFilePath;
        //        else
        //        {
        //            // First way -- provide FLV content using ContentProvider
        //            // Additionally, see Sample2_SWF_FLV_Embedding
        //            // This way is useful when you provide FLV from memory, stream, database and so on
        //            // When you should play FLV from URL / local file, use the Second way (see below)
        //            /*
        //                ContentProvider ContentProvider = 
        //                    new ContentProvider(f_in_box__control1.AxCode, FlashFilePath);
					
        //                f_in_box__control1.FlashProperty_FlashVars = "FLVPath=http://FLV/FlashVideo.flv";
        //                f_in_box__control1.PutMovieFromStream(this.GetType().Assembly.GetManifestResourceStream("Sample01_SWF_And_FLV_Player.Embedded_Movies.FLVPlayer.swf"));
        //            */
        //            // Second way -- just provide URL for Flash
        //            //f_in_box__control1.FlashProperty_ScaleMode = 
        //            f_in_box__control2.FlashProperty_FlashVars = "FLVPath=" + System.Web.HttpUtility.HtmlEncode("file://" + FlashFilePath);
        //            f_in_box__control2.PutMovieFromStream(this.GetType().Assembly.GetManifestResourceStream("Sample01_SWF_And_FLV_Player.Embedded_Movies.FLVPlayer.swf"));
        //        }
        //        winformsHost2.Visibility = Visibility.Visible;
        //        f_in_box__control2.FlashMethod_Play();
        //        winformsHost1.Visibility = Visibility.Collapsed;
        //        flashTimer.Start();
        //    }
        //    catch (Exception ex)
        //    {
        //        winformsHost2.Visibility = Visibility.Collapsed;
        //        DequeueContent();
        //    }
        //    return true;
        //}

        public static bool processIsRunning(string process)
        {
            return (System.Diagnostics.Process.GetProcessesByName(process).Length != 0);
        }

        void manualPPTRestartTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                manualPPTRestartTimer.Stop();
                KillProcess("PPT");
                KillProcess("POWERPNT");
                DequeueContent();
            }
            catch (Exception ex)
            {
            }
        }

        void manualPPTEndTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!IsProcessRunning("PPT"))
                {
                    manualPPTEndTimer.Stop();
                    KillProcess("PPT");
                    KillProcess("POWERPNT");
                    manualPPTRestartTimer.Start();
                };
            }
            catch (Exception ex)
            {
            }
        }

        private static bool IsProcessRunning(string processName)
        {
            try
            {
                bool processFound = false;
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes)
                {
                    processFound = true;
                };
                if (processFound)
                {
                    return true;
                }
                else
                {
                    return false;
                };
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        void manualContentEndTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                manualContentEndTimer.Stop();
                DequeueContent();
            }
            catch (Exception ex)
            {
            }
        }

        //public void SetContentQ(ObservableCollection<string> qIN)
        public void SetContentQ(ObservableCollection<ServiceReference1.Content> qIN)
        {
            //try
            //{
            //    f_in_box__control1.FlashMethod_Stop();
            //    f_in_box__control1.FlashMethod_StopPlay();
            //}
            //catch (Exception ex)
            //{
            //}
            if (IsTickerBackground == true)
            {
                tickerBackgroundRectangle.Visibility = Visibility.Visible;
                BrushConverter bc = new BrushConverter();
                tickerBackgroundRectangle.Fill = (Brush)bc.ConvertFromString(TickerBackgroundColour);
                tickerBackgroundRectangle.Opacity = TickerBackgroundOpacity;
            }
            else
            {
                contentQ = new ObservableCollection<ServiceReference1.Content>();
                contentQStore = new ObservableCollection<ServiceReference1.Content>();
                try
                {
                    foreach (ServiceReference1.Content item in qIN)
                    {
                        contentQ.Add(item);
                        contentQStore.Add(item);
                        si.sii("SET CONTENT QUEUE:" + item);
                    }
                    //if (contentQ.Count == 1)
                    //{
                    //    foreach (string item in qIN)
                    //    {
                    //        contentQ.Add(item);
                    //        contentQStore.Add(item);
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                }
                DequeueContent();
            }
        }

        public void SetMediaAssetCollection(ObservableCollection<Asset> currentAssetCollection, string MetaString)
        {
            try
            {
                UserControlAssetCollection.Clear();
                UserControlAssetCollection = currentAssetCollection;
                foreach (Asset item in UserControlAssetCollection)
                {
                    if (item.Type == "flash" || item.Type == "powerpoint")
                    {
                        item.MetaD = MetaString;
                        SiAuto.Main.LogMessage(item.Type + " Metastring=" + MetaString);
                    };
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void SetAssetCollection(ObservableCollection<Asset> currentAssetCollection, string MetaString)
        {
            try
            {
                UserControlAssetCollection.Clear();
                UserControlAssetCollection = currentAssetCollection;
                foreach (Asset item in UserControlAssetCollection)
                {
                    if (item.Type == "flash" || item.Type == "powerpoint")
                    {
                        item.MetaD = MetaString;
                        SiAuto.Main.LogMessage(item.Type+" Metastring=" + MetaString);
                    };
                }
            }
            catch (Exception ex)
            {
            }
        }

        internal void SetContentCollection(ObservableCollection<InstanceContentItem> currentInstanceContent, int _TargetID)
        {
            try
            {
                TargetID = _TargetID;
                UserControlActiveInstanceContentCollection.Clear();
                foreach (InstanceContentItem item in currentInstanceContent)
                {
                    if (item.Targetid == TargetID)
                    {
                        UserControlActiveInstanceContentCollection.Add(item);
                        SiAuto.Main.LogMessage("adding uc " + item.Assetid);
                    }
                }
                ResetContentQueue();
                
                DequeueContent();
            }
            catch (Exception ex)
            {
            }
        }

        internal void ResetContentQueue()
        {
            try
            {
                SiAuto.Main.LogMessage("Resetting Q");
                ContentQueue.Clear();
                foreach (ServiceReference1.Content item in contentQStore)
                {
                    SiAuto.Main.LogMessage("Qng - "+item);
                    ContentQueue.Enqueue(item);
                }
            }
            catch (Exception ex)
            {
            }
            //DequeueContent();
        }

        public void DequeueContent()
        {
            try
            {
                SiAuto.Main.LogMessage("DQ CONTENT");
                ServiceReference1.Content currentContent = new ServiceReference1.Content();
                if (ContentQueue.Count() <= 0)
                {
                    ResetContentQueue();
                }
                if (ContentQueue.Count > 0)
                {
                    SiAuto.Main.LogMessage("Running Q - DQ");

                    currentContent = ContentQueue.Dequeue();
                    StartMedia(currentContent);
                };
            }
            catch (Exception ex)
            {
                
            }
        }

        public void StartMedia(ServiceReference1.Content contentItemToPlay)
        {
            UserControlState = 1; //Set the State of the Control to Busy
            

            string fileType = "";
            string fileExtension = System.IO.Path.GetExtension(contentItemToPlay.Filelocation).ToLower();
            if (fileExtension == ".swf") fileType = "flash";
            if (fileExtension == ".wmv" || fileExtension == ".mpg" || fileExtension == ".avi") fileType = "video";
            if (fileExtension == ".bmp" || fileExtension == ".jpg" || fileExtension == ".png") fileType = "image";

            string subFolder = "";
            if (fileType == "video") subFolder = "Video";
            if (fileType == "image") subFolder = "Images";
           
            string contentFile = @"c:\content\ConferenceContent\" + subFolder + @"\" + contentItemToPlay.Filelocation;


            Gurock.SmartInspect.SiAuto.Main.LogMessage("StartMedia: " + contentFile);
            //IMAGETimer
            if (fileType == "image")
            {
                ImageSourceConverter imgConv = new ImageSourceConverter();
                ImageSource imageSource = (ImageSource)imgConv.ConvertFromString(@contentFile);
                imageA.Source = imageSource;
                imageA.Visibility = Visibility.Visible;
                mediaElementA.Visibility = Visibility.Collapsed;
                mediaElementB.Visibility = Visibility.Collapsed;
                winformsHost1.Visibility = Visibility.Hidden;
                winformsHost2.Visibility = Visibility.Hidden;
                imageTimer.Interval = new TimeSpan(0, 0, Convert.ToInt16(contentItemToPlay.Metadata1));
                imageTimer.Start();
               // MainWindow.KeepAliveTicker++;
            }

            //VIDEO
            if (fileType == "video")
            {
                if (MediaStateA == 0)
                {
                    MediaStateA = 1;
                    MediaStateB = 0;
                    Double vol = 0;
                    try
                    {
                        vol = Convert.ToDouble(contentItemToPlay.Metadata9);
                    }
                    catch (Exception ex)
                    {
                        vol = 0;
                    }
                    mediaElementA.Volume = vol; 
                    mediaElementA.Source = new Uri(@contentFile);
                    SiAuto.Main.LogMessage("Playing on MediaElement A" + @contentFile);
                    mediaElementA.Play();
                    winformsHost1.Visibility = Visibility.Hidden;
                    winformsHost2.Visibility = Visibility.Hidden;
                    imageA.Visibility = Visibility.Hidden;
                   // MainWindow.KeepAliveTicker++;
                }
                else
                {
                    MediaStateA = 0;
                    MediaStateB = 1;
                    Double vol = 0;
                    try
                    {
                        vol = Convert.ToDouble(contentItemToPlay.Metadata9);
                    }
                    catch (Exception ex)
                    {
                        vol = 0;
                    }
                    mediaElementB.Volume = vol;
                    mediaElementB.Source = new Uri(@contentFile);
                    SiAuto.Main.LogMessage("Playing on MediaElement B" + @contentFile);
                    mediaElementB.Play();
                    winformsHost1.Visibility = Visibility.Hidden;
                    winformsHost2.Visibility = Visibility.Hidden;
                    imageA.Visibility = Visibility.Hidden;
                   // MainWindow.KeepAliveTicker++;
                }
            }

            //FLASH
            if (fileType == "flash")
            {
                {
                    if (flashstate == 1)
                    {
                        //PlayFlashOn1(@contentFile);
                        //MainWindow.KeepAliveTicker++;
                    }
                    else
                    {
                        //PlayFlashOn2(@contentFile);
                        //MainWindow.KeepAliveTicker++;
                    }
                    mediaElementA.Visibility = Visibility.Hidden;
                    mediaElementB.Visibility = Visibility.Hidden;
                    imageA.Visibility = Visibility.Hidden;
                }
            }

            #region DSTV
            ////DSTV
            //if (assetPlaying.Type == "dstv")
            //{
            //    if (MediaStateA == 0)
            //    {
            //        MediaStateA = 1;
            //        MediaStateB = 0;
            //        mediaElementA.Source = new Uri(@assetPlaying.Url);
            //        SiAuto.Main.LogMessage("Playing on MediaElementA");
            //        mediaElementA.Play();
            //        manualContentEndTimer.Interval = new TimeSpan(0, 0, assetPlaying.Duration);
            //        manualContentEndTimer.Start();
            //    }
            //    else
            //    {
            //        MediaStateA = 0;
            //        MediaStateB = 1;
            //        mediaElementB.Source = new Uri(@assetPlaying.Url);
            //        SiAuto.Main.LogMessage("Playing on MediaElementB");
            //        mediaElementB.Play();
            //        manualContentEndTimer.Interval = new TimeSpan(0, 0, assetPlaying.Duration);
            //        manualContentEndTimer.Start();
            //    }
            //} 
            #endregion

            #region Powerpoint
            ////POWERPOINT
            //if (assetPlaying.Type == "powerpoint")
            //{
            //    {
            //        //x y width height flash
            //        // KillProcess("FlashNow");
            //     //   string PowerPointString = assetPlaying.MetaD + " " + @"c:\content\powerpoint\" + assetPlaying.Url;
                    
                    
                    
            //        string PowerPointString =  ControlDimensions+" "+ @"c:\content\powerpoint\" + assetPlaying.Url;
                    
            //        SiAuto.Main.LogMessage(PowerPointString);
            //        try
            //        {
            //            Process.Start(@"c:\content\bin\PPT.exe", PowerPointString);
            //        }
            //        catch (Exception ex)
            //        {
            //            SiAuto.Main.LogMessage(ex.Message);
            //        }
                    
            //        mediaElementA.Visibility = Visibility.Hidden;
            //        mediaElementB.Visibility = Visibility.Hidden;
            //       // manualPPTEndTimer.Start();
            //    }
            //}
            #endregion

            #region Image
            ////IMAGE
            //if (assetPlaying.Type == "image" || assetPlaying.Type == "background")
            //{
            //    ImageSourceConverter imgConv = new ImageSourceConverter();
            //    ImageSource imageSource = (ImageSource)imgConv.ConvertFromString(@"c:\content\image\"+assetPlaying.Url);
            //    imageA.Source = imageSource;
            //    if (MediaType == "image")
            //    {
            //        manualContentEndTimer.Interval = new TimeSpan(0, 0, assetPlaying.Duration);
            //        manualContentEndTimer.Start();
            //    };
            //    mediaElementA.Visibility = Visibility.Hidden;
            //    mediaElementB.Visibility = Visibility.Hidden;
            //}
            #endregion

        }

        private static void KillProcess(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void mediaElementA_MediaEnded(object sender, RoutedEventArgs e)
        {
            DequeueContent();
            mediaElementB.Visibility = Visibility.Collapsed;
            mediaElementA.Visibility = Visibility.Collapsed;
            //mediaElementC.Visibility = Visibility.Visible;
            e.Handled = true;
            //DequeueContent();
        }

        private void mediaElementA_MediaOpened(object sender, RoutedEventArgs e)
        {
            //MediaStateA = 1; MediaStateB = 0;
            mediaElementA.Visibility = Visibility.Visible;
            mediaElementB.Visibility = Visibility.Hidden;
            mediaElementB.Source = null;
            try
            {
                long currentT = DateTime.Now.Ticks;
                currentT += mediaElementA.NaturalDuration.TimeSpan.Ticks;
                //MainWindow.KeepAliveTicksThreshold = currentT;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            
            e.Handled = true;
        }

        private void mediaElementB_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            DequeueContent();
            mediaElementB.Visibility = Visibility.Collapsed;
            mediaElementA.Visibility = Visibility.Collapsed;
            //mediaElementC.Visibility = Visibility.Visible;
            e.Handled = true;
        }

        private void mediaElementB_MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            //MediaStateB = 1; MediaStateA = 0;
            mediaElementB.Visibility = Visibility.Visible;
            mediaElementA.Visibility = Visibility.Hidden;
            mediaElementA.Source = null;
            try
            {
                long currentT = DateTime.Now.Ticks;
                currentT += mediaElementB.NaturalDuration.TimeSpan.Ticks;
                //MainWindow.KeepAliveTicksThreshold = currentT;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            e.Handled = true;
        }

        private void mediaElementA_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                flashTimer.Stop();
                manualContentEndTimer.Stop();
                manualPPTEndTimer.Stop();
                manualPPTRestartTimer.Stop();
                flashTimer.Stop();
                imageTimer.Stop();
                //f_in_box__control1.FlashMethod_Stop();
                //f_in_box__control1.FlashMethod_StopPlay();
                //f_in_box__control1.Dispose();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
