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
using System.ServiceModel;
using SmartInspectHelper;
using System.IO;
using System.Collections.ObjectModel;
using Chilkat;

namespace ConferenceClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Configuration
        ServiceReference1.CoreServiceClient proxy = new ServiceReference1.CoreServiceClient();
        string CORESERVICE_IP_ADDRESS = "127.0.0.1";
        string CORESERVICE_PORT = "8888";
        string FTP_IP_ADDRESS = "127.0.0.1";
        string FTP_USERNAME = "ftpuser";
        string FTP_PASSWORD = "bugaboo";
        Ftp2 _ftp;

        //UserControls
		GridMeetingControl m1 = new GridMeetingControl();
		GridMeetingControl m2 = new GridMeetingControl();
        GridMeetingControl m3 = new GridMeetingControl();
        GridMeetingControl m4 = new GridMeetingControl();

        GridDirectionalControl d1 = new GridDirectionalControl();
        GridDirectionalControl d2 = new GridDirectionalControl();
        GridDirectionalControl d3 = new GridDirectionalControl();
        GridDirectionalControl d4 = new GridDirectionalControl();

        //Media items
        ServiceReference1.CSMedialoopCollection _allCSContentCollection = new ServiceReference1.CSMedialoopCollection();
        Queue<ServiceReference1.CSMedialoop> _activeCSContentQ = new Queue<ServiceReference1.CSMedialoop>();

        ObservableCollection<ServiceReference1.Content> contentCollection = new ObservableCollection<ServiceReference1.Content>();
        ServiceReference1.LoopContentCollection loopContentCollection = new ServiceReference1.LoopContentCollection();
        ObservableCollection<ServiceReference1.Content> contentQ = new ObservableCollection<ServiceReference1.Content>();
        ObservableCollection<ServiceReference1.Content> contentQ2 = new ObservableCollection<ServiceReference1.Content>();
        ObservableCollection<ServiceReference1.Content> contentQ3 = new ObservableCollection<ServiceReference1.Content>();
        ObservableCollection<ServiceReference1.Content> contentQ4 = new ObservableCollection<ServiceReference1.Content>();
        ObservableCollection<ServiceReference1.Content> contentQ5 = new ObservableCollection<ServiceReference1.Content>();

        UserControlContent ucc1 = new UserControlContent();
        UserControlContent ucc2 = new UserControlContent();
        UserControlContent ucc3 = new UserControlContent();
        UserControlContent ucc4 = new UserControlContent();

        string Venue1Name = "";
        Boolean _directional = false;
        public int _CurrentEventCount = 0;

        ServiceReference1.CSScreenCollection _screens = new ServiceReference1.CSScreenCollection();
        ServiceReference1.CSTemplateCollection _templateCollection = new ServiceReference1.CSTemplateCollection();
        ServiceReference1.CSDirectionCollection _directionCollection = new ServiceReference1.CSDirectionCollection();
        ServiceReference1.CSEventCollection _events = new ServiceReference1.CSEventCollection();
        public System.Windows.Threading.DispatcherTimer updateTimer;
        public System.Windows.Threading.DispatcherTimer mediaTimer;
        public System.Windows.Threading.DispatcherTimer flipTimer;

        Int16 DirectionalFlipOrFlop = 0; //used to Flip Between Directional Content when more than six
        Boolean DirectionalFlipEnabled = true;
        Int16 DirectionalFlipMax = 6;

        Boolean firstMediaUpdate = true;
        Boolean firstMediaContentQCollection = true;
        Boolean firstDirectionalEventsCollection = true; //Flags used to determine startup so that timer doesn't have to roll around

        DateTime lastTemplateCollection = DateTime.Now.AddMinutes(-20);
        DateTime lastScreenCollection = DateTime.Now.AddMinutes(-20);
        DateTime lastMediaLoopCollection = DateTime.Now.AddMinutes(-20);
        DateTime lastDirectionCollection = DateTime.Now.AddMinutes(-20);

        public MainWindow()
        {
            InitializeComponent();
            ConfigureFolders();
            si.EnableSmartInspect("Conference Client", true);
            TestService();  //This also Adjusts the Proxy Settings
        }

        private void ConfigureFolders()
        {
            si.sie("ConfigureFolders");
            
            if (!Directory.Exists(@"c:\content"))
            {
                Directory.CreateDirectory(@"c:\content");
            }
            if (!Directory.Exists(@"c:\content\ConferenceContent"))
            {
                Directory.CreateDirectory(@"c:\content\ConferenceContent");
            }
            if (!Directory.Exists(@"c:\content\ConferenceContent\Logos"))
            {
                Directory.CreateDirectory(@"c:\content\ConferenceContent\Logos");
            }
            if (!Directory.Exists(@"c:\content\ConferenceContent\Directional"))
            {
                Directory.CreateDirectory(@"c:\content\ConferenceContent\Directional");
            }
            if (!Directory.Exists(@"c:\content\ConferenceContent\Video"))
            {
                Directory.CreateDirectory(@"c:\content\ConferenceContent\Video");
            }
            if (!Directory.Exists(@"c:\content\ConferenceContent\Images"))
            {
                Directory.CreateDirectory(@"c:\content\ConferenceContent\Images");
            }
            if (!Directory.Exists(@"c:\content\ConferenceContent\Tickers"))
            {
                Directory.CreateDirectory(@"c:\content\ConferenceContent\Tickers");
            }
            si.sil("ConfigureFolders");
        }

        private void StartDisplayClientServices()
        {
            si.sie("StartDisplayClientServices");
            
            if (Properties.Settings.Default.Registration.Length <= 2)
            {
                WindowSoftwareRegistration wsr = new WindowSoftwareRegistration();
                wsr.ShowDialog();
            }
            ConfigureDisplayLayouts();
            
            if (_directional == true)
            {
                UpdateDirectionalContent();
                if (GetTemplateValue("flippingEnabled", "default") == "1")
                {
                    DirectionalFlipEnabled = true;
                }
                else
                {
                    DirectionalFlipEnabled = false;
                }
            }
            else
            {
                DirectionalFlipEnabled = false;
                UpdateVenueContent();
            }

            CreateTimers();
            si.sil("StartDisplayClientServices");
        }

        private void CreateTimers()
        {
            si.sie("CreateTimers");
            
            updateTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimer.Interval = new TimeSpan(0, 0, 3);
            updateTimer.Tick += new EventHandler(updateTimer_Tick);
            updateTimer.Start();

            mediaTimer = new System.Windows.Threading.DispatcherTimer();
            mediaTimer.Interval = new TimeSpan(0, 0, 1);
            //mediaTimer.Tick += new EventHandler(mediaTimer_Tick);

            int dur = 10;
            try
            {
                dur = Convert.ToInt16(GetTemplateValue("flippingDuration", "default"));
            }
            catch (Exception)
            {
                dur = 10;
            }
            flipTimer = new System.Windows.Threading.DispatcherTimer();
            flipTimer.Interval = new TimeSpan(0, 0, dur);
            flipTimer.Tick += new EventHandler(flipTimer_Tick);
            if (_directional == true && DirectionalFlipEnabled == true) 
            {
                si.sii("_directional = " + _directional.ToString()); 
                si.sii("_directionalFlipEnabled = " + DirectionalFlipEnabled.ToString());
                si.sii("Template Count = " + _templateCollection.Count.ToString());
                si.sii("Starting Flip/Flop Directional Timer");
                flipTimer.Start();
            } else 
            {
                si.sii("_directional = " + _directional.ToString());
                si.sii("_directionalFlipEnabled = " + DirectionalFlipEnabled.ToString());
                si.sii("Template Count = " + _templateCollection.Count.ToString());
                si.sii("No need to Start Flip/Flop Timer...");
            }

            si.sil("CreateTimers");
        }

        void flipTimer_Tick(object sender, EventArgs e)
        {
            si.sii("Flip Timer Tick");
            if (GetTemplateValue("flippingEnabled", "default") == "1")
            {
                DirectionalFlipEnabled = true;
            }
            else
            {
                DirectionalFlipEnabled = false;
            }
            if (!DirectionalFlipEnabled || _directional == false)
            {
                si.sii("Directional Flip is not enabled - Disabling Flip Timer");
                flipTimer.Stop();
                return;
            }
            if (DateTime.Now.Second>10 && DateTime.Now.Second<=59)
            {
                si.sii("Doing Flip/Flop - call PopulateDirectionalContent()");
                PopulateCurrentDirectionalContent();
            }
        }

        //void mediaTimer_Tick(object sender, EventArgs e)
        //{
        //    si.sie("MEDIA_TIMER TICK");
        //    string s = "";
        //    try
        //    {
        //        mediaTimer.Stop();
        //        if (contentCollection.Count <= 0)
        //        {
        //            //MessageBox.Show(contentCollection.Count.ToString());
        //            foreach (ServiceReference1.Content item in proxy.CollectMedia())
        //            {
        //                contentCollection.Add(item);
        //                s += item.Filelocation + "|";
        //            }
        //            //MessageBox.Show(s);
        //            CollectMediaInContentCollection();
        //        }
        //        else
        //        {
        //            // DisplayBackground();
        //            //  RollMedia();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //MessageBox.Show(ex.Message);
        //    }
        //    // mediaTimer.Start();
        //    //DisplayBackground();
        //    RollMedia();
        //    this.Topmost = true;
        //    this.Activate();
        //    this.Focus();
        //}

        private void RollVenueMedia()
        {
            si.sie("RollVenueMedia()");
            try
            {
                Gurock.SmartInspect.SiAuto.Main.LogMessage("PRIOR TO UCC");
                contentQ = new ObservableCollection<ServiceReference1.Content>();
                foreach (ServiceReference1.CSMedialoop item in _activeCSContentQ)
                {
                    ServiceReference1.Content newContent = new ServiceReference1.Content();
                    newContent.Active = true;
                    newContent.Contenttype = item.Mediatype;
                    newContent.Filelocation = item.Mediafilename;
                    if (item.Mediatype == "image") newContent.Metadata1 = GetTemplateValue("mediaVenueS", "default"); //image duration
                    contentQ.Add(newContent);
                }

                Boolean isIdle = false;
                Boolean okToRunMedia = true;
                if (m1.LabelRoomEventTitle.Content == "Currently" && m1.labelEventInfo.Content == "Available")
                {
                    isIdle = true;
                }

                if (GetTemplateValue("mediaVenueIdle", "default") == "1" && isIdle == false) okToRunMedia = false;

                if (GetTemplateValue("mediaVenueEnabled", "default") == "0" || okToRunMedia == false)
                {
                    //Don't run venue media!
                    contentQ.Clear();
                }
                else
                {
                    //run media
                    DateTime dt = DateTime.Now;
                    int m = dt.Minute;
                    if (firstMediaUpdate == true || m == 0 || m == 5 || m == 10 || m == 15 || m == 20 || m == 25 || m == 30 || m == 35 || m == 40 || m == 45 || m == 50 || m == 55)
                    {
                        firstMediaUpdate = false;
                        ucc1.SetContentQ(contentQ);
                    }
                    
                }

                double x = Convert.ToDouble(0); //sim("targetx=" + target.x);
                double y = Convert.ToDouble(0); //sim("targety=" + target.y);
                double width = Convert.ToDouble(1024);
                double height = Convert.ToDouble(640);

                x = Convert.ToDouble(GetTemplateValue("mediaVenueX","default"));
                y = Convert.ToDouble(GetTemplateValue("mediaVenueY", "default"));
                width = Convert.ToDouble(GetTemplateValue("mediaVenueW", "default"));
                height = Convert.ToDouble(GetTemplateValue("mediaVenueH", "default"));

                m1.gridMedia2.Margin = new Thickness(x, y, 0, 0);
                m1.gridMedia2.Width = width;
                m1.gridMedia2.Height = height;

                if (contentQ.Count > 0)
                {
                    m1.gridMedia2.Visibility = Visibility.Visible;
                }
                else
                {
                    m1.gridMedia2.Visibility = Visibility.Hidden;
                }
                    
            }
            catch (Exception ex)
            {
                Gurock.SmartInspect.SiAuto.Main.LogMessage("Roll Media error:" + ex.Message);
            }
            si.sie("RollVenueMedia()");
        }

        private void RollDirectionalMedia()
        {
            si.sie("RollDirectionalMedia()");
            try
            {
                Gurock.SmartInspect.SiAuto.Main.LogMessage("PRIOR TO UCC");
                contentQ = new ObservableCollection<ServiceReference1.Content>();
                foreach (ServiceReference1.CSMedialoop item in _activeCSContentQ)
                {
                    ServiceReference1.Content newContent = new ServiceReference1.Content();
                    newContent.Active = true;
                    newContent.Contenttype = item.Mediatype;
                    newContent.Filelocation = item.Mediafilename;
                    if (item.Mediatype == "image") newContent.Metadata1 = GetTemplateValue("mediaDirS", "default"); //image duration
                    contentQ.Add(newContent);
                }

                Boolean isIdle = false;
                Boolean okToRunMedia = true;
                si.sii("CURRENT EVENT COUNT=" + _CurrentEventCount.ToString());
                if (_CurrentEventCount <= 0)
                {
                    isIdle = true;
                    si.sii("SETTING IDLE TO TRUE");
                }

                if (GetTemplateValue("mediaDirIdle", "default") == "1" && isIdle == false) okToRunMedia = false;
                
                if (GetTemplateValue("mediaDirEnabled", "default") == "0" || okToRunMedia == false)
                {
                    //Don't run venue media!
                    contentQ.Clear();
                }
                else
                {
                    //run media
                    DateTime dt = DateTime.Now;
                    int m = dt.Minute;
                    if (firstMediaUpdate == true || m == 0 || m == 5 || m == 10 || m == 15 || m == 20 || m == 25 || m == 30 || m == 35 || m == 40 || m == 45 || m == 50 || m == 55)
                    {
                        firstMediaUpdate = false;
                        ucc1.SetContentQ(contentQ);
                    }
                }

                double x = Convert.ToDouble(0); //sim("targetx=" + target.x);
                double y = Convert.ToDouble(0); //sim("targety=" + target.y);
                double width = Convert.ToDouble(1024);
                double height = Convert.ToDouble(640);

                x = Convert.ToDouble(GetTemplateValue("mediaDirX", "default"));
                y = Convert.ToDouble(GetTemplateValue("mediaDirY", "default"));
                width = Convert.ToDouble(GetTemplateValue("mediaDirW", "default"));
                height = Convert.ToDouble(GetTemplateValue("mediaDirH", "default"));

                d1.gridMedia2.Margin = new Thickness(x, y, 0, 0);
                d1.gridMedia2.Width = width;
                d1.gridMedia2.Height = height;

                if (contentQ.Count > 0)
                {
                    d1.gridMedia2.Visibility = Visibility.Visible;
                }
                else
                {
                    d1.gridMedia2.Visibility = Visibility.Hidden;
                }

            }
            catch (Exception ex)
            {
                Gurock.SmartInspect.SiAuto.Main.LogMessage("Roll Media error:" + ex.Message);
            }
            si.sie("RollDirectionalMedia()");
        }

        //private void DisplayBackground() //From DisplayClient - Perhaps not required for Conference
        //{
        //    try
        //    {
        //        string filen = "";
        //        string selectedn = "";
        //        //ServiceReference1.Content _background = new ServiceReference1.Content();
        //        foreach (ServiceReference1.Content item in contentCollection)
        //        {
        //            filen = item.Filelocation;
        //            if (System.IO.Path.GetExtension(filen) == ".jpg" || System.IO.Path.GetExtension(filen) == ".png")
        //            {
        //                selectedn = item.Filelocation;
        //            }
        //        }
        //        selectedn = @"c:\content\_media\" + System.IO.Path.GetFileName(selectedn);
        //        ImageSourceConverter imgConv = new ImageSourceConverter();
        //        ImageSource imageSource = (ImageSource)imgConv.ConvertFromString(@selectedn);
        //        //imageBackground.Source = imageSource;
        //        //imageBackground.Visibility = Visibility.Visible;
        //    }
        //    catch (Exception ex)
        //    {
        //        Gurock.SmartInspect.SiAuto.Main.LogMessage("Display Background error:" + ex.Message);
        //    }
        //}

        void updateTimer_Tick(object sender, EventArgs e)
        {
            updateTimer.Interval = new TimeSpan(0, 0, 3);
            updateTimer.Stop();
            DateTime dtNow = DateTime.Now;
            try
            {
                if (dtNow.Second >= 1 && dtNow.Second <= 5)
                {
                    //ConfigureDisplayLayouts(), but first move mouse
                    updateTimer.Interval = new TimeSpan(0, 0, 5);
                    MouseOperations mo = new MouseOperations();
                    MouseOperations.SetCursorPosition(2048, 2048);

                    if (dtNow.Hour > 2 && dtNow.Hour <= 5)
                    {
                        gridScreenSaver.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        gridScreenSaver.Visibility = Visibility.Collapsed;
                    }

                    if (_directional == true)
                    {
                        UpdateDirectionalContent();
                    }
                    else
                    {
                        UpdateVenueContent();
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
            updateTimer.Start();
        }

        public void ConfigureDisplayLayouts()
        {
            si.sie("ConfigureDisplayLayouts");
            
            try
            {
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Images\default.jpg"));
                m1.ImageBackground.Source = bi;
                d1.ImageBackground.Source = bi;
            }
            catch (Exception ex)
            {
            }

            try
            {
                if (_directional == true)
                {
                    spContent.Children.Add(d1);
                    d1.Width = 1366;
                    d1.gridMedia2.Children.Add(ucc1);
                }
                else
                {
                    spContent.Children.Add(m1);
                    m1.Width = 1366;
                    m1.gridMedia2.Children.Add(ucc1);
                }

                if (Properties.Settings.Default.Registration.Length > 2)
                {
                    m1.tbUnregistered.Visibility = Visibility.Hidden;
                }
                else
                {
                    m1.tbUnregistered.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                si.six("Error whilst Configuring Display Layouts:",ex);
            }
            
            si.sil("ConfigureDisplayLayouts");
        }

        private void PopulateVenueMediaLoopIfRequired()
        {
            DateTime dtCurrent = DateTime.Now;
            if (dtCurrent.Second <= 5 || firstMediaContentQCollection)
            {
                si.sii("Refreshing Venue Media Loop Now");
                firstMediaContentQCollection = false;
                DateTime dtNow = DateTime.Now;
                if (dtNow.AddMinutes(-5).Ticks > lastMediaLoopCollection.Ticks) //Only collect Templates once otherwise calls will go out to Core repeatedly for each screen
                {
                    _allCSContentCollection = proxy.CollectCSMediaLoops();
                    lastMediaLoopCollection = DateTime.Now;
                    RefreshMediaContentQ();
                    RollVenueMedia();
                }
            }
        }

        private void PopulateDirectionalMediaLoopIfRequired()
        {
            DateTime dtCurrent = DateTime.Now;
            if (dtCurrent.Second <= 5 || firstMediaContentQCollection)
            {
                si.sii("Refreshing Directional Media Loop Now");
                firstMediaContentQCollection = false;
                DateTime dtNow = DateTime.Now;
                if (dtNow.AddMinutes(-5).Ticks > lastMediaLoopCollection.Ticks) //Only collect Templates once otherwise calls will go out to Core repeatedly for each screen
                {
                    _allCSContentCollection = proxy.CollectCSMediaLoops();
                    lastMediaLoopCollection = DateTime.Now;
                    RefreshMediaContentQ();
                    RollDirectionalMedia();
                }
            }
        }

        private void RefreshMediaContentQ()
        {
            si.sie("RefreshMediaContentQ()");
            _activeCSContentQ.Clear();
            try
            {
                foreach (ServiceReference1.CSMedialoop item in _allCSContentCollection)
                {
                    _activeCSContentQ.Enqueue(item);
                    si.sii("Adding to _activeContentQueue:"+item.Mediafilename);
                    string subFolder = "";
                    if (item.Mediatype.ToLower() == "video") subFolder = "Video";
                    if (item.Mediatype.ToLower() == "image") subFolder = "Images";
                    if (!File.Exists(@"c:\content\ConferenceContent\" + subFolder + @"\" + item.Mediafilename))
                        CollectFile(item.Mediafilename, item.Mediatype);
                }
            }
            catch (Exception ex)
            {
            }
            si.sil("RefreshMediaContentQ()");
        }

        private void UpdateVenueContent()
        {
            si.sie("UpdateVenueContent");

            try
            {
                if (CheckDisplayRegistration() == false)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                si.six(ex);
            }

            try
            {
                UpdateVenueScreenTitle();
            }
            catch (Exception ex)
            {
                si.six(ex);
            }
            try
            {
                UpdateVenueTemplate("default");
            }
            catch (Exception ex)
            {
                si.six(ex);
            }
            try
            {
                PopulateVenueCurrentContent();
            }
            catch (Exception ex)
            {
                si.six(ex);
            }
            try
            {
                PopulateVenueMediaLoopIfRequired();
            }
            catch (Exception ex)
            {
                si.six(ex);
            }
            si.sil("UpdateVenueContent");
        }

        private void UpdateDirectionalContent()
        {
            si.sie("UpdateDirectionalContent");

            try
            {
                if (CheckDisplayRegistration() == false)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                si.six(ex);
            }

            try
            {
                UpdateVenueScreenTitle();
            }
            catch (Exception ex)
            {
                si.six(ex);
            }

            try
            {
                PopulateCurrentDirectionalContent();
            }
            catch (Exception ex)
            {
                si.six(ex);
            }

            try
            {
                PopulateDirectionalMediaLoopIfRequired();
            }
            catch (Exception ex)
            {
                si.six(ex);
            }
            
            si.sil("UpdateDirectionalContent");
        }

        private void ConfigureGeneralSettings()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("GeneralBackgroundColour", "default")); SolidColorBrush BrushBackground = new SolidColorBrush(color);
                //mainWindow.Background = BrushBackground;
                m1.MeetingGrid.Background = BrushBackground;
                m2.MeetingGrid.Background = BrushBackground;
                m3.MeetingGrid.Background = BrushBackground;
                m4.MeetingGrid.Background = BrushBackground;
                d1.DirectionalGrid.Background = BrushBackground;
                d2.DirectionalGrid.Background = BrushBackground;
                d3.DirectionalGrid.Background = BrushBackground;
                d4.DirectionalGrid.Background = BrushBackground;

                string dalignment = GetTemplateValue("DirectionalAlignment", "default").ToLower();
                if (dalignment == "c")
                {
                    d1.GridWrapPanel.VerticalAlignment = VerticalAlignment.Center;
                    d2.GridWrapPanel.VerticalAlignment = VerticalAlignment.Center;
                    d3.GridWrapPanel.VerticalAlignment = VerticalAlignment.Center;
                    d4.GridWrapPanel.VerticalAlignment = VerticalAlignment.Center;
                }
                if (dalignment == "t")
                {
                    d1.GridWrapPanel.VerticalAlignment = VerticalAlignment.Top;
                    d2.GridWrapPanel.VerticalAlignment = VerticalAlignment.Top;
                    d3.GridWrapPanel.VerticalAlignment = VerticalAlignment.Top;
                    d4.GridWrapPanel.VerticalAlignment = VerticalAlignment.Top;
                }
                if (dalignment == "b")
                {
                    d1.GridWrapPanel.VerticalAlignment = VerticalAlignment.Bottom;
                    d2.GridWrapPanel.VerticalAlignment = VerticalAlignment.Bottom;
                    d3.GridWrapPanel.VerticalAlignment = VerticalAlignment.Bottom;
                    d4.GridWrapPanel.VerticalAlignment = VerticalAlignment.Bottom;
                }
                
            }
            catch (Exception ex)
            {
            }
        }

        private void PopulateCurrentDirectionalContent()
        {
            si.sie("PopulateCurrentDirectionalContent");
            
            try
            {

                //Only Refresh Eventrs and Template Collection Top of the Minute or On Startup
                DateTime dtCurrent = DateTime.Now;
                if (dtCurrent.Second <= 5 || firstDirectionalEventsCollection)
                {
                    firstDirectionalEventsCollection = false;
                    _events = _events = proxy.CollectCSEvents();

                    DateTime dtNow = DateTime.Now;
                    if (dtNow.AddMinutes(-5).Ticks > lastDirectionCollection.Ticks) //Only collect Templates once otherwise calls will go out to Core repeatedly for each screen
                    {
                        _directionCollection = proxy.CollectCSDirections();
                        lastDirectionCollection = DateTime.Now;
                    }
                    if (dtNow.AddMinutes(-5).Ticks > lastTemplateCollection.Ticks) //Only collect Templates once otherwise calls will go out to Core repeatedly for each screen
                    {
                        _templateCollection = proxy.CollectCSTemplates();
                        lastTemplateCollection = DateTime.Now;
                    }
                }

                d1.radWrap.Children.Clear();

                ConfigureGeneralSettings();

                ServiceReference1.CSEventCollection _currentevents = new ServiceReference1.CSEventCollection();

                //STEP 1. Find all current events
                long dtNowTicks = DateTime.Now.Ticks;
                //long dtLessTicks = DateTime.Now.AddMinutes(-15).Ticks;
                foreach (ServiceReference1.CSEvent _event in _events)
                {
                    si.sii("Checking:"+_event.Description + "/" + _event.Eventstart.ToString() + "/" + _event.Eventend);
                    DateTime dtst = (DateTime)_event.Eventstart;
                    dtst = dtst.AddMinutes(-15);
                    DateTime dtend;
                    try
                    {
                        dtend = (DateTime)_event.Eventend;
                    }
                    catch (Exception)
                    {
                        dtend = dtst.AddMinutes(-20);
                    }
                    
                    long est = dtst.Ticks; long een = dtend.Ticks;
                    if (dtNowTicks > est && dtNowTicks < een)
                    {
                        si.sii("Adding:" + _event.Description + "/" + _event.Eventstart.ToString() + "/" + _event.Eventend);
                        _currentevents.Add(_event);
                    }
                }

                _CurrentEventCount = _currentevents.Count;

                int stepCount = 0;
                foreach (ServiceReference1.CSEvent _event in _currentevents)
                {
                    stepCount++;
                    Boolean okToAdd = false;
                    if (DirectionalFlipEnabled)
                    {
                        if (DirectionalFlipOrFlop == 0)
                        { //FLIP
                            if (stepCount <= DirectionalFlipMax) okToAdd = true;
                        }
                        else
                        if (DirectionalFlipOrFlop == 1)
                        {
                            //FLOP
                            if (stepCount > DirectionalFlipMax) okToAdd = true;
                        }
                        if (DirectionalFlipOrFlop == 2)
                        {
                            //ADD ANYWAY TO PREVENT IDLE EVENT HAPPENING
                            if (stepCount > DirectionalFlipMax) okToAdd = true;
                        }
                    }
                    if (_CurrentEventCount <= DirectionalFlipMax)  //Add event but still allow spash screen (2)
                    {
                        okToAdd = true;
                        if (DirectionalFlipOrFlop == 0)
                        {
                            if (File.Exists(@"c:\content\conferenceContent\Images\splash.jpg"))
                            {
                                DirectionalFlipOrFlop = 2;
                            }
                            else
                            {
                                DirectionalFlipOrFlop = 0;
                            }
                        }
                        else
                        {
                            DirectionalFlipOrFlop = 0;
                        }
                    }

                    if (_CurrentEventCount > 6 && !DirectionalFlipEnabled) //should be >6
                    {
                        UserControlBasicDirectionalTemplate ucb = new UserControlBasicDirectionalTemplate();
                        ucb.tbTitle.Text = _event.Title;
                        ucb.labelInfo.Content = _event.Description;
                        ucb.labelTime.Content = _event.Eventstart.ToString();
                        ucb.labelVenue.Content = _event.Screenlocation;
                        SetDirectionalEventLogoNow(ucb, _event.Eventlogofile);
                        SetDirectionalArrowLogoNow(ucb, _event);
                        UpdateSmallDirectionalTemplate(ucb);
                        if (okToAdd || !DirectionalFlipEnabled) d1.radWrap.Children.Add(ucb);
                    }
                    else
                    if ((_CurrentEventCount > 3 && _CurrentEventCount <= 6 && !DirectionalFlipEnabled) || (_CurrentEventCount>3 && DirectionalFlipEnabled))
                    {
                        UserControlBasicDWDirectionalTemplate ucb = new UserControlBasicDWDirectionalTemplate();
                        ucb.tbTitle.Text = _event.Title;
                        ucb.labelInfo.Content = _event.Description;
                        ucb.labelTime.Content = _event.Eventstart.ToString();
                        ucb.labelVenue.Content = _event.Screenlocation;
                        SetDirectionalDWEventLogoNow(ucb, _event.Eventlogofile);
                        SetDirectionalDWArrowLogoNow(ucb, _event);
                        UpdateWideDirectionalTemplate(ucb);
                        if (okToAdd || !DirectionalFlipEnabled) d1.radWrap.Children.Add(ucb);
                    }
                    else
                    {
                        UserControlBasicVWDirectionalTemplate ucb = new UserControlBasicVWDirectionalTemplate();
                        ucb.tbTitle.Text = _event.Title;
                        ucb.labelInfo.Content = _event.Description;
                        ucb.labelTime.Content = _event.Eventstart.ToString();
                        ucb.labelVenue.Content = _event.Screenlocation;
                        SetDirectionalVWEventLogoNow(ucb, _event.Eventlogofile);
                        SetDirectionalVWArrowLogoNow(ucb, _event);
                        UpdateVWideDirectionalTemplate(ucb);
                        if (okToAdd || !DirectionalFlipEnabled) d1.radWrap.Children.Add(ucb);
                    }
                }
            }
            catch (Exception ex)
            {
                    si.six("Error during PopulateCurrentDirectionalContent Loop:",ex);
            }

            if (DirectionalFlipOrFlop == 2)
            { //SPLASH MODE
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Images\splash.jpg"));
                m1.ImageBackground.Source = bi;
                d1.ImageBackground.Source = bi;
                d1.radWrap.Visibility = Visibility.Collapsed;
            }
            else
            { //NORMAL DIRECTIONAL MODE
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Images\default.jpg"));
                m1.ImageBackground.Source = bi;
                d1.ImageBackground.Source = bi;
                d1.radWrap.Visibility = Visibility.Visible;
            }

            if (DirectionalFlipEnabled)
            {
                if (DirectionalFlipOrFlop == 0)
                { //FLIP
                    DirectionalFlipOrFlop = 1;
                }
                else
                    if (DirectionalFlipOrFlop == 1)
                    {
                        DirectionalFlipOrFlop = 2;
                    }
                    else
                        if (DirectionalFlipOrFlop == 2)
                        {
                            DirectionalFlipOrFlop = 0;
                        }
            }
            
            si.sil("PopulateCurrentDirectionalContent");
        }

        private void UpdateSmallDirectionalTemplate(UserControlBasicDirectionalTemplate ucb)
        {
            si.sie("UpdateSmallDirectionalTemplate");
            
            //GetTemplateValue("DateBandOpacity", "default")
            //Change Fonts
            try
            {
                FontFamilyConverter converter = new FontFamilyConverter();
                ucb.tbTitle.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("SDTitleFont", "default"));
                ucb.labelInfo.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("SDInfoFont", "default"));
                ucb.labelTime.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("SDDateFont", "default"));
                ucb.labelVenue.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("SDVenueFont", "default"));

                ucb.tbTitle.FontSize = Convert.ToInt16(GetTemplateValue("SDTitleFontSize", "default"));
                ucb.labelInfo.FontSize = Convert.ToInt16(GetTemplateValue("SDInfoFontSize", "default"));
                ucb.labelTime.FontSize = Convert.ToInt16(GetTemplateValue("SDDateFontSize", "default"));
                ucb.labelVenue.FontSize = Convert.ToInt16(GetTemplateValue("SDVenueFontSize", "default"));

                ucb.tbTitle.Opacity = Convert.ToDouble(GetTemplateValue("SDTitleOpacity", "default"));
                ucb.labelInfo.Opacity = Convert.ToDouble(GetTemplateValue("SDInfoOpacity", "default"));
                ucb.labelVenue.Opacity = Convert.ToDouble(GetTemplateValue("SDVenueOpacity", "default"));
                ucb.labelTime.Opacity = Convert.ToDouble(GetTemplateValue("SDDateOpacity", "default"));

                var color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("SDTitleFontCol", "default")); SolidColorBrush BrushBackground = new SolidColorBrush(color);
                ucb.tbTitle.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("SDInfoFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelInfo.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("SDDateFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelTime.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("SDVenueFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelVenue.Foreground = BrushBackground;

                ////ucb.bdrTemplate.Background;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("SDBGCol", "default"));
                BrushBackground = new SolidColorBrush(color);
                ucb.bdrTemplate.Background = BrushBackground; ucb.bdrTemplate.Background.Opacity = Convert.ToDouble(GetTemplateValue("SDBGOpacity", "default"));
                Thickness bt = new Thickness(Convert.ToDouble(GetTemplateValue("SDBorderThickness", "default")));
                ucb.bdrTemplate.BorderThickness = bt;

                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("SDBorderCol", "default"));
                BrushBackground = new SolidColorBrush(color);
                ucb.bdrTemplate.BorderBrush = BrushBackground;
            }
            catch (Exception ex)
            {
                si.six("Error during UpdateSmallDirectionalTemplate()", ex);
            }

            si.sil("UpdateSmallDirectionalTemplate");
        }

        private void UpdateWideDirectionalTemplate(UserControlBasicDWDirectionalTemplate ucb)
        {
            si.sie("UpdateWideDirectionalTemplate");
            
            //GetTemplateValue("DateBandOpacity", "default")
            //Change Fonts
            try
            {
                FontFamilyConverter converter = new FontFamilyConverter();
                ucb.tbTitle.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("LDTitleFont", "default"));
                ucb.labelInfo.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("LDInfoFont", "default"));
                ucb.labelTime.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("LDDateFont", "default"));
                ucb.labelVenue.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("LDVenueFont", "default"));

                ucb.tbTitle.FontSize = Convert.ToInt16(GetTemplateValue("LDTitleFontSize", "default"));
                ucb.labelInfo.FontSize = Convert.ToInt16(GetTemplateValue("LDInfoFontSize", "default"));
                ucb.labelTime.FontSize = Convert.ToInt16(GetTemplateValue("LDDateFontSize", "default"));
                ucb.labelVenue.FontSize = Convert.ToInt16(GetTemplateValue("LDVenueFontSize", "default"));

                ucb.tbTitle.Opacity = Convert.ToDouble(GetTemplateValue("LDTitleOpacity", "default"));
                ucb.labelInfo.Opacity = Convert.ToDouble(GetTemplateValue("LDInfoOpacity", "default"));
                ucb.labelVenue.Opacity = Convert.ToDouble(GetTemplateValue("LDVenueOpacity", "default"));
                ucb.labelTime.Opacity = Convert.ToDouble(GetTemplateValue("LDDateOpacity", "default"));

                var color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("LDTitleFontCol", "default")); SolidColorBrush BrushBackground = new SolidColorBrush(color);
                ucb.tbTitle.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("LDInfoFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelInfo.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("LDDateFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelTime.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("LDVenueFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelVenue.Foreground = BrushBackground;

                ////ucb.bdrTemplate.Background;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("LDBGCol", "default"));
                BrushBackground = new SolidColorBrush(color);
                ucb.bdrTemplate.Background = BrushBackground; ucb.bdrTemplate.Background.Opacity = Convert.ToDouble(GetTemplateValue("LDBGOpacity", "default"));
                Thickness bt = new Thickness(Convert.ToDouble(GetTemplateValue("LDBorderThickness", "default")));
                ucb.bdrTemplate.BorderThickness = bt;

                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("LDBorderCol", "default"));
                BrushBackground = new SolidColorBrush(color);
                ucb.bdrTemplate.BorderBrush = BrushBackground;
            }
            catch (Exception ex)
            {
                si.six("Error during UpdateWideDirectionalTemplate()", ex);
            }

            si.sil("UpdateWideDirectionalTemplate");
        }

        private void UpdateVWideDirectionalTemplate(UserControlBasicVWDirectionalTemplate ucb)
        {
            si.sie("UpdateVWideDirectionalTemplate");

            //GetTemplateValue("DateBandOpacity", "default")
            //Change Fonts
            try
            {
                FontFamilyConverter converter = new FontFamilyConverter();
                ucb.tbTitle.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("WDTitleFont", "default"));
                ucb.labelInfo.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("WDInfoFont", "default"));
                ucb.labelTime.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("WDDateFont", "default"));
                ucb.labelVenue.FontFamily = (FontFamily)converter.ConvertFrom(GetTemplateValue("WDVenueFont", "default"));

                ucb.tbTitle.FontSize = Convert.ToInt16(GetTemplateValue("WDTitleFontSize", "default"));
                ucb.labelInfo.FontSize = Convert.ToInt16(GetTemplateValue("WDInfoFontSize", "default"));
                ucb.labelTime.FontSize = Convert.ToInt16(GetTemplateValue("WDDateFontSize", "default"));
                ucb.labelVenue.FontSize = Convert.ToInt16(GetTemplateValue("WDVenueFontSize", "default"));

                ucb.tbTitle.Opacity = Convert.ToDouble(GetTemplateValue("WDTitleOpacity", "default"));
                ucb.labelInfo.Opacity = Convert.ToDouble(GetTemplateValue("WDInfoOpacity", "default"));
                ucb.labelVenue.Opacity = Convert.ToDouble(GetTemplateValue("WDVenueOpacity", "default"));
                ucb.labelTime.Opacity = Convert.ToDouble(GetTemplateValue("WDDateOpacity", "default"));

                var color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("WDTitleFontCol", "default")); SolidColorBrush BrushBackground = new SolidColorBrush(color);
                ucb.tbTitle.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("WDInfoFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelInfo.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("WDDateFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelTime.Foreground = BrushBackground;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("WDVenueFontCol", "default")); BrushBackground = new SolidColorBrush(color);
                ucb.labelVenue.Foreground = BrushBackground;

                ////ucb.bdrTemplate.Background;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("WDBGCol", "default"));
                BrushBackground = new SolidColorBrush(color);
                ucb.bdrTemplate.Background = BrushBackground; ucb.bdrTemplate.Background.Opacity = Convert.ToDouble(GetTemplateValue("WDBGOpacity", "default"));
                Thickness bt = new Thickness(Convert.ToDouble(GetTemplateValue("WDBorderThickness", "default")));
                ucb.bdrTemplate.BorderThickness = bt;
                color = (Color)ColorConverter.ConvertFromString(GetTemplateValue("WDBorderCol", "default"));
                BrushBackground = new SolidColorBrush(color);
                ucb.bdrTemplate.BorderBrush = BrushBackground;
            }
            catch (Exception ex)
            {
                si.six("Error during UpdateVWideDirectionalTemplate()", ex);
            }

            si.sil("UpdateVWideDirectionalTemplate");
        }

        private void SetDirectionalArrowLogoNow(UserControlBasicDirectionalTemplate ucb, ServiceReference1.CSEvent _event)
        {
            si.sie("SetDirectionalArrowLogoNow");
            
            try
            {
                Boolean founddir = false;
                ServiceReference1.CSDirection selectedDirection = new ServiceReference1.CSDirection();
                foreach (ServiceReference1.CSDirection csdir in _directionCollection)
                {
                    if (csdir.Directionalscreenname.ToLower()==Venue1Name.ToLower() &&
                        csdir.Meetingroomname.ToLower() == _event.Screenlocation.ToLower())
                    {
                        selectedDirection = csdir;
                        founddir = true;
                        break;
                    }
                }

                if (!founddir)
                {
                    ucb.imageDirection.Visibility = Visibility.Collapsed;
                }

                string fnonly = System.IO.Path.GetFileName(selectedDirection.Directionimagefile);
                if (!File.Exists(@"c:\content\ConferenceContent\Directional\" + fnonly))
                {
                    si.sii("Fetching arrow: " + fnonly);
                    CollectFile(fnonly, "arrow");
                }
                else
                {
                    si.sii("Arrow image already local " + fnonly);
                }

                si.sii("Setting arrow image: " + fnonly);
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Directional\" + fnonly));
                ucb.imageDirection.Source = bi;
                ucb.imageDirection.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                si.six("Error during SetDirectionalArrowLogoNow()", ex);
            }
            si.sil("SetDirectionalArrowLogoNow");
        }

        private void SetDirectionalDWArrowLogoNow(UserControlBasicDWDirectionalTemplate ucb, ServiceReference1.CSEvent _event)
        {
            si.sie("SetDirectionalDWArrowLogoNow");
            
            try
            {
                Boolean founddir = false;
                ServiceReference1.CSDirection selectedDirection = new ServiceReference1.CSDirection();
                foreach (ServiceReference1.CSDirection csdir in _directionCollection)
                {
                    if (csdir.Directionalscreenname.ToLower() == Venue1Name.ToLower() &&
                        csdir.Meetingroomname.ToLower() == _event.Screenlocation.ToLower())
                    {
                        selectedDirection = csdir;
                        founddir = true;
                        break;
                    }
                }

                if (!founddir)
                {
                    ucb.imageDirection.Visibility = Visibility.Collapsed;
                }

                string fnonly = System.IO.Path.GetFileName(selectedDirection.Directionimagefile);
                if (!File.Exists(@"c:\content\ConferenceContent\Directional\" + fnonly))
                {
                    si.sii("Fetching arrow: " + fnonly);
                    CollectFile(fnonly, "arrow");
                }
                else
                {
                    si.sii("Arrow image already local " + fnonly);
                }

                si.sii("Setting arrow image: " + fnonly);
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Directional\" + fnonly));
                ucb.imageDirection.Source = bi;
                ucb.imageDirection.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                si.six("Error during SetDirectionalDWArrowLogoNow()", ex);
            }
            si.sil("SetDirectionalDWArrowLogoNow");
        }

        private void SetDirectionalVWArrowLogoNow(UserControlBasicVWDirectionalTemplate ucb, ServiceReference1.CSEvent _event)
        {
            si.sie("SetDirectionalVWArrowLogoNow");

            try
            {
                Boolean founddir = false;
                ServiceReference1.CSDirection selectedDirection = new ServiceReference1.CSDirection();
                foreach (ServiceReference1.CSDirection csdir in _directionCollection)
                {
                    if (csdir.Directionalscreenname.ToLower() == Venue1Name.ToLower() &&
                        csdir.Meetingroomname.ToLower() == _event.Screenlocation.ToLower())
                    {
                        selectedDirection = csdir;
                        founddir = true;
                        break;
                    }
                }

                if (!founddir)
                {
                    ucb.imageDirection.Visibility = Visibility.Collapsed;
                }

                string fnonly = System.IO.Path.GetFileName(selectedDirection.Directionimagefile);
                if (!File.Exists(@"c:\content\ConferenceContent\Directional\" + fnonly))
                {
                    si.sii("Fetching arrow: " + fnonly);
                    CollectFile(fnonly, "arrow");
                }
                else
                {
                    si.sii("Arrow image already local " + fnonly);
                }

                si.sii("Setting arrow image: " + fnonly);
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Directional\" + fnonly));
                ucb.imageDirection.Source = bi;
                ucb.imageDirection.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                si.six("Error during SetDirectionalVWArrowLogoNow()", ex);
            }
            si.sil("SetDirectionalVWArrowLogoNow");
        }

        private void SetDirectionalEventLogoNow(UserControlBasicDirectionalTemplate ucb, string fn)
        {
            si.sie("SetDirectionalEventLogoNow");
            
            try
            {
                string fnonly = System.IO.Path.GetFileName(fn);
                if (!File.Exists(@"c:\content\ConferenceContent\Logos\" + fnonly))
                {
                    si.sii("About to fetch Logo file: " + fnonly);
                    CollectFile(fnonly, "logo");
                }
                else
                {
                    si.sii("Logo File already local: " + fnonly);
                }
                si.sii("About to set logo file: " + fnonly);
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Logos\" + fnonly));
                ucb.imageLogo.Source = bi;
            }
            catch (Exception ex)
            {
                si.six("Error during SetDirectionalEventLogoNow()", ex);
            }
            si.sil("SetDirectionalEventLogoNow");
        }

        private void SetDirectionalDWEventLogoNow(UserControlBasicDWDirectionalTemplate ucb, string fn)
        {
            si.sie("SetDirectionalDWEventLogoNow");
            
            try
            {
                string fnonly = System.IO.Path.GetFileName(fn);
                if (!File.Exists(@"c:\content\ConferenceContent\Logos\" + fnonly))
                {
                    si.sii("About to fetch Logo file: " + fnonly);
                    CollectFile(fnonly, "logo");
                }
                else
                {
                    si.sii("Logo File already local: " + fnonly);
                }
                si.sii("About to set logo file: " + fnonly);
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Logos\" + fnonly));
                ucb.imageLogo.Source = bi;
            }
            catch (Exception ex)
            {
                si.six("Error during SetDirectionalDWEventLogoNow()", ex);
            }
            si.sil("SetDirectionalDWEventLogoNow");
        }

        private void SetDirectionalVWEventLogoNow(UserControlBasicVWDirectionalTemplate ucb, string fn)
        {
            si.sie("SetDirectionalVWEventLogoNow");

            try
            {
                string fnonly = System.IO.Path.GetFileName(fn);
                if (!File.Exists(@"c:\content\ConferenceContent\Logos\" + fnonly))
                {
                    si.sii("About to fetch Logo file: " + fnonly);
                    CollectFile(fnonly, "logo");
                }
                else
                {
                    si.sii("Logo File already local: " + fnonly);
                }
                si.sii("About to set logo file: " + fnonly);
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Logos\" + fnonly));
                ucb.imageLogo.Source = bi;
            }
            catch (Exception ex)
            {
                si.six("Error during SetDirectionalVWEventLogoNow()", ex);
            }
            si.sil("SetDirectionalVWEventLogoNow");
        }

        private void UpdateVenueScreenTitle()
        {
            si.sie("UpdateVenueScreenTitle");

            try
            {
                foreach (ServiceReference1.CSScreen item in _screens)
                {
                    m1.LabelRoomName.Content = item.Screenlocation;
                    if (item.Isdirectional == 1 && item.Screennumber == "1")
                    {
                        _directional = true;
                    }
                    else
                    {
                        if (item.Screennumber == "1")
                        {
                            //DirectionalFlipEnabled = false;
                            _directional = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                si.six("Exception during UpdateVenueScreenTitle()", ex);
            }

            si.sil("UpdateVenueScreenTitle");
        }
        private void UpdateVenueTemplate(string template)
        {
            si.sie("UpdateVenueTemplate");

            DateTime dtNow = DateTime.Now;
            if (dtNow.AddSeconds(-300).Ticks > lastTemplateCollection.Ticks) //Only collect Templates once otherwise calls will go out to Core repeatedly for each screen
            {
                _templateCollection = proxy.CollectCSTemplates();
                lastTemplateCollection = dtNow;
            }
            
            ConfigureGeneralSettings();

            try
            {
                foreach (ServiceReference1.CSTemplate _temp in _templateCollection)
                {
                    si.sii(_temp.Template.ToLower()+":"+template.ToLower());
                    if (_temp.Template.ToLower() == template.ToLower())
                    {
                        si.sii("Applying Attribute:"+_temp.Name);
                        ApplyThisVenueTemplateAttributeNow(_temp);
                    }
                }
            }
            catch (Exception ex)
            {
                si.six("Error during UpdateVenueTemplate()", ex);
            }
            si.sil("UpdateVenueTemplate");
        }

        private void PopulateVenueWithIdleNow()
        {
            si.sie("PopulateVenueWithIdleNow");

            try
            {
                m1.LabelRoomEventTitle.Content = "Currently";
                m1.labelEventInfo.Content = "Available";
                m1.labelMeetingTime.Content = DateTime.Now.ToLongDateString();
                m1.ImageRoomEventLogo.Source = null;
                m1.bdrImageRoomEventLogo.Visibility = Visibility.Hidden;
                RollVenueMedia();
            }
            catch (Exception ex)
            {
                si.six("Error during PopulateVenueWithIdleNow()", ex);
            }

            si.sil("PopulateVenueWithIdleNow");
        }

        private void PopulateVenueCurrentContent()
        {
            si.sie("PopulateVenueCurrentContent");
            
            
            ServiceReference1.CSEventCollection _events = new ServiceReference1.CSEventCollection();
            try
            {
                Boolean found = false;
                long dtNowTicks = DateTime.Now.Ticks;
                long est, een = 0;
                DateTime dtst, dtend;
                _events = proxy.CollectCSEvents();
                foreach (ServiceReference1.CSEvent evnt in _events)
                {
                    if (evnt.Screenlocation.ToLower() == Venue1Name.ToLower())
                    {
                        dtst = (DateTime)evnt.Eventstart;
                        dtend = (DateTime)evnt.Eventend;
                        est = dtst.Ticks; een = dtend.Ticks;
                        if (dtNowTicks > est && dtNowTicks < een)
                        {
                            UpdateVenueTemplate(evnt.Template);
                            PopulateVenueWithThisEventNow(evnt);
                            found = true;
                        }
                    }
                }
                if (found == false)
                {
                    PopulateVenueWithIdleNow();
                }
            }
            catch (Exception ex)
            {
                si.six("Error during PopulateVenueCurrentContent()", ex);
            }
            si.sil("PopulateVenueCurrentContent");
        }

        private void ApplyThisVenueTemplateAttributeNow(ConferenceClient.ServiceReference1.CSTemplate _temp)
        {
            si.sie("ApplyThisVenueTemplateAttributeNow");
            
            try
            {
                if (_temp.Name == "TitleFont" || _temp.Name == "InfoFont" || _temp.Name == "VenueFont" || _temp.Name == "DateFont") 
                    ChangeFont(_temp.Name, _temp.Attribvaluetext);
                if (_temp.Name == "TitleFontSize" || _temp.Name == "InfoFontSize" || _temp.Name == "VenueFontSize" || _temp.Name == "DateFontSize")
                    ChangeFontSize(_temp.Name, _temp.Attribvaluetext);
                if (_temp.Name == "TitleFontCol" || _temp.Name == "InfoFontCol" || _temp.Name == "VenueFontCol" || _temp.Name == "DateFontCol")
                    ChangeFontCol(_temp.Name, _temp.Attribvaluetext);
                if (_temp.Name == "VenuePx" || _temp.Name == "DatePx" || _temp.Name == "TitlePx" || _temp.Name == "InfoPx")
                    ChangeVenuePx(_temp.Name, _temp.Attribvaluetext);
                if (_temp.Name == "LogoX") //Others are not required including logo capacity as all are queried
                    ChangeLogo(_temp.Name, _temp.Attribvaluetext);
                if (_temp.Name == "VenueBandCol" || _temp.Name == "TitleBandCol" || _temp.Name == "InfoBandCol" || _temp.Name == "DateBandCol")
                    ChangeBandColour(_temp.Name, _temp.Attribvaluetext);
                //if (_temp.Name == "VenueBandOpacity" || _temp.Name == "TitleBandOpacity" || _temp.Name == "InfoBandOpacity" || _temp.Name == "DateBandOpacity")
                //    ChangeBandOpacity(_temp.Name, _temp.Attribvaluetext);
            }
            catch (Exception ex)
            {
                si.six("Error during ApplyThisVenueTemplateAttributeNow()", ex);
            }
            si.sil("ApplyThisVenueTemplateAttributeNow");
        }

        private void ChangeFont(string n, string v)
        {
            si.sie("ChangeFont");
            
            try
            {
                si.sii("Changing font: "+n+":"+v);
                FontFamilyConverter converter = new FontFamilyConverter();
                if (n == "TitleFont") m1.LabelRoomEventTitle.FontFamily = (FontFamily)converter.ConvertFrom(v);
                if (n == "InfoFont") m1.labelEventInfo.FontFamily = (FontFamily)converter.ConvertFrom(v);
                if (n == "DateFont") m1.labelMeetingTime.FontFamily = (FontFamily)converter.ConvertFrom(v); m1.labelMeetingTime.Opacity = Convert.ToDouble(GetTemplateValue("DateTxtOpacity", "default"));
                if (n == "VenueFont") m1.LabelRoomName.FontFamily = (FontFamily)converter.ConvertFrom(v);
            }
            catch (Exception ex)
            {
                si.sie(ex.Message);
            }
            si.sil("ChangeFont");
        }

        private void ChangeFontSize(string n, string v)
        {
            si.sie("ChangeFontSize");
            
            try
            {
                si.sii("Changing font size: " + n + ":" + v);
                if (n == "TitleFontSize") m1.LabelRoomEventTitle.FontSize = Convert.ToInt16(v);
                if (n == "InfoFontSize") m1.labelEventInfo.FontSize = Convert.ToInt16(v);
                if (n == "DateFontSize") m1.labelMeetingTime.FontSize = Convert.ToInt16(v);
                if (n == "VenueFontSize") m1.LabelRoomName.FontSize = Convert.ToInt16(v);
            }
            catch (Exception ex)
            {
                si.sie(ex.Message);
            }
            si.sil("ChangeFontSize");
        }

        private void ChangeFontCol(string n, string v)
        {
            si.sie("ChangeFontCol");
            
            try
            {
                si.sii("Changing font col: " + n + ":" + v);
                var color = (Color)ColorConverter.ConvertFromString(v);
                SolidColorBrush BrushBackground = new SolidColorBrush(color);
                if (n == "TitleFontCol") m1.LabelRoomEventTitle.Foreground = BrushBackground;
                if (n == "InfoFontCol") m1.labelEventInfo.Foreground = BrushBackground;
                if (n == "DateFontCol") m1.labelMeetingTime.Foreground = BrushBackground;
                if (n == "VenueFontCol") m1.LabelRoomName.Foreground = BrushBackground;
            }
            catch (Exception ex)
            {
                si.sie(ex.Message);
            }
            si.sil("ChangeFontCol");
        }

        private void ChangeBandColour(string n, string v)
        {
            si.sie("ChangeBandColour");
            
            try
            {
                si.sii("Changing band col: " + n + ":" + v);
                var color = (Color)ColorConverter.ConvertFromString(v);
                SolidColorBrush BrushBackground = new SolidColorBrush(color);
                if (n == "VenueBandCol") m1.bdrRoomName.Background = BrushBackground; m1.bdrRoomName.Background.Opacity = Convert.ToDouble(GetTemplateValue("VenueBandOpacity","default"));
                if (n == "TitleBandCol") m1.bdrRoomEventTitle.Background = BrushBackground; m1.bdrRoomEventTitle.Background.Opacity = Convert.ToDouble(GetTemplateValue("TitleBandOpacity", "default"));
                if (n == "InfoBandCol") m1.bdrEventInfo.Background = BrushBackground; m1.bdrEventInfo.Background.Opacity = Convert.ToDouble(GetTemplateValue("InfoBandOpacity", "default"));
                if (n == "DateBandCol") m1.bdrMeetingTime.Background = BrushBackground; m1.bdrMeetingTime.Background.Opacity = Convert.ToDouble(GetTemplateValue("DateBandOpacity", "default"));
            }
            catch (Exception ex)
            {
                si.sie(ex.Message);
            }
            si.sil("ChangeBandColour");
        }

        private string GetTemplateValue(string attr, string templatename)
        {
            si.sie("GetTemplateValue");
            
            string rval = "0";
            try
            {
                foreach (ServiceReference1.CSTemplate _temp in _templateCollection)
                {
                    if (_temp.Template.ToLower() == templatename.ToLower() && _temp.Name.ToLower()==attr.ToLower())
                    {
                        rval = _temp.Attribvaluetext;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                si.six("Error during GetTemplateValue():"+attr, ex);
            }
            si.sil("GetTemplateValue");
            return rval;
        }

        private void ChangeBandOpacity(string n, string v)
        {
            si.sie("ChangeBandOpacity");
            
            try
            {
                si.sii("Changing background opacity: " + n + ":" + v);
                var color = (Color)ColorConverter.ConvertFromString(v);
                SolidColorBrush BrushBackground = new SolidColorBrush(color);
                if (n == "VenueBandOpacity") m1.bdrRoomName.Background.Opacity = Convert.ToDouble("0.1");
                if (n == "TitleBandOpacity") m1.bdrRoomEventTitle.Background.Opacity = Convert.ToDouble("0.1");
                if (n == "InfoBandOpacity") m1.bdrEventInfo.Background.Opacity = Convert.ToDouble(v);
                if (n == "DateBandOpacity") m1.bdrMeetingTime.Background.Opacity = Convert.ToDouble(v);
            }
            catch (Exception ex)
            {
                si.sie(ex.Message);
            }
            si.sil("ChangeBandOpacity");
        }

        private void ChangeVenuePx(string n, string v)
        {
            si.sie("ChangeVenuePx");
            
            try
            {
                si.sii("Changing venue px: " + n + ":" + v);

                m1.bdrRoomEventTitle.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                m1.bdrRoomName.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                m1.bdrMeetingTime.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                m1.bdrEventInfo.VerticalAlignment = System.Windows.VerticalAlignment.Top;

                if (n == "TitlePx") m1.bdrRoomEventTitle.Margin = new Thickness(0, Convert.ToInt16(v), 0, m1.bdrRoomEventTitle.ActualHeight);
                if (n == "InfoPx") m1.bdrEventInfo.Margin = new Thickness(0, Convert.ToInt16(v), 0, m1.bdrEventInfo.ActualHeight);
                if (n == "VenuePx") m1.bdrRoomName.Margin = new Thickness(0, Convert.ToInt16(v), 0, m1.bdrRoomName.ActualHeight);
                if (n == "DatePx") m1.bdrMeetingTime.Margin = new Thickness(0, Convert.ToInt16(v), 0, m1.bdrMeetingTime.ActualHeight);
            }
            catch (Exception ex)
            {
                si.sie(ex.Message);
            }
            si.sil("ChangeVenuePx");
        }

        private void ChangeLogo(string n, string v)
        {
            si.sie("ChangeLogo");
            
            try
            {
                si.sii("Changing logo px: " + n + ":" + v);

                m1.bdrImageRoomEventLogo.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                m1.bdrImageRoomEventLogo.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

                double x, y, h, w = 0;

                x = Convert.ToDouble(GetTemplateValue("LogoX","default"));
                y = Convert.ToDouble(GetTemplateValue("LogoY","default"));
                w = Convert.ToDouble(GetTemplateValue("LogoW","default"));
                h = Convert.ToDouble(GetTemplateValue("LogoH","default"));

                if (n == "LogoX") m1.bdrImageRoomEventLogo.Margin = new Thickness(x,y,0,0); m1.bdrImageRoomEventLogo.Width = w; m1.bdrImageRoomEventLogo.Height = h;
                m1.bdrImageRoomEventLogo.Opacity = Convert.ToDouble(GetTemplateValue("LogoOpacity", "default"));
            }
            catch (Exception ex)
            {
                si.sie(ex.Message);
            }
            si.sil("ChangeLogo");
        }

        private void PopulateVenueWithThisEventNow(ConferenceClient.ServiceReference1.CSEvent evnt)
        {
            si.sie("PopulateVenueWithThisEventNow");

            try
            {
                m1.LabelRoomEventTitle.Content = evnt.Title;
                m1.labelEventInfo.Content = evnt.Description;
                DateTime dtst = (DateTime)evnt.Eventstart;
                DateTime dtend = (DateTime)evnt.Eventend;
                m1.labelMeetingTime.Content = dtst.ToShortTimeString() + " - " + dtend.ToShortTimeString();
                if (evnt.Eventlogofile != null && evnt.Eventlogofile != "")
                {
                    if (SetEventLogoNow(evnt.Eventlogofile))
                    {
                        m1.bdrImageRoomEventLogo.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        m1.bdrImageRoomEventLogo.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    m1.bdrImageRoomEventLogo.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                si.six(ex);
            }

            si.sil("PopulateVenueWithThisEventNow");
        }

        private Boolean SetEventLogoNow(string logoFileName)
        {
            si.sie("SetEventLogoNow");
            
            Boolean functionResult = false;
            try
            {
                si.sii("Request to set logo:"+logoFileName);
                string fnonly = System.IO.Path.GetFileName(logoFileName);
                si.sii("Request to set logo (truncated):" + fnonly);
                if (!File.Exists(@"c:\content\ConferenceContent\Logos\" + fnonly))
                {
                    si.sii("About to Collect file Now...");
                    CollectFile(fnonly, "logo");
                    si.sii("File SHOULD be collected...");
                }
                else
                {
                    si.sii("File is local - no need to collectFile");
                }
                BitmapImage bi = new BitmapImage(new Uri(@"c:\content\conferenceContent\Logos\"+fnonly));
                m1.ImageRoomEventLogo.Source = bi;
                functionResult = true;
            }
            catch (Exception ex)
            {
                si.six("Error setting event logo", ex);
                functionResult = false;
            }
            return functionResult;
            si.sil("SetEventLogoNow");
        }

        private void CollectFile(string filenameonly, string fileType)
        {
            //si.sie("CollectFile()");
            //si.sii("File detail = "+filenameonly+" ("+fileType+")");
            //Check for empty filenameonly
            Boolean okToGoAheadAndCollectFile = false;
            try
            {
                filenameonly = System.IO.Path.GetFileName(filenameonly);
                if (filenameonly.Length >= 3) okToGoAheadAndCollectFile = true;
            }
            catch (Exception)
            {
            }

            if (okToGoAheadAndCollectFile == false) return;

            try
            {
                Gurock.SmartInspect.SiAuto.Main.LogMessage("Starting CollectFile - filename = " + filenameonly);
                Gurock.SmartInspect.SiAuto.Main.LogMessage("Filename only = " + filenameonly);
                Boolean success;
                _ftp = new Ftp2();
                success = _ftp.UnlockComponent("FTP212345678_29E8FB35jA2U");
                if (success != true)
                {
                    //MessageBox.Show(_ftp.LastErrorText);
                    return;
                }
                // _ftp.Hostname = Properties.Settings.Default.ServiceAddress;
                _ftp.Hostname = Properties.Settings.Default.ServiceAddress;
                _ftp.Username = FTP_USERNAME;
                _ftp.Password = FTP_PASSWORD;
                _ftp.Passive = true;

                Gurock.SmartInspect.SiAuto.Main.LogMessage("FTP IP = " + _ftp.Hostname);
                Gurock.SmartInspect.SiAuto.Main.LogMessage("FTP USERNAME = " + _ftp.Username);
                Gurock.SmartInspect.SiAuto.Main.LogMessage("FTP PWD = " + FTP_PASSWORD);

                // Connect and login to the FTP server.
                success = _ftp.Connect();
                if (success != true)
                {
                    //Wait and then try again
                    System.Threading.Thread.Sleep(250);
                    success = _ftp.Connect();
                    if (success != true)
                    {
                        //Andagain
                        System.Threading.Thread.Sleep(250);
                        success = _ftp.Connect();
                        if (success != true)
                        {
                            Gurock.SmartInspect.SiAuto.Main.LogMessage("FTP CONNECTION ERROR");
                        }
                    }
                }

                Gurock.SmartInspect.SiAuto.Main.LogMessage("FTP CONNECTED OK");

                string fileext = System.IO.Path.GetExtension(filenameonly);
                string subFolder = "";
                si.sii("FileType=" + fileType);
                #region Set Folders to Collect From
                if (fileType.ToLower() == "video")
                {
                    subFolder = "Conference/Video";
                }
                else
                    if (fileType.ToLower().ToLower() == "image")
                    {
                        subFolder = "Conference/Images";
                    }
                    else
                        if (fileType.ToLower().ToLower() == "flash")
                        {
                            subFolder = "Conference/Flash";
                        }
                        else
                            if (fileType.ToLower().ToLower() == "audio")
                            {
                                subFolder = "Conference/Audio";
                            }
                            else
                                if (fileType.ToLower().ToLower() == "arrow")
                                {
                                    subFolder = "Conference/Directional";
                                }
                                else
                                    if (fileType.ToLower().ToLower() == "logo")
                                    {
                                        subFolder = "Conference/Logos";
                                    };
                #endregion

                Gurock.SmartInspect.SiAuto.Main.LogMessage("FTP Subfolder from = " + subFolder);

                #region OlderSubFolder Settings
                //if (fileext.ToLower() == ".wmv" || fileext.ToLower() == ".avi" || fileext.ToLower() == ".mpg")
                //{
                //    subFolder = "Video";
                //}
                //else
                //    if (fileext.ToLower() == ".png" || fileext.ToLower() == ".jpg" || fileext.ToLower() == ".bmp")
                //    {
                //        subFolder = "Images";
                //    }
                //    else
                //        if (fileext.ToLower() == ".swf")
                //        {
                //            subFolder = "Flash";
                //        }
                //        else
                //            if (fileext.ToLower() == ".mp3")
                //            {
                //                subFolder = "Audio";
                //            }
                //            else
                //                if (fileext.ToLower() == ".txt")
                //                {
                //                    subFolder = "Ticker";
                //                };
                #endregion

                // Change to the remote directory where the file will be uploaded.
                success = _ftp.ChangeRemoteDir(subFolder);
                if (success != true)
                {
                    Gurock.SmartInspect.SiAuto.Main.LogMessage("Error changing to subfolder:" + subFolder);
                    success = _ftp.ChangeRemoteDir(subFolder);
                    if (success != true)
                    {
                    }
                }

                string localFilename;
                string savefolder = "";
                fileType = fileType.ToLower();
                switch (fileType)
                {
                    case "logo": savefolder="Logos"; break;
                    case "arrow": savefolder = "Directional"; break;
                    case "image": savefolder = "Images"; break;
                    case "video": savefolder = "Video"; break;
                }
                localFilename = @"c:\content\ConferenceContent\"+savefolder+@"\"+filenameonly;

                string remoteFilename;
                remoteFilename = filenameonly;
                
                Gurock.SmartInspect.SiAuto.Main.LogMessage("Saving To:" + localFilename);
                Gurock.SmartInspect.SiAuto.Main.LogMessage("Saving From:" + remoteFilename);

                success = _ftp.GetFile(remoteFilename, localFilename);

                if (success != true)
                {
                    //Try again
                    success = _ftp.GetFile(remoteFilename, localFilename);
                    if (success != true)
                    {
                        return;
                    }
                }

                _ftp.Disconnect();

            }
            catch (Exception ex)
            {
                Gurock.SmartInspect.SiAuto.Main.LogMessage("Collecting media error:" + ex.Message);
            }
            //si.sil("CollectFile");
        }

        private void AdjustProxySettings()
        {
            si.sie("AdjustProxySettings");
            
            try
            {
                CORESERVICE_IP_ADDRESS = Properties.Settings.Default.ServiceAddress;
                CORESERVICE_PORT = Properties.Settings.Default.ServicePort;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Increase binding max sizes so that the image can be retrieved  
            proxy = new ServiceReference1.CoreServiceClient(new BasicHttpBinding(), new EndpointAddress("http://" + CORESERVICE_IP_ADDRESS + ":" + CORESERVICE_PORT + "/iTactixCoreService"));

            // Increase binding max sizes so that the image can be retrieved  
            if (proxy.Endpoint.Binding is System.ServiceModel.BasicHttpBinding)
            {
                System.ServiceModel.BasicHttpBinding binding = (System.ServiceModel.BasicHttpBinding)proxy.Endpoint.Binding;
                int max = 5000000;  // around 5M  
                binding.MaxReceivedMessageSize = max;
                binding.MaxBufferSize = max;
                binding.ReaderQuotas.MaxArrayLength = max;
            }
            // si.sil("AdjustProxySettings");
            si.sil("AdjustProxySettings");
        }

        private void TestService()
        {
            si.sie("Check Service and Configuration - TestService()");
            listboxStartup.Items.Clear();
            LogStartup("iTactix Conference Client Diagnostic Check");
            LogStartup("  ");
            borderConfiguration.Visibility = Visibility.Collapsed;
            AdjustProxySettings();
            LogStartup("- Checking Service at http://"+CORESERVICE_IP_ADDRESS+":"+CORESERVICE_PORT);
            Boolean allok = true;
            try
            {
                ServiceReference1.UserCollection _users = new ServiceReference1.UserCollection();
                _users = proxy.CollectUsers();
                if (_users.Count <= 0)
                {
                    MessageBox.Show(_users.Count.ToString());
                    allok = false;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                allok = false;
            }
            if (allok == false)
            {
                si.sii("allok = false");
                si.sii("collapsed");
                LogStartup("- Service Check Failed - Please check your configuration");
                tbPort.Text = Properties.Settings.Default.ServicePort;
                tbServiceAddress.Text = Properties.Settings.Default.ServiceAddress;
                tbClientID.Text = Properties.Settings.Default.PCName;
                borderConfiguration.Visibility = Visibility.Visible;
                tbServiceAddress.Focus();
                si.sii("done");
            }
            else
            {
                LogStartup("- Service OK....");
                LogStartup("- Checking Display Registration");
                if (CheckDisplayRegistration() == false)
                {
                    LogStartup("- There is no Computer Registered with this name");
                    LogStartup("- PC Name Check Failed, Please check your configuration");
                    tbPort.Text = Properties.Settings.Default.ServicePort;
                    tbServiceAddress.Text = Properties.Settings.Default.ServiceAddress;
                    tbClientID.Text = Properties.Settings.Default.PCName;
                    borderConfiguration.Visibility = Visibility.Visible;
                }
                else
                {
                    LogStartup("- Computer Registered OK");
                    LogStartup("- All OK | Starting Display Client");
                    System.Threading.Thread.Sleep(500);
                    borderConfiguration.Visibility = Visibility.Hidden;
                    listboxStartup.Visibility = Visibility.Hidden;
                    StartDisplayClientServices();
                }
            }
            si.sil("Check Service and Configuration - TestService()");
        }

        private Boolean CheckDisplayRegistration()
        {
            si.sie("CheckDisplayRegistration");
            
            DateTime dtNow = DateTime.Now;
            if (dtNow.AddMinutes(-5).Ticks > lastScreenCollection.Ticks) //Only collect Templates once otherwise calls will go out to Core repeatedly for each screen
            {
                _screens = new ServiceReference1.CSScreenCollection();
                _screens = proxy.CollectCSScreens();
                lastScreenCollection = dtNow;
            }
            Boolean found = false;
            try
            {
                ServiceReference1.CSScreenCollection _screensListed = new ServiceReference1.CSScreenCollection();
                foreach (ServiceReference1.CSScreen item in _screens)
                {
                    _screensListed.Add(item);
                }
                //_screensListed = proxy.CollectCSScreens();
                foreach (ServiceReference1.CSScreen item in _screensListed)
                {
                    if (item.Computername.ToLower() == Properties.Settings.Default.PCName.ToLower() && item.Screennumber == "1")
                    {
                        _screens.Add(item);
                        Venue1Name = item.Screenlocation;
                        if (item.Isdirectional == 1)
                        {
                            //Venue1Name = "directional";
                            _directional = true;
                        }
                        
                        found = true;
                    }
                }
                si.sil("CheckDisplayRegistration");
                return found;

            }
            catch (Exception)
            {
                si.sil("CheckDisplayRegistration");
                return found;
            }
        }

        private void LogStartup(string s)
        {
            listboxStartup.Items.Add(s);
        }

        private void btnChangeTitleForTest_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m2.LabelRoomName.Content = "Changed!";
            //m2.LabelRoomName.Height = 200;
            m2.bdrRoomName.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            m2.bdrRoomName.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            m2.bdrRoomName.Margin = new Thickness(50,50,300,0);
            m2.bdrRoomName.Height = 200;
            //test.Margin = new Thickness(0, -5, 0, 0);

        }

        private void btnSaveConfiguration_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            spContent.Children.Clear();
            Properties.Settings.Default.PCName = tbClientID.Text;
            Properties.Settings.Default.ServiceAddress = tbServiceAddress.Text;
            Properties.Settings.Default.ServicePort = tbPort.Text;
            Properties.Settings.Default.Save();
            TestService();
        }

        private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key == Key.F8)
            {
                TestService();
                //borderConfiguration.Visibility = Visibility.Visible;
            }
        }
    }
}
