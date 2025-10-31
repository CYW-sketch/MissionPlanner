#if !LIB
extern alias Drawing;
#endif

using GMap.NET.WindowsForms;
using log4net;
using MissionPlanner.ArduPilot;
using MissionPlanner.Comms;
using MissionPlanner.Controls;
using MissionPlanner.GCSViews.ConfigurationView;
using MissionPlanner.Log;
using MissionPlanner.Maps;
using MissionPlanner.Utilities;

using MissionPlanner.Warnings;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MissionPlanner.ArduPilot.Mavlink;
using MissionPlanner.Utilities.HW;
using Transitions;
using System.Linq;
using MissionPlanner.Joystick;
using System.Net;
using Newtonsoft.Json;
using MissionPlanner;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Flurl.Util;
using Org.BouncyCastle.Bcpg;
using log4net.Repository.Hierarchy;
using System.Numerics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using static MAVLink;
using DroneCAN;

namespace MissionPlanner
{
    public partial class MainV2 : Form
    {
        private static readonly ILog log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static menuicons displayicons; //do not initialize to allow update of custom icons
        public static string running_directory = Settings.GetRunningDirectory();

        public abstract class menuicons
        {
            public abstract Image fd { get; }
            public abstract Image fp { get; }
            public abstract Image initsetup { get; }
            public abstract Image config_tuning { get; }
            public abstract Image sim { get; }
            public abstract Image terminal { get; }
            public abstract Image help { get; }
            public abstract Image donate { get; }
            public abstract Image connect { get; }
            public abstract Image disconnect { get; }
            public abstract Image bg { get; }
            public abstract Image wizard { get; }
        }


        public class burntkermitmenuicons : menuicons
        {
            public override Image fd
            {
                get
                {
                    if (File.Exists($"{running_directory}light_flightdata_icon.png"))
                        return Image.FromFile($"{running_directory}light_flightdata_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_flightdata_icon;
                }
            }

            public override Image fp
            {
                get
                {
                    if (File.Exists($"{running_directory}light_flightplan_icon.png"))
                        return Image.FromFile($"{running_directory}light_flightplan_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_flightplan_icon;
                }
            }

            public override Image initsetup
            {
                get
                {
                    if (File.Exists($"{running_directory}light_initialsetup_icon.png"))
                        return Image.FromFile($"{running_directory}light_initialsetup_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_initialsetup_icon;
                }
            }

            public override Image config_tuning
            {
                get
                {
                    if (File.Exists($"{running_directory}light_tuningconfig_icon.png"))
                        return Image.FromFile($"{running_directory}light_tuningconfig_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_tuningconfig_icon;
                }
            }

            public override Image sim
            {
                get
                {
                    if (File.Exists($"{running_directory}light_simulation_icon.png"))
                        return Image.FromFile($"{running_directory}light_simulation_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_simulation_icon;
                }
            }

            public override Image terminal
            {
                get
                {
                    if (File.Exists($"{running_directory}light_terminal_icon.png"))
                        return Image.FromFile($"{running_directory}light_terminal_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_terminal_icon;
                }
            }

            public override Image help
            {
                get
                {
                    if (File.Exists($"{running_directory}light_help_icon.png"))
                        return Image.FromFile($"{running_directory}light_help_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_help_icon;
                }
            }

            public override Image donate
            {
                get
                {
                    if (File.Exists($"{running_directory}light_donate_icon.png"))
                        return Image.FromFile($"{running_directory}light_donate_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.donate;
                }
            }

            public override Image connect
            {
                get
                {
                    if (File.Exists($"{running_directory}light_connect_icon.png"))
                        return Image.FromFile($"{running_directory}light_connect_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_connect_icon;
                }
            }

            public override Image disconnect
            {
                get
                {
                    if (File.Exists($"{running_directory}light_disconnect_icon.png"))
                        return Image.FromFile($"{running_directory}light_disconnect_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.light_disconnect_icon;
                }
            }

            public override Image bg
            {
                get
                {
                    if (File.Exists($"{running_directory}light_icon_background.png"))
                        return Image.FromFile($"{running_directory}light_icon_background.png");
                    else
                        return global::MissionPlanner.Properties.Resources.bgdark;
                }
            }

            public override Image wizard
            {
                get
                {
                    if (File.Exists($"{running_directory}light_wizard_icon.png"))
                        return Image.FromFile($"{running_directory}light_wizard_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.wizardicon;
                }
            }
        }

        public class highcontrastmenuicons : menuicons
        {
            private string running_directory = Settings.GetRunningDirectory();

            public override Image fd
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_flightdata_icon.png"))
                        return Image.FromFile($"{running_directory}dark_flightdata_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_flightdata_icon;
                }
            }

            public override Image fp
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_flightplan_icon.png"))
                        return Image.FromFile($"{running_directory}dark_flightplan_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_flightplan_icon;
                }
            }

            public override Image initsetup
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_initialsetup_icon.png"))
                        return Image.FromFile($"{running_directory}dark_initialsetup_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_initialsetup_icon;
                }
            }

            public override Image config_tuning
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_tuningconfig_icon.png"))
                        return Image.FromFile($"{running_directory}dark_tuningconfig_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_tuningconfig_icon;
                }
            }

            public override Image sim
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_simulation_icon.png"))
                        return Image.FromFile($"{running_directory}dark_simulation_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_simulation_icon;
                }
            }

            public override Image terminal
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_terminal_icon.png"))
                        return Image.FromFile($"{running_directory}dark_terminal_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_terminal_icon;
                }
            }

            public override Image help
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_help_icon.png"))
                        return Image.FromFile($"{running_directory}dark_help_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_help_icon;
                }
            }

            public override Image donate
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_donate_icon.png"))
                        return Image.FromFile($"{running_directory}dark_donate_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.donate;
                }
            }

            public override Image connect
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_connect_icon.png"))
                        return Image.FromFile($"{running_directory}dark_connect_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_connect_icon;
                }
            }

            public override Image disconnect
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_disconnect_icon.png"))
                        return Image.FromFile($"{running_directory}dark_disconnect_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.dark_disconnect_icon;
                }
            }

            public override Image bg
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_icon_background.png"))
                        return Image.FromFile($"{running_directory}dark_icon_background.png");
                    else
                        return null;
                }
            }

            public override Image wizard
            {
                get
                {
                    if (File.Exists($"{running_directory}dark_wizard_icon.png"))
                        return Image.FromFile($"{running_directory}dark_wizard_icon.png");
                    else
                        return global::MissionPlanner.Properties.Resources.wizardicon;
                }
            }
        }

        Controls.MainSwitcher MyView;

        private static DisplayView _displayConfiguration = File.Exists(DisplayViewExtensions.custompath)
            ? new DisplayView().Custom()
            : new DisplayView().Advanced();

        public static event EventHandler LayoutChanged;

        public static DisplayView DisplayConfiguration
        {
            get { return _displayConfiguration; }
            set
            {
                _displayConfiguration = value;
                Settings.Instance["displayview"] = _displayConfiguration.ConvertToString();
                LayoutChanged?.Invoke(null, EventArgs.Empty);
            }
        }


        public static bool ShowAirports { get; set; }
        public static bool ShowTFR { get; set; }

        private Utilities.adsb _adsb;

        public bool EnableADSB
        {
            get { return _adsb != null; }
            set
            {
                if (value == true)
                {
                    _adsb = new Utilities.adsb();

                    if (Settings.Instance["adsbserver"] != null)
                        Utilities.adsb.server = Settings.Instance["adsbserver"];
                    if (Settings.Instance["adsbport"] != null)
                        Utilities.adsb.serverport = int.Parse(Settings.Instance["adsbport"].ToString());
                }
                else
                {
                    Utilities.adsb.Stop();
                    _adsb = null;
                }
            }
        }

        //public static event EventHandler LayoutChanged;

        /// <summary>
        /// Active Comport interface
        /// </summary>
        public static MAVLinkInterface comPort
        {
            get { return _comPort; }
            set
            {
                if (_comPort == value)
                    return;
                _comPort = value;
                if (instance == null)
                    return;
                _comPort.MavChanged -= instance.comPort_MavChanged;
                _comPort.MavChanged += instance.comPort_MavChanged;
                instance.comPort_MavChanged(null, null);
            }
        }

        static MAVLinkInterface _comPort = new MAVLinkInterface();

        /// <summary>
        /// passive comports
        /// </summary>
        public static List<MAVLinkInterface> Comports = new List<MAVLinkInterface>();

        public delegate void WMDeviceChangeEventHandler(WM_DEVICECHANGE_enum cause);

        public event WMDeviceChangeEventHandler DeviceChanged;

        /// <summary>
        /// other planes in the area from adsb
        /// </summary>
        public object adsblock = new object();

        public ConcurrentDictionary<string, adsb.PointLatLngAltHdg> adsbPlanes =
            new ConcurrentDictionary<string, adsb.PointLatLngAltHdg>();

        public static string titlebar;

        /// <summary>
        /// Comport name
        /// </summary>
        public static string comPortName = "";

        public static int comPortBaud = 57600;

        /// <summary>
        /// 自动连接管理
        /// </summary>
        public static AutoConnectManager AutoConnectManager = new AutoConnectManager();

        /// <summary>
        /// 上次手动连接的地址和端口（用于手动重连）
        /// </summary>
        private static string _lastManualHost = "";
        private static string _lastManualPort = "";

        /// <summary>
        /// mono detection
        /// </summary>
        public static bool MONO = false;

        public bool UseCachedParams { get; set; } = false;
        public static bool Android { get; set; }
        public static bool IOS { get; set; }
        public static bool OSX { get; set; }


        /// <summary>
        /// speech engine enable
        /// </summary>
        public static bool speechEnable
        {
            get { return speechEngine == null ? false : speechEngine.speechEnable; }
            set
            {
                if (speechEngine != null) speechEngine.speechEnable = value;
            }
        }

        public static bool speech_armed_only = false;
        public static bool speechEnabled()
        {
            if (speechEngine == null)
                return false;

            if (!speechEnable) {
                return false;
            }
            if (speech_armed_only) {
                return MainV2.comPort.MAV.cs.armed;
            }
            return true;
        }

        /// <summary>
        /// spech engine static class
        /// </summary>
        public static ISpeech speechEngine { get; set; }

        /// <summary>
        /// joystick static class
        /// </summary>
        public static Joystick.JoystickBase joystick { get; set; }

        /// <summary>
        /// track last joystick packet sent. used to control rate
        /// </summary>
        DateTime lastjoystick = DateTime.Now;

        /// <summary>
        /// determine if we are running sitl
        /// </summary>
        public static bool sitl
        {
            get
            {
                if (MissionPlanner.GCSViews.SITL.SITLSEND == null) return false;
                if (MissionPlanner.GCSViews.SITL.SITLSEND.Client.Connected) return true;
                return false;
            }
        }

        /// <summary>
        /// hud background image grabber from a video stream - not realy that efficent. ie no hardware overlays etc.
        /// </summary>
        public static WebCamService.Capture cam { get; set; }
        /// <summary>
        /// used for custom autoconnect for predefined endpoints
        /// </summary>
        public List<AutoConnect.ConnectionInfo> ExtraConnectionList { get; } = new List<AutoConnect.ConnectionInfo>();
        /// <summary>
        /// used for dynamic custom port types
        /// </summary>
        public Dictionary<Regex, Func<string, string, ICommsSerial>> CustomPortList { get; } = new Dictionary<Regex, Func<string, string, ICommsSerial>>();

        /// <summary>
        /// controls the main serial reader thread
        /// </summary>
        bool serialThread = false;

        bool pluginthreadrun = false;

        bool joystickthreadrun = false;

        bool adsbThread = false;

        Thread httpthread;
        Thread pluginthread;

        /// <summary>
        /// track the last heartbeat sent
        /// </summary>
        private DateTime heatbeatSend = DateTime.UtcNow;

        /// <summary>
        /// track the last ads-b send time
        /// </summary>
        private DateTime adsbSend = DateTime.Now;
        /// <summary>
        /// track the adsb plane index we're round-robin sending
        /// starts at -1 because it'll get incremented before sending
        /// </summary>
        private int adsbIndex = -1;

        /// <summary>
        /// used to call anything as needed.
        /// </summary>
        public static MainV2 instance = null;

        public static bool isHerelink = false;

        public static MainSwitcher View;

        /// <summary>
        /// store the time we first connect UTC
        /// </summary>
        DateTime connecttime = DateTime.UtcNow;
        /// <summary>
        /// no data repeat interval UTC
        /// </summary>
        DateTime nodatawarning = DateTime.UtcNow;

        /// <summary>
        /// update the connect button UTC
        /// </summary>
        DateTime connectButtonUpdate = DateTime.UtcNow;

        /// <summary>
        /// declared here if i want a "single" instance of the form
        /// ie configuration gets reloaded on every click
        /// </summary>
        public GCSViews.FlightData FlightData;

        public GCSViews.FlightPlanner FlightPlanner;
        GCSViews.SITL Simulation;

        private Form connectionStatsForm;
        private ConnectionStats _connectionStats;

        /// <summary>
        /// This 'Control' is the toolstrip control that holds the comport combo, baudrate combo etc
        /// Otiginally seperate controls, each hosted in a toolstip sqaure, combined into this custom
        /// control for layout reasons.
        /// </summary>
        public static ConnectionControl _connectionControl;

        public static bool TerminalTheming = true;

        public void updateLayout(object sender, EventArgs e)
        {
            MenuSimulation.Visible = DisplayConfiguration.displaySimulation;
            MenuHelp.Visible = DisplayConfiguration.displayHelp;
            MissionPlanner.Controls.BackstageView.BackstageView.Advanced = DisplayConfiguration.isAdvancedMode;

            // force autohide on
            if (DisplayConfiguration.autoHideMenuForce)
            {
                AutoHideMenu(true);
                Settings.Instance["menu_autohide"] = true.ToString();
                autoHideToolStripMenuItem.Visible = false;
            }
            else if (Settings.Instance.GetBoolean("menu_autohide"))
            {
                AutoHideMenu(Settings.Instance.GetBoolean("menu_autohide"));
                Settings.Instance["menu_autohide"] = Settings.Instance.GetBoolean("menu_autohide").ToString();
            }



            //Flight data page
            if (MainV2.instance.FlightData != null)
            {
                //hide menu items
                MainV2.instance.FlightData.updateDisplayView();
            }

            if (MainV2.instance.FlightPlanner != null)
            {
                //hide menu items
                MainV2.instance.FlightPlanner.updateDisplayView();
            }
        }


        public MainV2()
        {
            log.Info("Mainv2 ctor");

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            // create one here - but override on load
            Settings.Instance["guid"] = Guid.NewGuid().ToString();

            //Check for -config argument, and if it is an xml extension filename then use that for config
            if (Program.args.Length > 0 && Program.args.Contains("-config"))
            {
                var cmds = ProcessCommandLine(Program
                    .args); //This will be called later as well, but we need it here and now
                if (cmds.ContainsKey("config") &&
                    (cmds["config"] != null) &&
                    (String.Compare(Path.GetExtension(cmds["config"]), ".xml", true) == 0))
                {
                    Settings.FileName = cmds["config"];
                }
            }

            // load config
            LoadConfig();

            // 加载自动连接配置
            LoadAutoConnectConfig();

            // force language to be loaded
            L10N.GetConfigLang();

            ShowAirports = true;

            // setup adsb
            Utilities.adsb.ApplicationVersion = System.Windows.Forms.Application.ProductVersion;
            Utilities.adsb.UpdatePlanePosition += adsb_UpdatePlanePosition;

            MAVLinkInterface.UpdateADSBPlanePosition += adsb_UpdatePlanePosition;

            MAVLinkInterface.UpdateADSBCollision += (sender, tuple) =>
            {
                lock (adsblock)
                {
                    if (MainV2.instance.adsbPlanes.ContainsKey(tuple.id))
                    {
                        // update existing
                        ((adsb.PointLatLngAltHdg) instance.adsbPlanes[tuple.id]).ThreatLevel = tuple.threat_level;
                    }
                }
            };

            MAVLinkInterface.gcssysid = (byte) Settings.Instance.GetByte("gcsid", MAVLinkInterface.gcssysid);

            Form splash = Program.Splash;

            splash?.Refresh();

            Application.DoEvents();

            instance = this;

            MyView = new MainSwitcher(this);

            View = MyView;

            if (Settings.Instance.ContainsKey("language") && !string.IsNullOrEmpty(Settings.Instance["language"]))
            {
                changelanguage(CultureInfoEx.GetCultureInfo(Settings.Instance["language"]));
            }

            InitializeComponent();

            //Init Theme table and load BurntKermit as a default
            ThemeManager.thmColor = new ThemeColorTable(); //Init colortable
            ThemeManager.thmColor.InitColors(); //This fills up the table with BurntKermit defaults.
            ThemeManager.thmColor
                .SetTheme(); //Set the colors, this need to handle the case when not all colors are defined in the theme file



            if (Settings.Instance["theme"] == null)
            {
                if (File.Exists($"{running_directory}custom.mpsystheme"))
                    Settings.Instance["theme"] = "custom.mpsystheme";
                else
                    Settings.Instance["theme"] = "BurntKermit.mpsystheme";
            }

            ThemeManager.LoadTheme(Settings.Instance["theme"]);

            Utilities.ThemeManager.ApplyThemeTo(this);


            // define default basestream
            comPort.BaseStream = new SerialPort();
            comPort.BaseStream.BaudRate = 57600;
            ((SerialPort)comPort.BaseStream).espFix = Settings.Instance.GetBoolean("CHK_rtsresetesp32", false);

            _connectionControl = toolStripConnectionControl.ConnectionControl;
            _connectionControl.CMB_baudrate.TextChanged += this.CMB_baudrate_TextChanged;
            _connectionControl.CMB_serialport.SelectedIndexChanged += this.CMB_serialport_SelectedIndexChanged;
            _connectionControl.CMB_serialport.Click += this.CMB_serialport_Click;
            _connectionControl.cmb_sysid.Click += cmb_sysid_Click;

            _connectionControl.ShowLinkStats += (sender, e) => ShowConnectionStatsForm();
            srtm.datadirectory = $"{Settings.GetDataDirectory()}srtm";

            var t = Type.GetType("Mono.Runtime");
            MONO = (t != null);

            try
            {
                if (speechEngine == null)
                    speechEngine = new Speech();
                MAVLinkInterface.Speech = speechEngine;
                CurrentState.Speech = speechEngine;
            }
            catch
            {
            }

            // proxy loader - dll load now instead of on config form load
            new Transition(new TransitionType_EaseInEaseOut(2000));

            PopulateSerialportList();
            if (_connectionControl.CMB_serialport.Items.Count > 0)
            {
                _connectionControl.CMB_baudrate.SelectedIndex = 8;
                _connectionControl.CMB_serialport.SelectedIndex = 0;
            }
            // ** Done

            splash?.Refresh();
            Application.DoEvents();

            // load last saved connection settings
            string temp = Settings.Instance.ComPort;
            if (!string.IsNullOrEmpty(temp))
            {
                _connectionControl.CMB_serialport.SelectedIndex = _connectionControl.CMB_serialport.FindString(temp);
                if (_connectionControl.CMB_serialport.SelectedIndex == -1)
                {
                    _connectionControl.CMB_serialport.Text = temp; // allows ports that dont exist - yet
                }

                comPort.BaseStream.PortName = temp;
                comPortName = temp;
            }

            string temp2 = Settings.Instance.BaudRate;
            if (!string.IsNullOrEmpty(temp2))
            {
                var idx = _connectionControl.CMB_baudrate.FindString(temp2);
                if (idx == -1)
                {
                    _connectionControl.CMB_baudrate.Text = temp2;
                }
                else
                {
                    _connectionControl.CMB_baudrate.SelectedIndex = idx;
                }

                comPortBaud = int.Parse(temp2);
            }

            MissionPlanner.Utilities.Tracking.cid = new Guid(Settings.Instance["guid"].ToString());

            if (splash != null)
            {
                this.Text = splash?.Text;
                titlebar = splash?.Text;
            }

            if (!MONO) // windows only
            {
                if (Settings.Instance["showconsole"] != null && Settings.Instance["showconsole"].ToString() == "True")
                {
                }
                else
                {
                    NativeMethods.ShowWindow(NativeMethods.GetConsoleWindow(), NativeMethods.SW_HIDE);

                }

                // prevent system from sleeping while mp open
                var previousExecutionState =
                    NativeMethods.SetThreadExecutionState(
                        NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
            }

            ChangeUnits();

            if (Settings.Instance["showairports"] != null)
            {
                MainV2.ShowAirports = bool.Parse(Settings.Instance["showairports"]);
            }

            // set default
            ShowTFR = true;
            // load saved
            if (Settings.Instance["showtfr"] != null)
            {
                MainV2.ShowTFR = Settings.Instance.GetBoolean("showtfr", ShowTFR);
            }

            if (Settings.Instance["enableadsb"] != null)
            {
                MainV2.instance.EnableADSB = Settings.Instance.GetBoolean("enableadsb");
            }

            try
            {
                log.Debug(Process.GetCurrentProcess().Modules.ToJSON());
            }
            catch
            {
            }

            try
            {
                log.Info("Create FD");
                FlightData = new GCSViews.FlightData();
                log.Info("Create FP");
                FlightPlanner = new GCSViews.FlightPlanner();
                //Configuration = new GCSViews.ConfigurationView.Setup();
                log.Info("Create SIM");
                Simulation = new GCSViews.SITL();
                //Firmware = new GCSViews.Firmware();
                //Terminal = new GCSViews.Terminal();

                FlightData.Width = MyView.Width;
                FlightPlanner.Width = MyView.Width;
                Simulation.Width = MyView.Width;
            }
            catch (ArgumentException e)
            {
                //http://www.microsoft.com/en-us/download/details.aspx?id=16083
                //System.ArgumentException: Font 'Arial' does not support style 'Regular'.

                log.Fatal(e);
                CustomMessageBox.Show($"{e}\n\n Font Issues? Please install this http://www.microsoft.com/en-us/download/details.aspx?id=16083");
                //splash.Close();
                //this.Close();
                Application.Exit();
            }
            catch (Exception e)
            {
                log.Fatal(e);
                CustomMessageBox.Show($"A Major error has occured : {e}");
                Application.Exit();
            }

            //set first instance display configuration
            if (DisplayConfiguration == null)
            {
                DisplayConfiguration = DisplayConfiguration.Advanced();
            }

            // load old config
            if (Settings.Instance["advancedview"] != null)
            {
                if (Settings.Instance.GetBoolean("advancedview") == true)
                {
                    DisplayConfiguration = new DisplayView().Advanced();
                }

                // remove old config
                Settings.Instance.Remove("advancedview");
            } //// load this before the other screens get loaded

            if (Settings.Instance["displayview"] != null)
            {
                try
                {
                    DisplayConfiguration = Settings.Instance.GetDisplayView("displayview");
                    //Force new view in case of saved view in config.xml
                    DisplayConfiguration.displayAdvancedParams = false;
                    DisplayConfiguration.displayStandardParams = false;
                    DisplayConfiguration.displayFullParamList = true;
                }
                catch
                {
                    DisplayConfiguration = DisplayConfiguration.Advanced();
                }
            }

            LayoutChanged += updateLayout;
            LayoutChanged(null, EventArgs.Empty);

            if (Settings.Instance["CHK_GDIPlus"] != null)
                GCSViews.FlightData.myhud.opengl = !bool.Parse(Settings.Instance["CHK_GDIPlus"].ToString());

            if (Settings.Instance["CHK_hudshow"] != null)
                GCSViews.FlightData.myhud.hudon = bool.Parse(Settings.Instance["CHK_hudshow"].ToString());

            try
            {
                if (Settings.Instance["MainLocX"] != null && Settings.Instance["MainLocY"] != null)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    Point startpos = new Point(Settings.Instance.GetInt32("MainLocX"),
                        Settings.Instance.GetInt32("MainLocY"));

                    // fix common bug which happens when user removes a monitor, the app shows up
                    // offscreen and it is very hard to move it onscreen.  Also happens with
                    // remote desktop a lot.  So this only restores position if the position
                    // is visible.
                    foreach (Screen s in Screen.AllScreens)
                    {
                        if (s.WorkingArea.Contains(startpos))
                        {
                            this.Location = startpos;
                            break;
                        }
                    }

                }

                if (Settings.Instance["MainMaximised"] != null)
                {
                    this.WindowState =
                        (FormWindowState) Enum.Parse(typeof(FormWindowState), Settings.Instance["MainMaximised"]);
                    // dont allow minimised start state
                    if (this.WindowState == FormWindowState.Minimized)
                    {
                        this.WindowState = FormWindowState.Normal;
                        this.Location = new Point(100, 100);
                    }
                }

                if (Settings.Instance["MainHeight"] != null)
                    this.Height = Settings.Instance.GetInt32("MainHeight");
                if (Settings.Instance["MainWidth"] != null)
                    this.Width = Settings.Instance.GetInt32("MainWidth");

                // set presaved default telem rates
                if (Settings.Instance["CMB_rateattitude"] != null)
                    CurrentState.rateattitudebackup = Settings.Instance.GetInt32("CMB_rateattitude");
                if (Settings.Instance["CMB_rateposition"] != null)
                    CurrentState.ratepositionbackup = Settings.Instance.GetInt32("CMB_rateposition");
                if (Settings.Instance["CMB_ratestatus"] != null)
                    CurrentState.ratestatusbackup = Settings.Instance.GetInt32("CMB_ratestatus");
                if (Settings.Instance["CMB_raterc"] != null)
                    CurrentState.ratercbackup = Settings.Instance.GetInt32("CMB_raterc");
                if (Settings.Instance["CMB_ratesensors"] != null)
                    CurrentState.ratesensorsbackup = Settings.Instance.GetInt32("CMB_ratesensors");

                //Load customfield names from config

                for (short i = 0; i < 20; i++)
                {
                    var fieldname = "customfield" + i.ToString();
                    if (Settings.Instance.ContainsKey(fieldname))
                        CurrentState.custom_field_names.Add(fieldname, Settings.Instance[fieldname].ToUpper());
                }

                // make sure rates propogate
                MainV2.comPort.MAV.cs.ResetInternals();

                if (Settings.Instance["speechenable"] != null)
                    MainV2.speechEnable = Settings.Instance.GetBoolean("speechenable");

                if (Settings.Instance["analyticsoptout"] != null)
                    MissionPlanner.Utilities.Tracking.OptOut = Settings.Instance.GetBoolean("analyticsoptout");

                try
                {
                    if (Settings.Instance["TXT_homelat"] != null)
                        MainV2.comPort.MAV.cs.PlannedHomeLocation.Lat = Settings.Instance.GetDouble("TXT_homelat");

                    if (Settings.Instance["TXT_homelng"] != null)
                        MainV2.comPort.MAV.cs.PlannedHomeLocation.Lng = Settings.Instance.GetDouble("TXT_homelng");

                    if (Settings.Instance["TXT_homealt"] != null)
                        MainV2.comPort.MAV.cs.PlannedHomeLocation.Alt = Settings.Instance.GetDouble("TXT_homealt");

                    // remove invalid entrys
                    if (Math.Abs(MainV2.comPort.MAV.cs.PlannedHomeLocation.Lat) > 90 ||
                        Math.Abs(MainV2.comPort.MAV.cs.PlannedHomeLocation.Lng) > 180)
                        MainV2.comPort.MAV.cs.PlannedHomeLocation = new PointLatLngAlt();
                }
                catch
                {
                }
            }
            catch
            {
            }

            Warnings.CustomWarning.defaultsrc = comPort.MAV.cs;
            Warnings.WarningEngine.Start(speechEnable ? speechEngine : null);
            Warnings.WarningEngine.WarningMessage += (sender, s) => { MainV2.comPort.MAV.cs.messageHigh = s; };
            Warnings.WarningEngine.QuickPanelColoring += WarningEngine_QuickPanelColoring;

            if (CurrentState.rateattitudebackup == 0) // initilised to 10, configured above from save
            {
                CustomMessageBox.Show("NOTE: your attitude rate is 0, the hud will not work\nChange in Configuration > Planner > Telemetry Rates");
            }

            // create log dir if it doesnt exist
            try
            {
                if (!Directory.Exists(Settings.Instance.LogDir))
                    Directory.CreateDirectory(Settings.Instance.LogDir);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
#if !NETSTANDARD2_0
#if !NETCOREAPP2_0
            Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            // make sure new enough .net framework is installed
            if (!MONO)
            {
                try
                {
                    Microsoft.Win32.RegistryKey installed_versions =
                        Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
                    string[] version_names = installed_versions.GetSubKeyNames();
                    //version names start with 'v', eg, 'v3.5' which needs to be trimmed off before conversion
                    double Framework = Convert.ToDouble(version_names[version_names.Length - 1].Remove(0, 1),
                        CultureInfo.InvariantCulture);
                    int SP =
                        Convert.ToInt32(installed_versions.OpenSubKey(version_names[version_names.Length - 1])
                            .GetValue("SP", 0));

                    if (Framework < 4.0)
                    {
                        CustomMessageBox.Show("This program requires .NET Framework 4.0. You currently have " + Framework);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

#endif
#endif

            if (Program.IconFile != null)
            {
                this.Icon = Icon.FromHandle(((Bitmap) Program.IconFile).GetHicon());
            }

            MenuArduPilot.Image = new Bitmap(Properties.Resources._0d92fed790a3a70170e61a86db103f399a595c70,
                (int) (200), 31);
            MenuArduPilot.Width = MenuArduPilot.Image.Width;

            if (Program.Logo2 != null)
                MenuArduPilot.Image = Program.Logo2;

            Application.DoEvents();

            Comports.Add(comPort);

            MainV2.comPort.MavChanged += comPort_MavChanged;

            // save config to test we have write access
            SaveConfig();
        }

        void cmb_sysid_Click(object sender, EventArgs e)
        {
            MainV2._connectionControl.UpdateSysIDS();
        }

        void comPort_MavChanged(object sender, EventArgs e)
        {
            log.Info($"Mav Changed {MainV2.comPort.MAV.sysid}");

            HUD.Custom.src = MainV2.comPort.MAV.cs;

            CustomWarning.defaultsrc = MainV2.comPort.MAV.cs;

            MissionPlanner.Controls.PreFlight.CheckListItem.defaultsrc = MainV2.comPort.MAV.cs;

            // when uploading a firmware we dont want to reload this screen.
            if (instance.MyView.current.Control != null &&
                instance.MyView.current.Control.GetType() == typeof(GCSViews.InitialSetup))
            {
                var page = ((GCSViews.InitialSetup) instance.MyView.current.Control).backstageView.SelectedPage;
                if (page != null && page.Text.Contains("Install Firmware"))
                {
                    return;
                }
            }
        }
#if !NETSTANDARD2_0
#if !NETCOREAPP2_0
        void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            // try prevent crash on resume
            if (e.Mode == Microsoft.Win32.PowerModes.Suspend)
            {
                doDisconnect(MainV2.comPort);
            }
        }
#endif
#endif
        private void BGLoadAirports(object nothing)
        {
            // read airport list
            try
            {
                Utilities.Airports.ReadOurairports($"{running_directory}airports.csv");

                Utilities.Airports.checkdups = true;

                //Utilities.Airports.ReadOpenflights(Application.StartupPath + Path.DirectorySeparatorChar + "airports.dat");

                log.Info("Loaded " + Utilities.Airports.GetAirportCount + " airports");
            }
            catch
            {
            }
        }

        public void switchicons(menuicons icons)
        {
            //Check if we starting
            if (displayicons != null)
            {
                // dont update if no change
                if (displayicons.GetType() == icons.GetType())
                    return;
            }

            displayicons = icons;

            MainMenu.BackColor = SystemColors.MenuBar;

            MainMenu.BackgroundImage = displayicons.bg;

            MenuFlightData.Image = displayicons.fd;
            MenuFlightPlanner.Image = displayicons.fp;
            MenuInitConfig.Image = displayicons.initsetup;
            MenuSimulation.Image = displayicons.sim;
            MenuConfigTune.Image = displayicons.config_tuning;
            MenuConnect.Image = displayicons.connect;
            MenuHelp.Image = displayicons.help;


            MenuFlightData.ForeColor = ThemeManager.TextColor;
            MenuFlightPlanner.ForeColor = ThemeManager.TextColor;
            MenuInitConfig.ForeColor = ThemeManager.TextColor;
            MenuSimulation.ForeColor = ThemeManager.TextColor;
            MenuConfigTune.ForeColor = ThemeManager.TextColor;
            MenuConnect.ForeColor = ThemeManager.TextColor;
            MenuHelp.ForeColor = ThemeManager.TextColor;
        }

        void adsb_UpdatePlanePosition(object sender, MissionPlanner.Utilities.adsb.PointLatLngAltHdg adsb)
        {
            lock (adsblock)
            {
                var id = adsb.Tag;

                if (MainV2.instance.adsbPlanes.ContainsKey(id))
                {
                    var plane = (adsb.PointLatLngAltHdg)instance.adsbPlanes[id];
                    if (plane.Source == null && sender != null)
                    {
                        log.DebugFormat("Ignoring MAVLink-sourced ADSB_VEHICLE for locally-known aircraft {0}", adsb.Tag);
                        return;
                    }

                    // update existing
                    plane.Lat = adsb.Lat;
                    plane.Lng = adsb.Lng;
                    plane.Alt = adsb.Alt;
                    plane.Heading = adsb.Heading;
                    plane.Time = DateTime.Now;
                    plane.CallSign = adsb.CallSign;
                    plane.Squawk = adsb.Squawk;
                    plane.Raw = adsb.Raw;
                    plane.Speed = adsb.Speed;
                    plane.VerticalSpeed = adsb.VerticalSpeed;
                    plane.Source = sender;
                    instance.adsbPlanes[id] = plane;
                }
                else
                {
                    // create new plane
                    MainV2.instance.adsbPlanes[id] =
                        new adsb.PointLatLngAltHdg(adsb.Lat, adsb.Lng,
                                adsb.Alt, adsb.Heading, adsb.Speed, id,
                                DateTime.Now)
                            {CallSign = adsb.CallSign, Squawk = adsb.Squawk, Raw = adsb.Raw, Source = sender};
                }
            }
        }


        private void ResetConnectionStats()
        {
            log.Info("Reset connection stats");
            // If the form has been closed, or never shown before, we need do nothing, as
            // connection stats will be reset when shown
            if (this.connectionStatsForm != null && connectionStatsForm.Visible)
            {
                // else the form is already showing.  reset the stats
                this.connectionStatsForm.Controls.Clear();
                _connectionStats = new ConnectionStats(comPort);
                this.connectionStatsForm.Controls.Add(_connectionStats);
                ThemeManager.ApplyThemeTo(this.connectionStatsForm);
            }
        }

        private void ShowConnectionStatsForm()
        {
            if (this.connectionStatsForm == null || this.connectionStatsForm.IsDisposed)
            {
                // If the form has been closed, or never shown before, we need all new stuff
                this.connectionStatsForm = new Form
                {
                    Width = 430,
                    Height = 180,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = Strings.LinkStats
                };
                // Change the connection stats control, so that when/if the connection stats form is showing,
                // there will be something to see
                this.connectionStatsForm.Controls.Clear();
                _connectionStats = new ConnectionStats(comPort);
                this.connectionStatsForm.Controls.Add(_connectionStats);
                this.connectionStatsForm.Width = _connectionStats.Width;
            }

            this.connectionStatsForm.Show();
            ThemeManager.ApplyThemeTo(this.connectionStatsForm);
        }

        private void CMB_serialport_Click(object sender, EventArgs e)
        {
            string oldport = _connectionControl.CMB_serialport.Text;
            PopulateSerialportList();
            if (_connectionControl.CMB_serialport.Items.Contains(oldport))
                _connectionControl.CMB_serialport.Text = oldport;
        }

        private void PopulateSerialportList()
        {
            _connectionControl.CMB_serialport.Items.Clear();

            _connectionControl.CMB_serialport.Items.Add("AUTO");
            _connectionControl.CMB_serialport.Items.AddRange(SerialPort.GetPortNames());

            _connectionControl.CMB_serialport.Items.Add("TCP");
            _connectionControl.CMB_serialport.Items.Add("UDP");
            _connectionControl.CMB_serialport.Items.Add("UDPCl");
            _connectionControl.CMB_serialport.Items.Add("WS");

            foreach (var item in ExtraConnectionList)
            {
                _connectionControl.CMB_serialport.Items.Add(item.Label);
            }
        }

        /// <summary>
        /// 添加可用的UDP端口选项到下拉菜单
        /// </summary>
        private void AddAvailableUdpPorts()
        {
            try
            {
                // 默认的UDP端口列表
                var defaultPorts = new[] { "14550", "14551", "14552", "14553", "14554", "14555" };
                var availablePorts = new List<string>();
                
                // 检测每个端口是否可用
                foreach (var port in defaultPorts)
                {
                    if (IsUdpPortAvailable(port))
                    {
                        availablePorts.Add($"UDP:{port}");
                    }
                }
                
                // 如果至少有一个端口可用，添加到下拉菜单
                if (availablePorts.Count > 0)
                {
                    foreach (var port in availablePorts)
                    {
                        _connectionControl.CMB_serialport.Items.Add(port);
                    }
                }
                else
                {
                    // 如果没有检测到可用端口，添加默认UDP选项
                    _connectionControl.CMB_serialport.Items.Add("UDP");
                }
            }
            catch (Exception ex)
            {
                // 出错时回退到默认UDP选项
                _connectionControl.CMB_serialport.Items.Add("UDP");
                log.Warn("Failed to detect UDP ports", ex);
            }
        }

        /// <summary>
        /// 检测UDP端口是否可用（未被占用）
        /// </summary>
        private bool IsUdpPortAvailable(string port)
        {
            try
            {
                if (!int.TryParse(port, out int portNum))
                    return false;
                
                // 尝试绑定UDP端口来检测是否可用
                using (var udpClient = new UdpClient())
                {
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, portNum));
                    return true;
                }
            }
            catch
            {
                // 端口被占用或无法绑定
                return false;
            }
        }

        private void MenuFlightData_Click(object sender, EventArgs e)
        {
            MyView.ShowScreen("FlightData");

            // save config
            SaveConfig();
        }

        private void MenuFlightPlanner_Click(object sender, EventArgs e)
        {
            MyView.ShowScreen("FlightPlanner");

            // save config
            SaveConfig();
        }

        public void MenuSetup_Click(object sender, EventArgs e)
        {
            if (Settings.Instance.GetBoolean("password_protect") == false)
            {
                MyView.ShowScreen("HWConfig");
            }
            else
            {
                var pw = "";
                if (InputBox.Show("Enter Password", "Please enter your password", ref pw, true) ==
                    System.Windows.Forms.DialogResult.OK)
                {
                    bool ans = Password.ValidatePassword(pw);

                    if (ans == false)
                    {
                        CustomMessageBox.Show("Bad Password", "Bad Password");
                    }
                }

                if (Password.VerifyPassword(pw))
                {
                    MyView.ShowScreen("HWConfig");
                }
            }
        }

        private void MenuSimulation_Click(object sender, EventArgs e)
        {
            MyView.ShowScreen("Simulation");
        }

        private void MenuTuning_Click(object sender, EventArgs e)
        {
            if (Settings.Instance.GetBoolean("password_protect") == false)
            {
                MyView.ShowScreen("SWConfig");
            }
            else
            {
                var pw = "";
                if (InputBox.Show("Enter Password", "Please enter your password", ref pw, true) ==
                    System.Windows.Forms.DialogResult.OK)
                {
                    bool ans = Password.ValidatePassword(pw);

                    if (ans == false)
                    {
                        CustomMessageBox.Show("Bad Password", "Bad Password");
                    }
                }

                if (Password.VerifyPassword(pw))
                {
                    MyView.ShowScreen("SWConfig");
                }
            }
        }

        private void MenuTerminal_Click(object sender, EventArgs e)
        {
            MyView.ShowScreen("Terminal");
        }

        /// <summary>
        /// 加载自动连接配置
        /// </summary>
        private void LoadAutoConnectConfig()
        {
            try
            {
                // 获取本机IP地址作为默认主地址
                string localIP = GetLocalIPAddress();
                //主地址
                string primaryHost = Settings.Instance.GetString("AutoConnect_PrimaryHost", localIP);
                //备地址
                string backupHost = Settings.Instance.GetString("AutoConnect_BackupHost", "192.168.4.13");
                // 端口向后兼容：如果没有分别配置，则使用 AutoConnect_Port
                string defaultPort = Settings.Instance.GetString("AutoConnect_Port", "5760");
                string primaryPort = Settings.Instance.GetString("AutoConnect_PrimaryPort", defaultPort);
                string backupPort = Settings.Instance.GetString("AutoConnect_BackupPort", defaultPort);
                // 质量门限（0.0-1.0），窗口秒数和最小切换间隔秒数
                double qualityThreshold = Settings.Instance.GetDouble("AutoConnect_QualityThreshold", 0.7);
                int qualityWindowSec = Settings.Instance.GetInt32("AutoConnect_QualityWindowSec", 3);
                int minSwitchIntervalSec = Settings.Instance.GetInt32("AutoConnect_MinSwitchIntervalSec", 10);
                bool enabled = Settings.Instance.GetBoolean("AutoConnect_Enabled", true); // 默认启用

                // 设置TCP地址配置
                AutoConnectManager.SetTcpAddresses(primaryHost, primaryPort, backupHost, backupPort);
                AutoConnectManager.SetQualityPolicy(qualityThreshold, qualityWindowSec, minSwitchIntervalSec);
                AutoConnectManager.EnableDualListen = true; // 启用：双端UDP监听功能
                
                // 加载上次连接的TCP地址作为当前地址
                string lastHost = Settings.Instance.GetString("LastTCP_Host", "");
                if (!string.IsNullOrEmpty(lastHost))
                {
                    AutoConnectManager.CurrentTcpHost = lastHost;
                    log.Info($"Loaded last TCP host: {lastHost}");
                }
                
                if (enabled)
                {
                    AutoConnectManager.EnableAutoConnect();
                }

                log.Info($"AutoConnect configured - Primary: {primaryHost}:{primaryPort}, Backup: {backupHost}:{backupPort}, Threshold: {qualityThreshold:P0}, Window: {qualityWindowSec}s, MinSwitch: {minSwitchIntervalSec}s, DualListen: {AutoConnectManager.EnableDualListen}, Enabled: {enabled}");
            }
            catch (Exception ex)
            {
                log.Error("Error loading auto connect config", ex);
            }
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        private string GetLocalIPAddress()
        {
            try
            {
                using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                    return endPoint?.Address.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        public void UpdateConnectionStatus(bool isConnected)
        {
            try
            {
                _connectionControl.IsConnected(isConnected);
                
                // 直接更新连接图标，避免访问私有方法
                if (isConnected)
                {
                    this.MenuConnect.Image = displayicons.disconnect;
                    this.MenuConnect.Image.Tag = "Disconnect";
                }
                else
                {
                    this.MenuConnect.Image = displayicons.connect;
                    this.MenuConnect.Image.Tag = "Connect";
                }
            }
            catch (Exception ex)
            {
                log.Error("Error updating connection status", ex);
            }
        }

        /// <summary>
        /// 显示手动连接对话框
        /// </summary>
        private void ShowManualConnectDialog()
        {
            try
            {
                // 创建手动连接对话框
                using (var dialog = new Form())
                {
                    dialog.Text = "手动连接";
                    dialog.Size = new Size(350, 150);
                    dialog.StartPosition = FormStartPosition.CenterParent;
                    dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dialog.MaximizeBox = false;
                    dialog.MinimizeBox = false;

                    // 地址标签
                    var lblHost = new Label
                    {
                        Text = "地址:",
                        Location = new Point(20, 20),
                        Size = new Size(50, 20)
                    };

                    // 地址输入框
                    var txtHost = new TextBox
                    {
                        Location = new Point(80, 18),
                        Size = new Size(150, 20),
                        Text = !string.IsNullOrEmpty(_lastManualHost) ? _lastManualHost : AutoConnectManager.PrimaryTcpHost
                    };

                    // 端口标签
                    var lblPort = new Label
                    {
                        Text = "端口:",
                        Location = new Point(20, 50),
                        Size = new Size(50, 20)
                    };

                    // 端口输入框
                    var txtPort = new TextBox
                    {
                        Location = new Point(80, 48),
                        Size = new Size(150, 20),
                        Text = !string.IsNullOrEmpty(_lastManualPort) ? _lastManualPort : AutoConnectManager.GetPortForHost(AutoConnectManager.PrimaryTcpHost)
                    };

                    // 确定按钮
                    var btnOK = new Button
                    {
                        Text = "连接",
                        Location = new Point(150, 80),
                        Size = new Size(75, 25),
                        DialogResult = DialogResult.OK
                    };

                    // 取消按钮
                    var btnCancel = new Button
                    {
                        Text = "取消",
                        Location = new Point(235, 80),
                        Size = new Size(75, 25),
                        DialogResult = DialogResult.Cancel
                    };

                    // 添加控件到对话框
                    dialog.Controls.AddRange(new Control[] { lblHost, txtHost, lblPort, txtPort, btnOK, btnCancel });

                    // 设置默认按钮
                    dialog.AcceptButton = btnOK;
                    dialog.CancelButton = btnCancel;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // 验证输入
                        if (string.IsNullOrWhiteSpace(txtHost.Text))
                        {
                            CustomMessageBox.Show("请输入有效的地址", "错误");
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(txtPort.Text) || !int.TryParse(txtPort.Text, out int port))
                        {
                            CustomMessageBox.Show("请输入有效的端口号", "错误");
                            return;
                        }

                        // 保存手动连接信息
                        _lastManualHost = txtHost.Text;
                        _lastManualPort = txtPort.Text;

                        // 创建TCP连接
                        var tcpSerial = new TcpSerial();
                        tcpSerial.Host = txtHost.Text;
                        tcpSerial.Port = port.ToString();
                        comPort.BaseStream = tcpSerial;
                        _connectionControl.CMB_serialport.Text = "TCP";

                        // 标记为手动连接
                        AutoConnectManager.MarkManualConnect();

                        // 执行连接
                        doConnect(comPort, "preset", "0");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error showing manual connect dialog", ex);
                CustomMessageBox.Show("显示手动连接对话框时发生错误: " + ex.Message, "错误");
            }
        }

        /// <summary>
        /// 配置自动连接设置
        /// </summary>
        private void ConfigureAutoConnect()
        {
            try
            {
                // 获取当前配置
                string primaryHost = AutoConnectManager.PrimaryTcpHost;
                string backupHost = AutoConnectManager.BackupTcpHost;
                string primaryPort = AutoConnectManager.GetPortForHost(primaryHost);
                string backupPort = AutoConnectManager.GetPortForHost(backupHost);
                double qualityThreshold = Settings.Instance.GetDouble("AutoConnect_QualityThreshold", 0.7);
                int qualityWindowSec = Settings.Instance.GetInt32("AutoConnect_QualityWindowSec", 3);
                int minSwitchIntervalSec = Settings.Instance.GetInt32("AutoConnect_MinSwitchIntervalSec", 10);

                // 创建配置对话框
                using (var dialog = new Form())
                {
                    dialog.Text = "自动连接配置";
                    dialog.Size = new Size(400, 200);
                    dialog.StartPosition = FormStartPosition.CenterParent;

                    // 主TCP地址
                    var lblPrimary = new Label
                    {
                        Text = "主TCP地址:",
                        Location = new Point(20, 20),
                        Size = new Size(80, 20)
                    };
                    var txtPrimary = new TextBox
                    {
                        Text = primaryHost,
                        Location = new Point(100, 18),
                        Size = new Size(200, 20)
                    };

                    // 端口
                    var lblPort = new Label
                    {
                        Text = "主端口:",
                        Location = new Point(20, 80),
                        Size = new Size(80, 20)
                    };
                    var txtPrimaryPort = new TextBox
                    {
                        Text = primaryPort,
                        Location = new Point(100, 78),
                        Size = new Size(100, 20)
                    };

                    var lblQ = new Label
                    {
                        Text = "质量阈值(0-1):",
                        Location = new Point(20, 110),
                        Size = new Size(100, 20)
                    };
                    var txtQ = new TextBox
                    {
                        Text = qualityThreshold.ToString("0.00"),
                        Location = new Point(120, 108),
                        Size = new Size(60, 20)
                    };

                    var lblWin = new Label
                    {
                        Text = "窗口(s):",
                        Location = new Point(190, 110),
                        Size = new Size(60, 20)
                    };
                    var txtWin = new TextBox
                    {
                        Text = qualityWindowSec.ToString(),
                        Location = new Point(250, 108),
                        Size = new Size(40, 20)
                    };

                    var lblMin = new Label
                    {
                        Text = "最小切换(s):",
                        Location = new Point(300, 110),
                        Size = new Size(80, 20)
                    };
                    var txtMin = new TextBox
                    {
                        Text = minSwitchIntervalSec.ToString(),
                        Location = new Point(380, 108),
                        Size = new Size(50, 20)
                    };

                    // 启用自动连接
                    var chkEnabled = new CheckBox
                    {
                        Text = "启用自动连接",
                        Location = new Point(20, 110),
                        Size = new Size(150, 20),
                        Checked = AutoConnectManager.IsEnabled
                    };


                    // 确定按钮
                    var btnOK = new Button
                    {
                        Text = "确定",
                        Location = new Point(200, 140),
                        Size = new Size(75, 25),
                        DialogResult = DialogResult.OK
                    };

                    // 取消按钮
                    var btnCancel = new Button
                    {
                        Text = "取消",
                        Location = new Point(285, 140),
                        Size = new Size(75, 25),
                        DialogResult = DialogResult.Cancel
                    };

                    dialog.Controls.AddRange(new Control[] { lblPrimary, txtPrimary, lblPort, txtPrimaryPort, lblQ, txtQ, lblWin, txtWin, lblMin, txtMin, chkEnabled, btnOK, btnCancel });

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // 保存配置
                        AutoConnectManager.SetTcpAddresses(txtPrimary.Text, txtPrimaryPort.Text, backupHost, backupPort);
                        if (double.TryParse(txtQ.Text, out var q) && int.TryParse(txtWin.Text, out var w) && int.TryParse(txtMin.Text, out var m))
                        {
                            AutoConnectManager.SetQualityPolicy(Math.Max(0.0, Math.Min(1.0, q)), Math.Max(1, w), Math.Max(1, m));
                        }
                        
                        AutoConnectManager.EnableDualListen = true; // 启用：双端UDP监听功能

                        if (chkEnabled.Checked)
                        {
                            AutoConnectManager.EnableAutoConnect();
                        }
                        else
                        {
                            AutoConnectManager.DisableAutoConnect();
                        }

                        // 保存到设置
                        Settings.Instance["AutoConnect_PrimaryHost"] = txtPrimary.Text;
                        Settings.Instance["AutoConnect_BackupHost"] = backupHost;
                        Settings.Instance["AutoConnect_PrimaryPort"] = txtPrimaryPort.Text;
                        Settings.Instance["AutoConnect_BackupPort"] = backupPort;
                        Settings.Instance["AutoConnect_QualityThreshold"] = (txtQ.Text);
                        Settings.Instance["AutoConnect_QualityWindowSec"] = (txtWin.Text);
                        Settings.Instance["AutoConnect_MinSwitchIntervalSec"] = (txtMin.Text);
                        Settings.Instance["AutoConnect_Enabled"] = chkEnabled.Checked.ToString();

                        CustomMessageBox.Show("自动连接配置已保存", "配置成功");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error configuring auto connect", ex);
                CustomMessageBox.Show("配置自动连接时发生错误: " + ex.Message, "错误");
            }
        }

        public void doDisconnect(MAVLinkInterface comPort)
        {
            log.Info("We are disconnecting");
            
            // 保存当前连接信息（如果是TCP连接）
            if (comPort.BaseStream is TcpSerial tcpStream)
            {
                _lastManualHost = tcpStream.Host;
                _lastManualPort = tcpStream.Port.ToString();
                log.Info($"Saved manual connection info: {_lastManualHost}:{_lastManualPort}");
            }
            
            // 标记为手动断开连接
            AutoConnectManager.MarkManualDisconnect();
            
            try
            {
                if (speechEngine != null) // cancel all pending speech
                    speechEngine.SpeakAsyncCancelAll();

                comPort.BaseStream.DtrEnable = false;
                comPort.Close();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            // now that we have closed the connection, cancel the connection stats
            // so that the 'time connected' etc does not grow, but the user can still
            // look at the now frozen stats on the still open form
            try
            {
                // if terminal is used, then closed using this button.... exception
                if (this.connectionStatsForm != null)
                    ((ConnectionStats) this.connectionStatsForm.Controls[0]).StopUpdates();
            }
            catch
            {
            }

            // refresh config window if needed
            if (MyView.current != null)
            {
                if (MyView.current.Name == "HWConfig")
                    MyView.ShowScreen("HWConfig");
                if (MyView.current.Name == "SWConfig")
                    MyView.ShowScreen("SWConfig");
            }

            try
            {
                System.Threading.ThreadPool.QueueUserWorkItem((WaitCallback) delegate
                    {
                        try
                        {
                            MissionPlanner.Log.LogSort.SortLogs(Directory.GetFiles(Settings.Instance.LogDir, "*.tlog"));
                        }
                        catch
                        {
                        }
                    }
                );
            }
            catch
            {
            }

            this.MenuConnect.Image = global::MissionPlanner.Properties.Resources.light_connect_icon;
        }

        public void doConnect(MAVLinkInterface comPort, string portname, string baud, bool getparams = true, bool showui = true)
        {
            bool skipconnectcheck = false;
            log.Info($"We are connecting to {portname} {baud}");
            switch (portname)
            {
                case "preset":
                    skipconnectcheck = true;
                    this.BeginInvokeIfRequired(() =>
                    {
                        if (comPort.BaseStream is TcpSerial)
                            _connectionControl.CMB_serialport.Text = "TCP";
                        if (comPort.BaseStream is UdpSerial)
                            _connectionControl.CMB_serialport.Text = "UDP";
                        if (comPort.BaseStream is UdpSerialConnect)
                            _connectionControl.CMB_serialport.Text = "UDPCl";
                        if (comPort.BaseStream is SerialPort)
                        {
                            _connectionControl.CMB_serialport.Text = comPort.BaseStream.PortName;
                            _connectionControl.CMB_baudrate.Text = comPort.BaseStream.BaudRate.ToString();
                            ((SerialPort)comPort.BaseStream).espFix = Settings.Instance.GetBoolean("CHK_rtsresetesp32", false);

                        }
                    });
                    break;
                case "TCP":
                    var tcpSerial = new TcpSerial();
                    comPort.BaseStream = tcpSerial;
                    _connectionControl.CMB_serialport.Text = "TCP";
                    
                    // 设置默认端口，避免弹出端口输入框
                    // 只让用户输入地址，端口使用默认值
                    tcpSerial.Port = AutoConnectManager.GetPortForHost(AutoConnectManager.PrimaryTcpHost);
                    
                    // 根据连接类型设置标志
                    if (AutoConnectManager.IsAutoConnecting)
                    {
                        AutoConnectManager.MarkAutoConnect();
                    }
                    else
                    {
                        AutoConnectManager.MarkManualConnect();
                    }
                    break;
                case "UDP":
                    var udpBase = new UdpSerial();
                    // 端口选择弹窗（14551/14552），选择后抑制内部再次弹窗
                    try
                    {
                        var sel = SelectUdpPort();
                        if (string.IsNullOrEmpty(sel))
                            return; // 取消
                        udpBase.Port = sel;
                        udpBase.SuppressPrompts = true;
                    }
                    catch
                    {
                        // 回退到默认
                        udpBase.Port = "14551";
                        udpBase.SuppressPrompts = false;
                    }
                    comPort.BaseStream = udpBase;
                    _connectionControl.CMB_serialport.Text = "UDP";
                    break;
                case "WS":
                    comPort.BaseStream = new WebSocket();
                    _connectionControl.CMB_serialport.Text = "WS";
                    break;
                case "UDPCl":
                    comPort.BaseStream = new UdpSerialConnect();
                    _connectionControl.CMB_serialport.Text = "UDPCl";
                    break;
                case "AUTO":
                    // do autoscan
                    Comms.CommsSerialScan.Scan(true);
                    DateTime deadline = DateTime.Now.AddSeconds(50);
                    ProgressReporterDialogue prd = new ProgressReporterDialogue();
                    prd.UpdateProgressAndStatus(-1, "Waiting for ports");
                    prd.DoWork += sender =>
                    {
                        while (Comms.CommsSerialScan.foundport == false || Comms.CommsSerialScan.run == 1)
                        {
                            System.Threading.Thread.Sleep(500);
                            Console.WriteLine("wait for port " + CommsSerialScan.foundport + " or " +
                                              CommsSerialScan.run);
                            if (sender.doWorkArgs.CancelRequested)
                            {
                                sender.doWorkArgs.CancelAcknowledged = true;
                                return;
                            }

                            if (DateTime.Now > deadline)
                            {
                                _connectionControl.IsConnected(false);
                                throw new Exception(Strings.Timeout);
                            }
                        }
                    };
                    prd.RunBackgroundOperationAsync();
                    return;
                default:
                    var extraconfig = ExtraConnectionList.Any(a => a.Label == portname);
                    if (extraconfig)
                    {
                        var config = ExtraConnectionList.First(a => a.Label == portname);
                        config.Enabled = true;
                        AutoConnect.ProcessEntry(config);
                        return;
                    }

                    var customport = CustomPortList.Any(a => a.Key.IsMatch(portname));
                    if (customport)
                    {
                        comPort.BaseStream = CustomPortList.First(a => a.Key.IsMatch(portname)).Value(portname, baud);
                    }
                    else
                    {
                        comPort.BaseStream = new SerialPort();
                        ((SerialPort)comPort.BaseStream).espFix = Settings.Instance.GetBoolean("CHK_rtsresetesp32", false);

                    }
                    break;
            }

            // Tell the connection UI that we are now connected.
            this.BeginInvokeIfRequired(() =>
            {
                _connectionControl.IsConnected(true);

                // Here we want to reset the connection stats counter etc.
                this.ResetConnectionStats();
            });

            comPort.MAV.cs.ResetInternals();

            //cleanup any log being played
            comPort.logreadmode = false;
            if (comPort.logplaybackfile != null)
                comPort.logplaybackfile.Close();
            comPort.logplaybackfile = null;

            try
            {
                log.Info("Set Portname");
                // set port, then options
                if (portname.ToLower() != "preset")
                    comPort.BaseStream.PortName = portname;

                log.Info("Set Baudrate");
                try
                {
                    if (baud != "" && baud != "0" && baud.IsNumber())
                        comPort.BaseStream.BaudRate = int.Parse(baud);
                }
                catch (Exception exp)
                {
                    log.Error(exp);
                }

                // prevent serialreader from doing anything
                comPort.giveComport = true;

                log.Info("About to do dtr if needed");
                // reset on connect logic.
                if (Settings.Instance.GetBoolean("CHK_resetapmonconnect") == true)
                {
                    log.Info("set dtr rts to false");
                    comPort.BaseStream.DtrEnable = false;
                    comPort.BaseStream.RtsEnable = false;

                    comPort.BaseStream.toggleDTR();
                }

                comPort.giveComport = false;

                // setup to record new logs
                try
                {
                    Directory.CreateDirectory(Settings.Instance.LogDir);
                    lock (this)
                    {
                        // create log names
                        var dt = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                        var tlog = Settings.Instance.LogDir + Path.DirectorySeparatorChar +
                                   dt + ".tlog";
                        var rlog = Settings.Instance.LogDir + Path.DirectorySeparatorChar +
                                   dt + ".rlog";

                        // check if this logname already exists
                        int a = 1;
                        while (File.Exists(tlog))
                        {
                            Thread.Sleep(1000);
                            // create new names with a as an index
                            dt = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "-" + a.ToString();
                            tlog = Settings.Instance.LogDir + Path.DirectorySeparatorChar +
                                   dt + ".tlog";
                            rlog = Settings.Instance.LogDir + Path.DirectorySeparatorChar +
                                   dt + ".rlog";
                        }

                        //open the logs for writing
                        comPort.logfile =
                            new BufferedStream(
                                File.Open(tlog, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None));
                        comPort.rawlogfile =
                            new BufferedStream(
                                File.Open(rlog, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None));
                        log.Info($"creating logfile {dt}.tlog");
                    }
                }
                catch (Exception exp2)
                {
                    log.Error(exp2);
                    CustomMessageBox.Show(Strings.Failclog);
                } // soft fail

                // reset connect time - for timeout functions
                connecttime = DateTime.UtcNow;

                // do the connect
                comPort.Open(false, skipconnectcheck, showui);

                if (!comPort.BaseStream.IsOpen)
                {
                    log.Info("comport is closed. existing connect");
                    try
                    {
                        _connectionControl.IsConnected(false);
                        UpdateConnectIcon();
                        comPort.Close();
                    }
                    catch
                    {
                    }

                    return;
                }

                // 手动UDP连接成功后：若启用双监听，则启动被动监听
                try
                {
                    if (AutoConnectManager.IsEnabled && AutoConnectManager.EnableDualListen && !AutoConnectManager.IsAutoConnecting &&
                        (comPort.BaseStream is UdpSerial || comPort.BaseStream is UdpSerialConnect))
                    {
                        AutoConnectManager.TriggerPassiveListenIfNeeded();
                    }
                }
                catch { }

                //158	MAV_COMP_ID_PERIPHERAL	Generic autopilot peripheral component ID. Meant for devices that do not implement the parameter microservice.
                if (getparams && comPort.MAV.compid != (byte)MAVLink.MAV_COMPONENT.MAV_COMP_ID_PERIPHERAL)
                {
                    if (UseCachedParams && File.Exists(comPort.MAV.ParamCachePath) &&
                        new FileInfo(comPort.MAV.ParamCachePath).LastWriteTime > DateTime.Now.AddHours(-1))
                    {
                        File.ReadAllText(comPort.MAV.ParamCachePath).FromJSON<MAVLink.MAVLinkParamList>()
                            .ForEach(a => comPort.MAV.param.Add(a));
                        comPort.MAV.param.TotalReported = comPort.MAV.param.TotalReceived;
                    }
                    else
                    {
                        if (Settings.Instance.GetBoolean("Params_BG", false))
                        {
                            Task.Run(() =>
                            {
                                try
                                {
                                    comPort.getParamListMavftp(comPort.MAV.sysid, comPort.MAV.compid);
                                }
                                catch
                                {

                                }
                            });
                        }
                        else
                        {
                            comPort.getParamList();
                        }
                    }
                }

                // check for newer firmware
                if (showui)
                    Task.Run(() =>
                    {
                        try
                        {
                            string[] fields1 = comPort.MAV.VersionString.Split(' ');

                            var softwares = APFirmware.GetReleaseNewest(APFirmware.RELEASE_TYPES.OFFICIAL);

                            foreach (var item in softwares)
                            {
                                // check primare firmware type. ie arudplane, arducopter
                                if (fields1[0].ToLower().Contains(item.VehicleType.ToLower()))
                                {
                                    Version ver1 = VersionDetection.GetVersion(comPort.MAV.VersionString);
                                    Version ver2 = item.MavFirmwareVersion;

                                    if (ver2 > ver1)
                                    {
                                        Common.MessageShowAgain(Strings.NewFirmware + "-" + item.VehicleType + " " + ver2,
                                            Strings.NewFirmwareA + item.VehicleType + " " + ver2 + Strings.Pleaseup +
                                            "[link;https://discuss.ardupilot.org/tags/stable-release;Release Notes]");
                                        break;
                                    }

                                    // check the first hit only
                                    break;
                                }
                            }

                            // load version specific config
                            ParameterMetaDataRepositoryAPMpdef.GetMetaDataVersioned(VersionDetection.GetVersion(comPort.MAV.VersionString));
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    });

                this.BeginInvokeIfRequired(() =>
                {
                    _connectionControl.UpdateSysIDS();

                    FlightData.CheckBatteryShow();

                    // save the baudrate for this port
                    Settings.Instance[_connectionControl.CMB_serialport.Text.Replace(" ","_") + "_BAUD"] =
                        _connectionControl.CMB_baudrate.Text;

                    this.Text = titlebar + " " + comPort.MAV.VersionString + " on " + comPort.MAV.SerialString;

                    // refresh config window if needed
                    if (MyView.current != null && showui)
                    {
                        if (MyView.current.Name == "HWConfig")
                            MyView.ShowScreen("HWConfig");
                        if (MyView.current.Name == "SWConfig")
                            MyView.ShowScreen("SWConfig");
                    }

                    // load wps on connect option.
                    if (Settings.Instance.GetBoolean("loadwpsonconnect") == true && showui)
                    {
                        // only do it if we are connected.
                        if (comPort.BaseStream.IsOpen)
                        {
                            MenuFlightPlanner_Click(null, null);
                            FlightPlanner.BUT_read_Click(null, null);
                        }
                    }

                    // get any rallypoints
                    if (MainV2.comPort.MAV.param.ContainsKey("RALLY_TOTAL") &&
                        int.Parse(MainV2.comPort.MAV.param["RALLY_TOTAL"].ToString()) > 0 && showui)
                    {
                        try
                        {
                            FlightPlanner.getRallyPointsToolStripMenuItem_Click(null, null);

                            double maxdist = 0;

                            foreach (var rally in comPort.MAV.rallypoints)
                            {
                                foreach (var rally1 in comPort.MAV.rallypoints)
                                {
                                    var pnt1 = new PointLatLngAlt(rally.Value.y / 10000000.0f, rally.Value.x / 10000000.0f);
                                    var pnt2 = new PointLatLngAlt(rally1.Value.y / 10000000.0f,
                                        rally1.Value.x / 10000000.0f);

                                    var dist = pnt1.GetDistance(pnt2);

                                    maxdist = Math.Max(maxdist, dist);
                                }
                            }

                            if (comPort.MAV.param.ContainsKey("RALLY_LIMIT_KM") &&
                                (maxdist / 1000.0) > (float)comPort.MAV.param["RALLY_LIMIT_KM"])
                            {
                                CustomMessageBox.Show(Strings.Warningrallypointdistance + " " +
                                                      (maxdist / 1000.0).ToString("0.00") + " > " +
                                                      (float)comPort.MAV.param["RALLY_LIMIT_KM"]);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Warn(ex);
                        }
                    }

                    // get any fences
                    if (MainV2.comPort.MAV.param.ContainsKey("FENCE_TOTAL") &&
                        int.Parse(MainV2.comPort.MAV.param["FENCE_TOTAL"].ToString()) > 1 &&
                        MainV2.comPort.MAV.param.ContainsKey("FENCE_ACTION") && showui)
                    {
                        try
                        {
                            FlightPlanner.GeoFencedownloadToolStripMenuItem_Click(null, null);
                        }
                        catch (Exception ex)
                        {
                            log.Warn(ex);
                        }
                    }

                    //Add HUD custom items source
                    HUD.Custom.src = MainV2.comPort.MAV.cs;

                    // set connected icon
                    this.MenuConnect.Image = displayicons.disconnect;
                    
                    // 如果是TCP连接，初始化自动连接管理器
                    // if (comPort.BaseStream is TcpSerial)
                    // {
                    //     // 检测当前连接的TCP地址
                    //     var tcpSerial = comPort.BaseStream as TcpSerial;
                    //     if (!string.IsNullOrEmpty(tcpSerial.Host))
                    //     {
                    //         // 设置当前TCP地址到自动连接管理器
                    //         AutoConnectManager.CurrentTcpHost = tcpSerial.Host;
                    //         log.Info($"Detected current TCP host: {tcpSerial.Host}");
                    //     }
                        
                    //     AutoConnectManager.Initialize();
                    //     AutoConnectManager.EnableAutoConnect();
                    // }
                    AutoConnectManager.Initialize();
                    AutoConnectManager.EnableAutoConnect();
                });
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                try
                {
                    _connectionControl.IsConnected(false);
                    UpdateConnectIcon();
                    comPort.Close();
                }
                catch (Exception ex2)
                {
                    log.Warn(ex2);
                }

                CustomMessageBox.Show($"Can not establish a connection\n\n{ex.Message}");
                return;
            }
        }


        private void MenuConnect_Click(object sender, EventArgs e)
        {
            Connect();

            // save config
            SaveConfig();
            if (comPort.BaseStream.IsOpen)
                _connectionControl.UpdateSysIDS();
        }

        private void Connect()
        {
            comPort.giveComport = false;

            log.Info("MenuConnect Start");

            // sanity check
            if (comPort.BaseStream.IsOpen && comPort.MAV.cs.groundspeed > 4)
            {
                if ((int) DialogResult.No ==
                    CustomMessageBox.Show(Strings.Stillmoving, Strings.Disconnect, MessageBoxButtons.YesNo))
                {
                    return;
                }
            }

            try
            {
                log.Info("Cleanup last logfiles");
                // cleanup from any previous sessions
                if (comPort.logfile != null)
                    comPort.logfile.Close();

                if (comPort.rawlogfile != null)
                    comPort.rawlogfile.Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(Strings.ErrorClosingLogFile + ex.Message, Strings.ERROR);
            }

            comPort.logfile = null;
            comPort.rawlogfile = null;

            // decide if this is a connect or disconnect
            if (comPort.BaseStream.IsOpen)
            {
                doDisconnect(comPort);
            }
            else
            {
                // 检查是否是手动连接（非自动连接）
                if (!AutoConnectManager.IsAutoConnecting)
                {
                    // 手动连接：根据端口类型分别弹窗
                    // 使用comPortName来判断，支持"UDP"或"UDP:端口"格式
                    if (string.Equals(comPortName, "UDP", StringComparison.OrdinalIgnoreCase) || 
                        comPortName.StartsWith("UDP:", StringComparison.OrdinalIgnoreCase))
                    {
                        var udpBase = new UdpSerial();
                        try
                        {
                            var chosen = SelectUdpPort();
                            if (string.IsNullOrEmpty(chosen))
                                return; // 用户取消
                            udpBase.Port = chosen; // 14551/14552
                            udpBase.SuppressPrompts = true; // 避免再次弹窗
                        }
                        catch
                        {
                            udpBase.Port = "14551";
                            udpBase.SuppressPrompts = false;
                        }

                        comPort.BaseStream = udpBase;
                        _connectionControl.CMB_serialport.Text = "UDP";

                        // 标记为手动连接
                        AutoConnectManager.MarkManualConnect();

                        // 直接连接
                        doConnect(comPort, "preset", "0");
                    }
                    else
                    {
                        // TCP 等其他类型：使用地址+端口手动输入对话框
                        ShowManualConnectDialog();
                    }
                }
                else
                {
                    // 自动连接，使用默认配置
                    doConnect(comPort, _connectionControl.CMB_serialport.Text, _connectionControl.CMB_baudrate.Text);
                }
            }

            _connectionControl.UpdateSysIDS();

            if (comPort.BaseStream.IsOpen)
                loadph_serial();
        }

        void loadph_serial()
        {
            try
            {
                if (comPort.MAV.SerialString == "")
                    return;

                if (comPort.MAV.SerialString.Contains("CubeBlack") &&
                    !comPort.MAV.SerialString.Contains("CubeBlack+") &&
                    comPort.MAV.param.ContainsKey("INS_ACC3_ID") && comPort.MAV.param["INS_ACC3_ID"].Value == 0 &&
                    comPort.MAV.param.ContainsKey("INS_GYR3_ID") && comPort.MAV.param["INS_GYR3_ID"].Value == 0 &&
                    comPort.MAV.param.ContainsKey("INS_ENABLE_MASK") && comPort.MAV.param["INS_ENABLE_MASK"].Value >= 7)
                {
                    MissionPlanner.Controls.SB.Show("Param Scan");
                }
            }
            catch
            {
            }

            try
            {
                if (comPort.MAV.SerialString == "")
                    return;

                // brd type should be 3
                // devids show which sensor is not detected
                // baro does not list a devid

                //devop read spi lsm9ds0_ext_am 0 0 0x8f 1
                if (comPort.MAV.SerialString.Contains("CubeBlack") && !comPort.MAV.SerialString.Contains("CubeBlack+"))
                {
                    Task.Run(() =>
                    {
                        bool bad1 = false;
                        byte[] data = new byte[0];

                        comPort.device_op(comPort.MAV.sysid, comPort.MAV.compid, out data,
                            MAVLink.DEVICE_OP_BUSTYPE.SPI,
                            "lsm9ds0_ext_g", 0, 0, 0x8f, 1);
                        if (data.Length != 0 && (data[0] != 0xd4 && data[0] != 0xd7))
                            bad1 = true;

                        comPort.device_op(comPort.MAV.sysid, comPort.MAV.compid, out data,
                            MAVLink.DEVICE_OP_BUSTYPE.SPI,
                            "lsm9ds0_ext_am", 0, 0, 0x8f, 1);
                        if (data.Length != 0 && data[0] != 0x49)
                            bad1 = true;

                        if (bad1)
                            this.BeginInvoke(method: (Action) delegate
                            {
                                MissionPlanner.Controls.SB.Show("SPI Scan");
                            });
                    });
                }

            }
            catch
            {
            }
        }

        private void CMB_serialport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_connectionControl.CMB_serialport.SelectedItem == _connectionControl.CMB_serialport.Text)
                return;

            comPortName = _connectionControl.CMB_serialport.Text;
            if (comPortName == "UDP" || comPortName == "UDPCl" || comPortName == "TCP" || comPortName == "AUTO")
            {
                _connectionControl.CMB_baudrate.Enabled = false;
            }
            else
            {
                _connectionControl.CMB_baudrate.Enabled = true;
            }

            try
            {
                // check for saved baud rate and restore
                if (Settings.Instance[_connectionControl.CMB_serialport.Text.Replace(" ", "_") + "_BAUD"] != null)
                {
                    _connectionControl.CMB_baudrate.Text =
                        Settings.Instance[_connectionControl.CMB_serialport.Text.Replace(" ", "_") + "_BAUD"];
                }
            }
            catch
            {
            }
        }


        /// <summary>
        /// overriding the OnCLosing is a bit cleaner than handling the event, since it
        /// is this object.
        ///
        /// This happens before FormClosed
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            log.Info("MainV2_FormClosing");

            log.Info("GMaps write cache");
            // speed up tile saving on exit
            GMap.NET.GMaps.Instance.CacheOnIdleRead = false;
            GMap.NET.GMaps.Instance.BoostCacheEngine = true;

            Settings.Instance["MainHeight"] = this.Height.ToString();
            Settings.Instance["MainWidth"] = this.Width.ToString();
            Settings.Instance["MainMaximised"] = this.WindowState.ToString();

            Settings.Instance["MainLocX"] = this.Location.X.ToString();
            Settings.Instance["MainLocY"] = this.Location.Y.ToString();

            log.Info("close logs");

            // close bases connection
            try
            {
                comPort.logreadmode = false;
                if (comPort.logfile != null)
                    comPort.logfile.Close();

                if (comPort.rawlogfile != null)
                    comPort.rawlogfile.Close();

                comPort.logfile = null;
                comPort.rawlogfile = null;
            }
            catch
            {
            }

            log.Info("close ports");
            // close all connections
            foreach (var port in Comports)
            {
                try
                {
                    port.logreadmode = false;
                    if (port.logfile != null)
                        port.logfile.Close();

                    if (port.rawlogfile != null)
                        port.rawlogfile.Close();

                    port.logfile = null;
                    port.rawlogfile = null;
                }
                catch
                {
                }
            }

            log.Info("stop adsb");
            Utilities.adsb.Stop();

            log.Info("stop WarningEngine");
            Warnings.WarningEngine.Stop();

            log.Info("stop GStreamer");
            GCSViews.FlightData.hudGStreamer.Stop();

            log.Info("closing vlcrender");
            try
            {
                while (vlcrender.store.Count > 0)
                    vlcrender.store[0].Stop();
            }
            catch
            {
            }

            log.Info("closing pluginthread");

            pluginthreadrun = false;

            if (pluginthread != null)
            {
                try
                {
                    while (!PluginThreadrunner.WaitOne(100)) Application.DoEvents();
                }
                catch
                {
                }

                pluginthread.Join();
            }

            log.Info("closing serialthread");

            serialThread = false;

            log.Info("closing adsbthread");

            adsbThread = false;

            log.Info("closing joystickthread");

            joystickthreadrun = false;

            log.Info("closing httpthread");

            // if we are waiting on a socket we need to force an abort
            httpserver.Stop();

            log.Info("sorting tlogs");
            try
            {
                System.Threading.ThreadPool.QueueUserWorkItem((WaitCallback) delegate
                    {
                        try
                        {
                            MissionPlanner.Log.LogSort.SortLogs(Directory.GetFiles(Settings.Instance.LogDir, "*.tlog"));
                        }
                        catch
                        {
                        }
                    }
                );
            }
            catch
            {
            }

            log.Info("closing MyView");

            // close all tabs
            MyView.Dispose();

            log.Info("closing fd");
            try
            {
                FlightData.Dispose();
            }
            catch
            {
            }

            log.Info("closing fp");
            try
            {
                FlightPlanner.Dispose();
            }
            catch
            {
            }

            log.Info("closing sim");
            try
            {
                Simulation.Dispose();
            }
            catch
            {
            }

            try
            {
                if (comPort.BaseStream.IsOpen)
                    comPort.Close();
            }
            catch
            {
            } // i get alot of these errors, the port is still open, but not valid - user has unpluged usb

            // save config
            SaveConfig();

            Console.WriteLine(httpthread?.IsAlive);
            Console.WriteLine(pluginthread?.IsAlive);

            log.Info("MainV2_FormClosing done");

            if (MONO)
                this.Dispose();
        }


        /// <summary>
        /// this happens after FormClosing...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            Console.WriteLine("MainV2_FormClosed");

            if (joystick != null)
            {
                while (!joysendThreadExited)
                    Thread.Sleep(10);

                joystick.Dispose(); //proper clean up of joystick.
            }
        }

        private void LoadConfig()
        {
            try
            {
                log.Info("Loading config");

                Settings.Instance.Load();

                comPortName = Settings.Instance.ComPort;
            }
            catch (Exception ex)
            {
                log.Error("Bad Config File", ex);
            }
        }

        private void SaveConfig()
        {
            try
            {
                log.Info("Saving config");
                Settings.Instance.ComPort = comPortName;

                if (_connectionControl != null)
                    Settings.Instance.BaudRate = _connectionControl.CMB_baudrate.Text;

                Settings.Instance.APMFirmware = MainV2.comPort.MAV.cs.firmware.ToString();

                Settings.Instance.Save();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// needs to be true by default so that exits properly if no joystick used.
        /// </summary>
        volatile private bool joysendThreadExited = true;

        /// <summary>
        /// thread used to send joystick packets to the MAV
        /// </summary>
        private async void joysticksend()
        {
            float rate = 50; // 1000 / 50 = 20 hz
            int count = 0;

            DateTime lastratechange = DateTime.Now;

            joystickthreadrun = true;

            while (joystickthreadrun)
            {
                joysendThreadExited = false;
                //so we know this thread is stil alive.
                try
                {
                    if (MONO)
                    {
                        log.Error("Mono: closing joystick thread");
                        break;
                    }

                    if (!MONO)
                    {
                        //joystick stuff

                        if (joystick != null && joystick.enabled)
                        {
                            if (!joystick.manual_control)
                            {
                                MAVLink.mavlink_rc_channels_override_t
                                    rc = new MAVLink.mavlink_rc_channels_override_t();

                                rc.target_component = comPort.MAV.compid;
                                rc.target_system = comPort.MAV.sysid;

                                if (joystick.getJoystickAxis(1) == Joystick.joystickaxis.None)
                                    rc.chan1_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(2) == Joystick.joystickaxis.None)
                                    rc.chan2_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(3) == Joystick.joystickaxis.None)
                                    rc.chan3_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(4) == Joystick.joystickaxis.None)
                                    rc.chan4_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(5) == Joystick.joystickaxis.None)
                                    rc.chan5_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(6) == Joystick.joystickaxis.None)
                                    rc.chan6_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(7) == Joystick.joystickaxis.None)
                                    rc.chan7_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(8) == Joystick.joystickaxis.None)
                                    rc.chan8_raw = ushort.MaxValue;
                                if (joystick.getJoystickAxis(9) == Joystick.joystickaxis.None)
                                    rc.chan9_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(10) == Joystick.joystickaxis.None)
                                    rc.chan10_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(11) == Joystick.joystickaxis.None)
                                    rc.chan11_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(12) == Joystick.joystickaxis.None)
                                    rc.chan12_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(13) == Joystick.joystickaxis.None)
                                    rc.chan13_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(14) == Joystick.joystickaxis.None)
                                    rc.chan14_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(15) == Joystick.joystickaxis.None)
                                    rc.chan15_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(16) == Joystick.joystickaxis.None)
                                    rc.chan16_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(17) == Joystick.joystickaxis.None)
                                    rc.chan17_raw = (ushort) 0;
                                if (joystick.getJoystickAxis(18) == Joystick.joystickaxis.None)
                                    rc.chan18_raw = (ushort) 0;

                                if (joystick.getJoystickAxis(1) != Joystick.joystickaxis.None)
                                    rc.chan1_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech1;
                                if (joystick.getJoystickAxis(2) != Joystick.joystickaxis.None)
                                    rc.chan2_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech2;
                                if (joystick.getJoystickAxis(3) != Joystick.joystickaxis.None)
                                    rc.chan3_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech3;
                                if (joystick.getJoystickAxis(4) != Joystick.joystickaxis.None)
                                    rc.chan4_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech4;
                                if (joystick.getJoystickAxis(5) != Joystick.joystickaxis.None)
                                    rc.chan5_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech5;
                                if (joystick.getJoystickAxis(6) != Joystick.joystickaxis.None)
                                    rc.chan6_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech6;
                                if (joystick.getJoystickAxis(7) != Joystick.joystickaxis.None)
                                    rc.chan7_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech7;
                                if (joystick.getJoystickAxis(8) != Joystick.joystickaxis.None)
                                    rc.chan8_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech8;
                                if (joystick.getJoystickAxis(9) != Joystick.joystickaxis.None)
                                    rc.chan9_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech9;
                                if (joystick.getJoystickAxis(10) != Joystick.joystickaxis.None)
                                    rc.chan10_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech10;
                                if (joystick.getJoystickAxis(11) != Joystick.joystickaxis.None)
                                    rc.chan11_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech11;
                                if (joystick.getJoystickAxis(12) != Joystick.joystickaxis.None)
                                    rc.chan12_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech12;
                                if (joystick.getJoystickAxis(13) != Joystick.joystickaxis.None)
                                    rc.chan13_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech13;
                                if (joystick.getJoystickAxis(14) != Joystick.joystickaxis.None)
                                    rc.chan14_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech14;
                                if (joystick.getJoystickAxis(15) != Joystick.joystickaxis.None)
                                    rc.chan15_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech15;
                                if (joystick.getJoystickAxis(16) != Joystick.joystickaxis.None)
                                    rc.chan16_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech16;
                                if (joystick.getJoystickAxis(17) != Joystick.joystickaxis.None)
                                    rc.chan17_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech17;
                                if (joystick.getJoystickAxis(18) != Joystick.joystickaxis.None)
                                    rc.chan18_raw = (ushort) MainV2.comPort.MAV.cs.rcoverridech18;

                                if (lastjoystick.AddMilliseconds(rate) < DateTime.Now)
                                {
                                    /*
                                if (MainV2.comPort.MAV.cs.rssi > 0 && MainV2.comPort.MAV.cs.remrssi > 0)
                                {
                                    if (lastratechange.Second != DateTime.Now.Second)
                                    {
                                        if (MainV2.comPort.MAV.cs.txbuffer > 90)
                                        {
                                            if (rate < 20)
                                                rate = 21;
                                            rate--;

                                            if (MainV2.comPort.MAV.cs.linkqualitygcs < 70)
                                                rate = 50;
                                        }
                                        else
                                        {
                                            if (rate > 100)
                                                rate = 100;
                                            rate++;
                                        }

                                        lastratechange = DateTime.Now;
                                    }

                                }
                                */
                                    //                                Console.WriteLine(DateTime.Now.Millisecond + " {0} {1} {2} {3} {4}", rc.chan1_raw, rc.chan2_raw, rc.chan3_raw, rc.chan4_raw,rate);

                                    //Console.WriteLine("Joystick btw " + comPort.BaseStream.BytesToWrite);

                                    if (!comPort.BaseStream.IsOpen)
                                        continue;

                                    if (comPort.BaseStream.BytesToWrite < 50)
                                    {
                                        if (sitl)
                                        {
                                            MissionPlanner.GCSViews.SITL.rcinput();
                                        }
                                        else
                                        {
                                            comPort.sendPacket(rc, rc.target_system, rc.target_component);
                                        }

                                        count++;
                                        lastjoystick = DateTime.Now;
                                    }
                                }
                            }
                            else
                            {
                                MAVLink.mavlink_manual_control_t rc = new MAVLink.mavlink_manual_control_t();

                                rc.target = comPort.MAV.compid;

                                if (joystick.getJoystickAxis(1) != Joystick.joystickaxis.None)
                                    rc.x = MainV2.comPort.MAV.cs.rcoverridech1;
                                if (joystick.getJoystickAxis(2) != Joystick.joystickaxis.None)
                                    rc.y = MainV2.comPort.MAV.cs.rcoverridech2;
                                if (joystick.getJoystickAxis(3) != Joystick.joystickaxis.None)
                                    rc.z = MainV2.comPort.MAV.cs.rcoverridech3;
                                if (joystick.getJoystickAxis(4) != Joystick.joystickaxis.None)
                                    rc.r = MainV2.comPort.MAV.cs.rcoverridech4;

                                if (lastjoystick.AddMilliseconds(rate) < DateTime.Now)
                                {
                                    if (!comPort.BaseStream.IsOpen)
                                        continue;

                                    if (comPort.BaseStream.BytesToWrite < 50)
                                    {
                                        if (sitl)
                                        {
                                            MissionPlanner.GCSViews.SITL.rcinput();
                                        }
                                        else
                                        {
                                            comPort.sendPacket(rc, comPort.MAV.sysid, comPort.MAV.compid);
                                        }

                                        count++;
                                        lastjoystick = DateTime.Now;
                                    }
                                }
                            }
                        }
                    }

                    await Task.Delay(40).ConfigureAwait(false);
                }
                catch
                {
                } // cant fall out
            }

            joysendThreadExited = true; //so we know this thread exited.
        }

        /// <summary>
        /// Used to fix the icon status for unexpected unplugs etc...
        /// </summary>
        private void UpdateConnectIcon()
        {
            if ((DateTime.UtcNow - connectButtonUpdate).Milliseconds > 500)
            {
                //                        Console.WriteLine(DateTime.Now.Millisecond);
                if (comPort.BaseStream.IsOpen)
                {
                    if (this.MenuConnect.Image == null || (string) this.MenuConnect.Image.Tag != "Disconnect")
                    {
                        this.BeginInvoke((MethodInvoker) delegate
                        {
                            this.MenuConnect.Image = displayicons.disconnect;
                            this.MenuConnect.Image.Tag = "Disconnect";
                            this.MenuConnect.Text = Strings.DISCONNECTc;
                            _connectionControl.IsConnected(true);
                        });
                    }
                }
                else
                {
                    if (this.MenuConnect.Image != null && (string) this.MenuConnect.Image.Tag != "Connect")
                    {
                        this.BeginInvoke((MethodInvoker) delegate
                        {
                            this.MenuConnect.Image = displayicons.connect;
                            this.MenuConnect.Image.Tag = "Connect";
                            this.MenuConnect.Text = Strings.CONNECTc;
                            _connectionControl.IsConnected(false);
                            if (_connectionStats != null)
                            {
                                _connectionStats.StopUpdates();
                            }
                        });
                    }

                    if (comPort.logreadmode)
                    {
                        this.BeginInvoke((MethodInvoker) delegate { _connectionControl.IsConnected(true); });
                    }
                }

                connectButtonUpdate = DateTime.UtcNow;
            }
        }

        ManualResetEvent PluginThreadrunner = new ManualResetEvent(false);

        private void PluginThread()
        {
            Hashtable nextrun = new Hashtable();

            pluginthreadrun = true;

            PluginThreadrunner.Reset();

            while (pluginthreadrun)
            {
                DateTime minnextrun = DateTime.Now.AddMilliseconds(1000);
                try
                {
                    foreach (var plugin in Plugin.PluginLoader.Plugins.ToArray())
                    {
                        if (!nextrun.ContainsKey(plugin))
                            nextrun[plugin] = DateTime.MinValue;

                        if ((DateTime.Now > plugin.NextRun) && (plugin.loopratehz > 0))
                        {
                            // get ms till next run
                            int msnext = (int) (1000 / plugin.loopratehz);

                            // allow the plug to modify this, if needed
                            plugin.NextRun = DateTime.Now.AddMilliseconds(msnext);

                            if (plugin.NextRun < minnextrun)
                                minnextrun = plugin.NextRun;

                            try
                            {
                                bool ans = plugin.Loop();
                            }
                            catch (Exception ex)
                            {
                                log.Error(ex);
                            }
                        }
                    }
                }
                catch
                {
                }

                var sleepms = (int) ((minnextrun - DateTime.Now).TotalMilliseconds);
                // max rate is 100 hz - prevent massive cpu usage
                if (sleepms > 0)
                    System.Threading.Thread.Sleep(sleepms);
            }

            while (Plugin.PluginLoader.Plugins.Count > 0)
            {
                var plugin = Plugin.PluginLoader.Plugins[0];
                try
                {
                    plugin.Exit();
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }

                Plugin.PluginLoader.Plugins.Remove(plugin);
            }

            try
            {
                PluginThreadrunner.Set();
            }
            catch
            {
            }

            return;
        }

        ManualResetEvent SerialThreadrunner = new ManualResetEvent(false);

        /// <summary>
        /// main serial reader thread
        /// controls
        /// serial reading
        /// link quality stats
        /// speech voltage - custom - alt warning - data lost
        /// heartbeat packet sending
        ///
        /// and can't fall out
        /// </summary>
        private async void SerialReader()
        {
            if (serialThread == true)
                return;
            serialThread = true;

            SerialThreadrunner.Reset();

            int minbytes = 10;

            int altwarningmax = 0;

            bool armedstatus = false;

            string lastmessagehigh = "";

            DateTime speechcustomtime = DateTime.Now;

            DateTime speechlowspeedtime = DateTime.Now;

            DateTime linkqualitytime = DateTime.Now;

            while (serialThread)
            {
                try
                {
                    await Task.Delay(1).ConfigureAwait(false); // was 5

                    try
                    {
                        if (ConfigTerminal.comPort is MAVLinkSerialPort)
                        {
                        }
                        else
                        {
                            if (ConfigTerminal.comPort != null && ConfigTerminal.comPort.IsOpen)
                                continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }

                    // update connect/disconnect button and info stats
                    try
                    {
                        UpdateConnectIcon();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }

                    // 30 seconds interval speech options
                    if (speechEnabled() && (DateTime.UtcNow - speechcustomtime).TotalSeconds > 30 &&
                        (MainV2.comPort.logreadmode || comPort.BaseStream.IsOpen))
                    {
                        if (MainV2.speechEngine.IsReady)
                        {
                            if (Settings.Instance.GetBoolean("speechcustomenabled"))
                            {
                                MainV2.speechEngine.SpeakAsync(ArduPilot.Common.speechConversion(comPort.MAV,
                                    "" + Settings.Instance["speechcustom"]));
                            }

                            speechcustomtime = DateTime.UtcNow;
                        }

                        // speech for battery alerts
                        //speechbatteryvolt
                        float warnvolt = Settings.Instance.GetFloat("speechbatteryvolt");
                        float warnpercent = Settings.Instance.GetFloat("speechbatterypercent");

                        if (Settings.Instance.GetBoolean("speechbatteryenabled") == true &&
                            MainV2.comPort.MAV.cs.battery_voltage <= warnvolt &&
                            MainV2.comPort.MAV.cs.battery_voltage >= 5.0)
                        {
                            if (MainV2.speechEngine.IsReady)
                            {
                                MainV2.speechEngine.SpeakAsync(ArduPilot.Common.speechConversion(comPort.MAV,
                                    "" + Settings.Instance["speechbattery"]));
                            }
                        }
                        else if (Settings.Instance.GetBoolean("speechbatteryenabled") == true &&
                                 (MainV2.comPort.MAV.cs.battery_remaining) < warnpercent &&
                                 MainV2.comPort.MAV.cs.battery_voltage >= 5.0 &&
                                 MainV2.comPort.MAV.cs.battery_remaining != 0.0)
                        {
                            if (MainV2.speechEngine.IsReady)
                            {
                                MainV2.speechEngine.SpeakAsync(
                                    ArduPilot.Common.speechConversion(comPort.MAV,
                                        "" + Settings.Instance["speechbattery"]));
                            }
                        }
                    }

                    // speech for airspeed alerts
                    if (speechEnabled() && (DateTime.UtcNow - speechlowspeedtime).TotalSeconds > 10 &&
                        (MainV2.comPort.logreadmode || comPort.BaseStream.IsOpen))
                    {
                        if (Settings.Instance.GetBoolean("speechlowspeedenabled") == true &&
                            MainV2.comPort.MAV.cs.armed)
                        {
                            float warngroundspeed = Settings.Instance.GetFloat("speechlowgroundspeedtrigger");
                            float warnairspeed = Settings.Instance.GetFloat("speechlowairspeedtrigger");

                            if (MainV2.comPort.MAV.cs.airspeed < warnairspeed)
                            {
                                if (MainV2.speechEngine.IsReady)
                                {
                                    MainV2.speechEngine.SpeakAsync(
                                        ArduPilot.Common.speechConversion(comPort.MAV,
                                            "" + Settings.Instance["speechlowairspeed"]));
                                    speechlowspeedtime = DateTime.UtcNow;
                                }
                            }
                            else if (MainV2.comPort.MAV.cs.groundspeed < warngroundspeed)
                            {
                                if (MainV2.speechEngine.IsReady)
                                {
                                    MainV2.speechEngine.SpeakAsync(
                                        ArduPilot.Common.speechConversion(comPort.MAV,
                                            "" + Settings.Instance["speechlowgroundspeed"]));
                                    speechlowspeedtime = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                speechlowspeedtime = DateTime.UtcNow;
                            }
                        }
                    }

                    // speech altitude warning - message high warning
                    if (speechEnabled() &&
                        (MainV2.comPort.logreadmode || comPort.BaseStream.IsOpen))
                    {
                        float warnalt = float.MaxValue;
                        if (Settings.Instance.ContainsKey("speechaltheight"))
                        {
                            warnalt = Settings.Instance.GetFloat("speechaltheight");
                        }

                        try
                        {
                            altwarningmax = (int) Math.Max(MainV2.comPort.MAV.cs.alt, altwarningmax);

                            if (Settings.Instance.GetBoolean("speechaltenabled") == true &&
                                MainV2.comPort.MAV.cs.alt != 0.00 &&
                                (MainV2.comPort.MAV.cs.alt <= warnalt) && MainV2.comPort.MAV.cs.armed)
                            {
                                if (altwarningmax > warnalt)
                                {
                                    if (MainV2.speechEngine.IsReady)
                                        MainV2.speechEngine.SpeakAsync(
                                            ArduPilot.Common.speechConversion(comPort.MAV,
                                                "" + Settings.Instance["speechalt"]));
                                }
                            }
                        }
                        catch
                        {
                        } // silent fail


                        try
                        {
                            // say the latest high priority message
                            if (MainV2.speechEngine.IsReady &&
                                lastmessagehigh != MainV2.comPort.MAV.cs.messageHigh &&
                                MainV2.comPort.MAV.cs.messageHigh != null)
                            {
                                if (!MainV2.comPort.MAV.cs.messageHigh.StartsWith("PX4v2 ") &&
                                    !MainV2.comPort.MAV.cs.messageHigh.StartsWith("PreArm:")) // Supress audibly repeating PreArm messages
                                {
                                    MainV2.speechEngine.SpeakAsync(MainV2.comPort.MAV.cs.messageHigh);
                                    lastmessagehigh = MainV2.comPort.MAV.cs.messageHigh;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    // not doing anything
                    if (!MainV2.comPort.logreadmode && !comPort.BaseStream.IsOpen)
                    {
                        altwarningmax = 0;
                    }

                    // attenuate the link qualty over time
                    if ((DateTime.UtcNow - MainV2.comPort.MAV.lastvalidpacket).TotalSeconds >= 1)
                    {
                        if (linkqualitytime.Second != DateTime.UtcNow.Second)
                        {
                            MainV2.comPort.MAV.cs.linkqualitygcs =
                                (ushort) (MainV2.comPort.MAV.cs.linkqualitygcs * 0.8f);
                            linkqualitytime = DateTime.UtcNow;

                            // force redraw if there are no other packets are being read
                            this.BeginInvokeIfRequired(
                                (Action)
                                delegate { GCSViews.FlightData.myhud.Invalidate(); });
                        }
                    }

                    // data loss warning - wait min of 3 seconds, ignore first 30 seconds of connect, repeat at 5 seconds interval
                    if ((DateTime.UtcNow - MainV2.comPort.MAV.lastvalidpacket).TotalSeconds > 3
                        && (DateTime.UtcNow - connecttime).TotalSeconds > 30
                        && (DateTime.UtcNow - nodatawarning).TotalSeconds > 5
                        && (MainV2.comPort.logreadmode || comPort.BaseStream.IsOpen)
                        && MainV2.comPort.MAV.cs.armed)
                    {
                        var msg = "WARNING No Data for " + (int)(DateTime.UtcNow - MainV2.comPort.MAV.lastvalidpacket).TotalSeconds + " Seconds";
                        MainV2.comPort.MAV.cs.messageHigh = msg;
                        if (speechEnabled())
                        {
                            if (MainV2.speechEngine.IsReady)
                            {
                                MainV2.speechEngine.SpeakAsync(msg);
                                nodatawarning = DateTime.UtcNow;
                            }
                        }
                    }

                    // get home point on armed status change.
                    if (armedstatus != MainV2.comPort.MAV.cs.armed && comPort.BaseStream.IsOpen)
                    {
                        armedstatus = MainV2.comPort.MAV.cs.armed;
                        // status just changed to armed
                        if (MainV2.comPort.MAV.cs.armed == true &&
                            MainV2.comPort.MAV.apname != MAVLink.MAV_AUTOPILOT.INVALID &&
                            MainV2.comPort.MAV.aptype != MAVLink.MAV_TYPE.GIMBAL)
                        {
                            System.Threading.ThreadPool.QueueUserWorkItem(state =>
                            {
                                Thread.CurrentThread.Name = "Arm State change";
                                try
                                {
                                    while (comPort.giveComport == true)
                                        Thread.Sleep(100);

                                    MainV2.comPort.MAV.cs.HomeLocation = new PointLatLngAlt(MainV2.comPort.getWP(0));
                                    if (MyView.current != null && MyView.current.Name == "FlightPlanner")
                                    {
                                        // update home if we are on flight data tab
                                        this.BeginInvokeIfRequired((Action) delegate { FlightPlanner.updateHome(); });
                                    }
                                }
                                catch
                                {
                                    // dont hang this loop
                                    this.BeginInvokeIfRequired(
                                        (Action)
                                        delegate
                                        {
                                            CustomMessageBox.Show("Failed to update home location (" +
                                                                  MainV2.comPort.MAV.sysid + ")");
                                        });
                                }
                            });
                        }

                        if (speechEnable && speechEngine != null)
                        {
                            if (Settings.Instance.GetBoolean("speecharmenabled"))
                            {
                                string speech = armedstatus
                                    ? Settings.Instance["speecharm"]
                                    : Settings.Instance["speechdisarm"];
                                if (!string.IsNullOrEmpty(speech))
                                {
                                    MainV2.speechEngine.SpeakAsync(
                                        ArduPilot.Common.speechConversion(comPort.MAV, speech));
                                }
                            }
                        }
                    }

                    if (comPort.MAV.param.TotalReceived < comPort.MAV.param.TotalReported)
                    {
                        if (comPort.MAV.param.TotalReported > 0 && comPort.BaseStream.IsOpen)
                        {
                            this.BeginInvokeIfRequired(() =>
                            {
                                try
                                {
                                    instance.status1.Percent =
                                        (comPort.MAV.param.TotalReceived / (double) comPort.MAV.param.TotalReported) *
                                        100.0;
                                }
                                catch (Exception e)
                                {
                                    log.Error(e);
                                }
                            });
                        }
                    }

                    // send a hb every seconds from gcs to ap
                    if (heatbeatSend.Second != DateTime.UtcNow.Second)
                    {
                        MAVLink.mavlink_heartbeat_t htb = new MAVLink.mavlink_heartbeat_t()
                        {
                            type = (byte) MAVLink.MAV_TYPE.GCS,
                            autopilot = (byte) MAVLink.MAV_AUTOPILOT.INVALID,
                            mavlink_version = 3 // MAVLink.MAVLINK_VERSION
                        };

                        // enumerate each link
                        foreach (var port in Comports.ToArray())
                        {
                            if (port == null || port.BaseStream == null || !port.BaseStream.IsOpen)
                                continue;

                            // poll for params at heartbeat interval - primary mav on this port only
                            if (!port.giveComport)
                            {
                                try
                                {
                                    // poll only when not armed
                                    if (!port.MAV.cs.armed && DateTime.UtcNow > connecttime.AddSeconds(60))
                                    {
                                        port.getParamPoll();
                                        port.getParamPoll();
                                    }
                                }
                                catch
                                {
                                }
                            }

                            // there are 3 hb types we can send, mavlink1, mavlink2 signed and unsigned
                            bool sentsigned = false;
                            bool sentmavlink1 = false;
                            bool sentmavlink2 = false;

                            // enumerate each mav
                            foreach (var MAV in port.MAVlist)
                            {
                                try
                                {
                                    // poll for version if we dont have it - every mav every port
                                    if (!port.giveComport && MAV.cs.capabilities == 0 &&
                                        (DateTime.Now.Second % 20) == 0 && MAV.cs.version < new Version(0, 1))
                                        port.getVersion(MAV.sysid, MAV.compid, false);

                                    // are we talking to a mavlink2 device
                                    if (MAV.mavlinkv2)
                                    {
                                        // is signing enabled
                                        if (MAV.signing)
                                        {
                                            // check if we have already sent
                                            if (sentsigned)
                                                continue;
                                            sentsigned = true;
                                        }
                                        else
                                        {
                                            // check if we have already sent
                                            if (sentmavlink2)
                                                continue;
                                            sentmavlink2 = true;
                                        }
                                    }
                                    else
                                    {
                                        // check if we have already sent
                                        if (sentmavlink1)
                                            continue;
                                        sentmavlink1 = true;
                                    }

                                    port.sendPacket(htb, MAV.sysid, MAV.compid);
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex);
                                    // close the bad port
                                    try
                                    {
                                        port.Close();
                                    }
                                    catch
                                    {
                                    }

                                    // refresh the screen if needed
                                    if (port == MainV2.comPort)
                                    {
                                        // refresh config window if needed
                                        if (MyView.current != null)
                                        {
                                            this.BeginInvoke((MethodInvoker) delegate()
                                            {
                                                if (MyView.current.Name == "HWConfig")
                                                    MyView.ShowScreen("HWConfig");
                                                if (MyView.current.Name == "SWConfig")
                                                    MyView.ShowScreen("SWConfig");
                                            });
                                        }
                                    }
                                }
                            }
                        }

                        heatbeatSend = DateTime.UtcNow;
                    }

                    // if not connected or busy, sleep and loop
                    if (comPort == null || comPort.BaseStream == null || !comPort.BaseStream.IsOpen || comPort.giveComport == true)
                    {
                        if (!comPort.BaseStream.IsOpen)
                        {
                            // check if other ports are still open
                            foreach (var port in Comports)
                            {
                                if (port != null && port.BaseStream != null && port.BaseStream.IsOpen)
                                {
                                    Console.WriteLine("Main comport shut, swapping to other mav");
                                    comPort = port;
                                    break;
                                }
                            }
                        }

                        await Task.Delay(100).ConfigureAwait(false);
                    }

                    // read the interfaces
                    foreach (var port in Comports.ToArray())
                    {
                         if (port == null || port.BaseStream == null)
                        {
                            continue; // 如果端口或流无效，则跳过本次循环
                        }
                        if (!port.BaseStream.IsOpen)
                        {
                            // skip primary interface
                            if (port == comPort)
                                continue;

                            // modify array and drop out
                            Comports.Remove(port);
                            port.Dispose();
                            break;
                        }

                        DateTime startread = DateTime.UtcNow;

                        // must be open, we have bytes, we are not yielding the port,
                        // the thread is meant to be running and we only spend 1 seconds max in this read loop
                        while (port.BaseStream.IsOpen && port.BaseStream.BytesToRead > minbytes &&
                               port.giveComport == false && serialThread && startread.AddSeconds(1) > DateTime.UtcNow)
                        {
                            try
                            {
                                await port.readPacketAsync().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                log.Error(ex);
                            }
                        }

                        // update currentstate of sysids on the port
                        foreach (var MAV in port.MAVlist)
                        {
                            try
                            {
                                MAV.cs.UpdateCurrentSettings(null, false, port, MAV);
                            }
                            catch (Exception ex)
                            {
                                log.Error(ex);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Tracking.AddException(e);
                    log.Error("Serial Reader fail :" + e.ToString());
                    try
                    {
                        comPort.Close();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
            }

            Console.WriteLine("SerialReader Done");
            SerialThreadrunner.Set();
        }

        ManualResetEvent ADSBThreadRunner = new ManualResetEvent(false);

        /// <summary>
        /// adsb periodic send thread
        /// </summary>
        private async void ADSBRunner()
        {
            if (adsbThread)
                return;
            adsbThread = true;
            ADSBThreadRunner.Reset();
            while (adsbThread)
            {
                await Task.Delay(1000).ConfigureAwait(false); // run every 1000 ms
                // Clean up old planes
                HashSet<string> planesToClean = new HashSet<string>();
                lock(adsblock)
                {
                    MainV2.instance.adsbPlanes.Where(a => a.Value.Time < DateTime.Now.AddSeconds(-30)).ForEach(a => planesToClean.Add(a.Key));
                    planesToClean.ForEach(a => MainV2.instance.adsbPlanes.TryRemove(a, out _));

                }
                PointLatLngAlt ourLocation = comPort.MAV.cs.Location;
                // Get only close planes, sorted by distance
                var relevantPlanes = MainV2.instance.adsbPlanes
                    .Select(v => new { v, Distance = v.Value.GetDistance(ourLocation) })
                    .Where(v => v.Distance <= 10000)
                    .Where(v => !(v.v.Value.Source is MAVLinkInterface))
                    .OrderBy(v => v.Distance)
                    .Select(v => v.v.Value)
                    .Take(10)
                    .ToList();
                adsbIndex = (++adsbIndex % Math.Max(1, Math.Min(relevantPlanes.Count, 10)));
                var currentPlane = relevantPlanes.ElementAtOrDefault(adsbIndex);
                if (currentPlane == null)
                {
                    continue;
                }
                MAVLink.mavlink_adsb_vehicle_t packet = new MAVLink.mavlink_adsb_vehicle_t();
                packet.altitude = (int)(currentPlane.Alt * 1000);
                packet.altitude_type = (byte)MAVLink.ADSB_ALTITUDE_TYPE.GEOMETRIC;
                packet.callsign = currentPlane.CallSign.MakeBytes();
                packet.squawk = currentPlane.Squawk;
                packet.emitter_type = (byte)MAVLink.ADSB_EMITTER_TYPE.NO_INFO;
                packet.heading = (ushort)(currentPlane.Heading * 100);
                packet.lat = (int)(currentPlane.Lat * 1e7);
                packet.lon = (int)(currentPlane.Lng * 1e7);
                packet.hor_velocity = (ushort)(currentPlane.Speed);
                packet.ver_velocity = (short)(currentPlane.VerticalSpeed);
                try
                {
                    packet.ICAO_address = uint.Parse(currentPlane.Tag, NumberStyles.HexNumber);
                }
                catch
                {
                    log.WarnFormat("invalid icao address: {0}", currentPlane.Tag);
                    packet.ICAO_address = 0;
                }
                packet.flags = (ushort)(MAVLink.ADSB_FLAGS.VALID_ALTITUDE | MAVLink.ADSB_FLAGS.VALID_COORDS |
                                          MAVLink.ADSB_FLAGS.VALID_VELOCITY | MAVLink.ADSB_FLAGS.VALID_HEADING | MAVLink.ADSB_FLAGS.VALID_CALLSIGN);

                //send to current connected
                MainV2.comPort.sendPacket(packet, MainV2.comPort.MAV.sysid, MainV2.comPort.MAV.compid);

            }

        }


        protected override void OnLoad(EventArgs e)
        {
            // check if its defined, and force to show it if not known about
            if (Settings.Instance["menu_autohide"] == null)
            {
                Settings.Instance["menu_autohide"] = "false";
            }

            try
            {
                AutoHideMenu(Settings.Instance.GetBoolean("menu_autohide"));
            }
            catch
            {
            }

            MyView.AddScreen(new MainSwitcher.Screen("FlightData", FlightData, true));
            MyView.AddScreen(new MainSwitcher.Screen("FlightPlanner", FlightPlanner, true));
            MyView.AddScreen(new MainSwitcher.Screen("HWConfig", typeof(GCSViews.InitialSetup), false));
            MyView.AddScreen(new MainSwitcher.Screen("SWConfig", typeof(GCSViews.SoftwareConfig), false));
            MyView.AddScreen(new MainSwitcher.Screen("Simulation", Simulation, true));
            MyView.AddScreen(new MainSwitcher.Screen("Help", typeof(GCSViews.Help), false));

            try
            {
                if (Control.ModifierKeys == Keys.Shift)
                {
                }
                else
                {
                    log.Info("Load Pluggins");
                    Plugin.PluginLoader.DisabledPluginNames.Clear();
                    foreach (var s in Settings.Instance.GetList("DisabledPlugins"))
                        Plugin.PluginLoader.DisabledPluginNames.Add(s);
                    Plugin.PluginLoader.LoadAll();
                    log.Info("Load Pluggins... Done");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            if (Program.Logo != null && Program.name == "VVVVZ")
            {
                this.PerformLayout();
                MenuFlightPlanner_Click(this, e);
                MainMenu_ItemClicked(this, new ToolStripItemClickedEventArgs(MenuFlightPlanner));
            }
            else
            {
                this.PerformLayout();
                log.Info("show FlightData");
                MenuFlightData_Click(this, e);
                log.Info("show FlightData... Done");
                MainMenu_ItemClicked(this, new ToolStripItemClickedEventArgs(MenuFlightData));
            }

            // for long running tasks using own threads.
            // for short use threadpool

            this.SuspendLayout();

            // setup http server
            try
            {
                log.Info("start http");
                httpthread = new Thread(new httpserver().listernforclients)
                {
                    Name = "motion jpg stream-network kml",
                    IsBackground = true
                };
                httpthread.Start();
            }
            catch (Exception ex)
            {
                log.Error("Error starting TCP listener thread: ", ex);
                CustomMessageBox.Show(ex.ToString());
            }

            log.Info("start joystick");
            try
            {
                // setup joystick packet sender
                joysticksend();
            }
            catch (NotSupportedException ex)
            {
                log.Error(ex);
            }

            log.Info("start serialreader");
            try
            {
                // setup main serial reader
                SerialReader();
            }
            catch (NotSupportedException ex)
            {
                log.Error(ex);
            }

            log.Info("start adsbsender");
            try
            {
                ADSBRunner();
            }
            catch (NotSupportedException ex)
            {
                log.Error(ex);
            }

            log.Info("start plugin thread");
            try
            {
                // setup main plugin thread
                pluginthread = new Thread(PluginThread)
                {
                    IsBackground = true,
                    Name = "plugin runner thread",
                    Priority = ThreadPriority.BelowNormal
                };
                pluginthread.Start();
            }
            catch (NotSupportedException ex)
            {
                log.Error(ex);
            }


            ThreadPool.QueueUserWorkItem(LoadGDALImages);

            ThreadPool.QueueUserWorkItem(BGLoadAirports);

            ThreadPool.QueueUserWorkItem(BGCreateMaps);

            //ThreadPool.QueueUserWorkItem(BGGetAlmanac);

            ThreadPool.QueueUserWorkItem(BGLogMessagesMetaData);

            // tfr went dead on 30-9-2020
            //ThreadPool.QueueUserWorkItem(BGgetTFR);

            ThreadPool.QueueUserWorkItem(BGNoFly);

            ThreadPool.QueueUserWorkItem(BGGetKIndex);

            // update firmware version list - only once per day
            ThreadPool.QueueUserWorkItem(BGFirmwareCheck);

            Task.Run(async () =>
            {
                try
                {
                    await UserAlert.GetAlerts().ConfigureAwait(false);
                }
                catch
                {
                }
            });

            log.Info("start AutoConnect");
            AutoConnect.NewMavlinkConnection += (sender, serial) =>
            {
                try
                {
                    log.Info("AutoConnect.NewMavlinkConnection " + serial.PortName);
                    MainV2.instance.BeginInvoke((Action) delegate
                    {
                        if (MainV2.comPort.BaseStream.IsOpen)
                        {
                            var mav = new MAVLinkInterface();
                            mav.BaseStream = serial;
                            MainV2.instance.doConnect(mav, "preset", serial.PortName);

                            MainV2.Comports.Add(mav);

                            try
                            {
                                Comports = Comports.Distinct().ToList();
                            }
                            catch { }
                        }
                        else
                        {
                            MainV2.comPort.BaseStream = serial;
                            MainV2.instance.doConnect(MainV2.comPort, "preset", serial.PortName);
                        }
                    });
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            };
            AutoConnect.NewVideoStream += (sender, gststring) =>
            {
                MainV2.instance.BeginInvoke((Action) delegate
                {
                    try
                    {
                        log.Info("AutoConnect.NewVideoStream " + gststring);
                        GStreamer.GstLaunch = GStreamer.LookForGstreamer();

                        if (!GStreamer.GstLaunchExists)
                        {
                            if (CustomMessageBox.Show(
                                    "A video stream has been detected, but gstreamer has not been configured/installed.\nDo you want to install/config it now?",
                                    "GStreamer", System.Windows.Forms.MessageBoxButtons.YesNo) ==
                                (int) System.Windows.Forms.DialogResult.Yes)
                            {
                                GStreamerUI.DownloadGStreamer();
                                if (!GStreamer.GstLaunchExists)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }

                        GCSViews.FlightData.hudGStreamer.Start(gststring);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                });
            };
            AutoConnect.Start();

            BinaryLog.onFlightMode += (firmware, modeno) =>
            {
                try
                {
                    if (firmware == "")
                        return null;

                    var modes = ArduPilot.Common.getModesList((Firmwares) Enum.Parse(typeof(Firmwares), firmware));
                    string currentmode = null;

                    foreach (var mode in modes)
                    {
                        if (mode.Key == modeno)
                        {
                            currentmode = mode.Value;
                            break;
                        }
                    }

                    return currentmode;
                }
                catch
                {
                    return null;
                }
            };

            GCSViews.FlightData.hudGStreamer.OnNewImage += (sender, image) =>
            {
                try
                {
                    if (image == null)
                    {
                        GCSViews.FlightData.myhud.bgimage = null;
                        return;
                    }

                    var old = GCSViews.FlightData.myhud.bgimage;
                    GCSViews.FlightData.myhud.bgimage = new Bitmap(image.Width, image.Height, 4 * image.Width,
                        PixelFormat.Format32bppPArgb,
                        image.LockBits(Rectangle.Empty, null, SKColorType.Bgra8888)
                            .Scan0);
                    if (old != null)
                        old.Dispose();
                }
                catch
                {
                }
            };

            vlcrender.onNewImage += (sender, image) =>
            {
                try
                {
                    if (image == null)
                    {
                        GCSViews.FlightData.myhud.bgimage = null;
                        return;
                    }

                    var old = GCSViews.FlightData.myhud.bgimage;
                    GCSViews.FlightData.myhud.bgimage = new Bitmap(image.Width,
                        image.Height,
                        4 * image.Width,
                        PixelFormat.Format32bppPArgb,
                        image.LockBits(Rectangle.Empty, null, SKColorType.Bgra8888).Scan0);
                    if (old != null)
                        old.Dispose();
                }
                catch
                {
                }
            };

            CaptureMJPEG.onNewImage += (sender, image) =>
            {
                try
                {
                    if (image == null)
                    {
                        GCSViews.FlightData.myhud.bgimage = null;
                        return;
                    }

                    var old = GCSViews.FlightData.myhud.bgimage;
                    GCSViews.FlightData.myhud.bgimage = new Bitmap(image.Width, image.Height, 4 * image.Width,
                        PixelFormat.Format32bppPArgb,
                        image.LockBits(Rectangle.Empty, null, SKColorType.Bgra8888).Scan0);
                    if (old != null)
                        old.Dispose();
                }
                catch
                {
                }
            };

            try
            {
                object locker = new object();
                List<string> seen = new List<string>();

                ZeroConf.StartUDPMavlink += (zeroconfHost) =>
                {
                    try
                    {
                        var ip = zeroconfHost.IPAddress;
                        var service = zeroconfHost.Services.Where(a => a.Key == "_mavlink._udp.local.");
                        var port = service.First().Value.Port;

                        lock (locker)
                        {
                            if (Comports.Any((a) =>
                                {
                                    return a.BaseStream.PortName == "UDPCl" + port.ToString() && a.BaseStream.IsOpen;
                                }
                            ))
                                return;

                            if (seen.Contains(zeroconfHost.Id))
                                return;

                            // no duplicates
                            if (!ExtraConnectionList.Any(a => a.Label == "ZeroConf " + zeroconfHost.DisplayName))
                                ExtraConnectionList.Add(new AutoConnect.ConnectionInfo("ZeroConf " + zeroconfHost.DisplayName, false, port, AutoConnect.ProtocolType.Udp, AutoConnect.ConnectionFormat.MAVLink, AutoConnect.Direction.Outbound, ip));

                            if (CustomMessageBox.Show(
                                    "A Mavlink stream has been detected, " + zeroconfHost.DisplayName + "(" +
                                    zeroconfHost.Id + "). Would you like to connect to it?",
                                    "Mavlink", System.Windows.Forms.MessageBoxButtons.YesNo) ==
                                (int) System.Windows.Forms.DialogResult.Yes)
                            {
                                var mav = new MAVLinkInterface();

                                if(!comPort.BaseStream.IsOpen)
                                    mav = comPort;

                                var udc = new UdpSerialConnect();
                                udc.Port = port.ToString();
                                udc.client = new UdpClient(ip, port);
                                udc.IsOpen = true;
                                udc.hostEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                                mav.BaseStream = udc;

                                MainV2.instance.Invoke((Action) delegate
                                {
                                    MainV2.instance.doConnect(mav, "preset", port.ToString());

                                    MainV2.Comports.Add(mav);

                                    try
                                    {
                                        Comports = Comports.Distinct().ToList();
                                    }
                                    catch { }

                                    MainV2._connectionControl.UpdateSysIDS();
                                });

                            }

                            // add to seen list, so we skip on next refresh
                            seen.Add(zeroconfHost.Id);
                        }
                    }
                    catch (Exception)
                    {

                    }
                };

                if (!isHerelink)
                {
                    ZeroConf.ProbeForMavlink();

                    ZeroConf.ProbeForRTSP();
                }
            }
            catch
            {
            }

            CommsSerialScan.doConnect += port =>
            {
                if (MainV2.instance.InvokeRequired)
                {
                    log.Info("CommsSerialScan.doConnect invoke");
                    MainV2.instance.BeginInvoke(
                        (Action) delegate()
                        {
                            MAVLinkInterface mav = new MAVLinkInterface();
                            mav.BaseStream = port;
                            MainV2.instance.doConnect(mav, "preset", "0");
                            MainV2.Comports.Add(mav);

                            try
                            {
                                Comports = Comports.Distinct().ToList();
                            }
                            catch { }
                        });
                }
                else
                {

                    log.Info("CommsSerialScan.doConnect NO invoke");
                    MAVLinkInterface mav = new MAVLinkInterface();
                    mav.BaseStream = port;
                    MainV2.instance.doConnect(mav, "preset", "0");
                    MainV2.Comports.Add(mav);

                    try
                    {
                        Comports = Comports.Distinct().ToList();
                    }
                    catch { }
                }
            };

            try
            {
                // prescan
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    MissionPlanner.Comms.CommsBLE.SerialPort_GetCustomPorts();
            }
            catch { }

            // add the custom port creator
            CustomPortList.Add(new Regex("BLE_.*"), (s1, s2) => { return new CommsBLE() { PortName = s1, BaudRate = int.Parse(s2) }; });

            this.ResumeLayout();

            Program.Splash?.Close();

            log.Info("appload time");
            MissionPlanner.Utilities.Tracking.AddTiming("AppLoad", "Load Time",
                (DateTime.Now - Program.starttime).TotalMilliseconds, "");

            int p = (int) Environment.OSVersion.Platform;
            bool isWin = (p != 4) && (p != 6) && (p != 128);
            bool winXp = isWin && Environment.OSVersion.Version.Major == 5;
            if (winXp)
            {
                Common.MessageShowAgain("Windows XP",
                    "This is the last version that will support Windows XP, please update your OS");

                // invalidate update url
                System.Configuration.ConfigurationManager.AppSettings["UpdateLocationVersion"] =
                    "https://firmware.ardupilot.org/MissionPlanner/xp/";
                System.Configuration.ConfigurationManager.AppSettings["UpdateLocation"] =
                    "https://firmware.ardupilot.org/MissionPlanner/xp/";
                System.Configuration.ConfigurationManager.AppSettings["UpdateLocationMD5"] =
                    "https://firmware.ardupilot.org/MissionPlanner/xp/checksums.txt";
                System.Configuration.ConfigurationManager.AppSettings["BetaUpdateLocationVersion"] = "";
            }

            try
            {
                // single update check per day - in a seperate thread
                if (Settings.Instance["update_check"] != DateTime.Now.ToShortDateString())
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(checkupdate);
                    Settings.Instance["update_check"] = DateTime.Now.ToShortDateString();
                }
                else if (Settings.Instance.GetBoolean("beta_updates") == true)
                {
                    MissionPlanner.Utilities.Update.dobeta = true;
                    System.Threading.ThreadPool.QueueUserWorkItem(checkupdate);
                }
            }
            catch (Exception ex)
            {
                log.Error("Update check failed", ex);
            }

            // play a tlog that was passed to the program/ load a bin log passed
            if (Program.args.Length > 0)
            {
                var cmds = ProcessCommandLine(Program.args);

                if (cmds.ContainsKey("file") && File.Exists(cmds["file"]) && cmds["file"].ToLower().EndsWith(".tlog"))
                {
                    FlightData.LoadLogFile(Program.args[0]);
                    FlightData.BUT_playlog_Click(null, null);
                }
                else if (cmds.ContainsKey("file") && File.Exists(cmds["file"]) &&
                         (cmds["file"].ToLower().EndsWith(".log") || cmds["file"].ToLower().EndsWith(".bin")))
                {
                    LogBrowse logbrowse = new LogBrowse();
                    ThemeManager.ApplyThemeTo(logbrowse);
                    logbrowse.logfilename = Program.args[0];
                    logbrowse.Show(this);
                    logbrowse.BringToFront();
                }

                if (cmds.ContainsKey("script") && File.Exists(cmds["script"]))
                {
                    // invoke for after onload finished
                    this.BeginInvoke((Action) delegate()
                    {
                        try
                        {
                            FlightData.selectedscript = cmds["script"];

                            FlightData.BUT_run_script_Click(null, null);
                        }
                        catch (Exception ex)
                        {
                            CustomMessageBox.Show("Start script failed: " + ex.ToString(), Strings.ERROR);
                        }
                    });
                }

                if (cmds.ContainsKey("joy") && cmds.ContainsKey("type"))
                {
                    if (cmds["type"].ToLower() == "plane")
                    {
                        MainV2.comPort.MAV.cs.firmware = Firmwares.ArduPlane;
                    }
                    else if (cmds["type"].ToLower() == "copter")
                    {
                        MainV2.comPort.MAV.cs.firmware = Firmwares.ArduCopter2;
                    }
                    else if (cmds["type"].ToLower() == "rover")
                    {
                        MainV2.comPort.MAV.cs.firmware = Firmwares.ArduRover;
                    }
                    else if (cmds["type"].ToLower() == "sub")
                    {
                        MainV2.comPort.MAV.cs.firmware = Firmwares.ArduSub;
                    }

                    var joy = JoystickBase.Create(() => MainV2.comPort);

                    if (joy.start(cmds["joy"]))
                    {
                        MainV2.joystick = joy;
                        MainV2.joystick.enabled = true;
                    }
                    else
                    {
                        CustomMessageBox.Show("Failed to start joystick");
                    }
                }

                if (cmds.ContainsKey("rtk"))
                {
                    var inject = new ConfigSerialInjectGPS();
                    if (cmds["rtk"].ToLower().Contains("http"))
                    {
                        inject.CMB_serialport.Text = "NTRIP";
                        var nt = new CommsNTRIP();
                        ConfigSerialInjectGPS.comPort = nt;
                        Task.Run(() =>
                        {
                            try
                            {
                                nt.Open(cmds["rtk"]);
                                nt.lat = MainV2.comPort.MAV.cs.PlannedHomeLocation.Lat;
                                nt.lng = MainV2.comPort.MAV.cs.PlannedHomeLocation.Lng;
                                nt.alt = MainV2.comPort.MAV.cs.PlannedHomeLocation.Alt;
                                this.BeginInvokeIfRequired(() => { inject.DoConnect().RunSynchronously(); });
                            }
                            catch (Exception ex)
                            {
                                this.BeginInvokeIfRequired(() => { CustomMessageBox.Show(ex.ToString()); });
                            }
                        });
                    }
                }

                if (cmds.ContainsKey("cam"))
                {
                    try
                    {
                        MainV2.cam = new WebCamService.Capture(int.Parse(cmds["cam"]), null);

                        MainV2.cam.Start();
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show(ex.ToString());
                    }
                }

                if (cmds.ContainsKey("gstream"))
                {
                    GStreamer.GstLaunch = GStreamer.LookForGstreamer();

                    if (!GStreamer.GstLaunchExists)
                    {
                        if (CustomMessageBox.Show(
                                "A video stream has been detected, but gstreamer has not been configured/installed.\nDo you want to install/config it now?",
                                "GStreamer", System.Windows.Forms.MessageBoxButtons.YesNo) ==
                            (int) System.Windows.Forms.DialogResult.Yes)
                        {
                            GStreamerUI.DownloadGStreamer();
                        }
                    }

                    try
                    {
                        new Thread(delegate()
                            {
                                // 36 retrys
                                for (int i = 0; i < 36; i++)
                                {
                                    try
                                    {
                                        var st = GCSViews.FlightData.hudGStreamer.Start(cmds["gstream"]);
                                        if (st == null)
                                        {
                                            // prevent spam
                                            Thread.Sleep(5000);
                                        }
                                        else
                                        {
                                            while (st.IsAlive)
                                            {
                                                Thread.Sleep(1000);
                                            }
                                        }
                                    }
                                    catch (BadImageFormatException ex)
                                    {
                                        // not running on x64
                                        log.Error(ex);
                                        return;
                                    }
                                    catch (DllNotFoundException ex)
                                    {
                                        // missing or failed download
                                        log.Error(ex);
                                        return;
                                    }
                                }
                            })
                            {IsBackground = true, Name = "Gstreamer cli"}.Start();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }

                if (cmds.ContainsKey("port") && cmds.ContainsKey("baud"))
                {
                    _connectionControl.CMB_serialport.Text = cmds["port"];
                    _connectionControl.CMB_baudrate.Text = cmds["baud"];

                    doConnect(MainV2.comPort, cmds["port"], cmds["baud"]);
                }
            }

            GMapMarkerBase.length = Settings.Instance.GetInt32("GMapMarkerBase_length", 500);
            GMapMarkerBase.DisplayCOGSetting = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayCOG", true);
            GMapMarkerBase.DisplayHeadingSetting = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayHeading", true);
            GMapMarkerBase.DisplayNavBearingSetting = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayNavBearing", true);
            GMapMarkerBase.DisplayRadiusSetting = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayRadius", true);
            GMapMarkerBase.DisplayTargetSetting = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayTarget", true);
            var inactiveDisplayStyle = GMapMarkerBase.InactiveDisplayStyleEnum.Normal;
            string inactiveDisplayStyleStr = Settings.Instance.GetString("GMapMarkerBase_InactiveDisplayStyle", inactiveDisplayStyle.ToString());
            Enum.TryParse(inactiveDisplayStyleStr, out inactiveDisplayStyle);
            GMapMarkerBase.InactiveDisplayStyle = inactiveDisplayStyle;
            Settings.Instance["GMapMarkerBase_InactiveDisplayStyle"] = inactiveDisplayStyle.ToString();
        }

        private void BGLogMessagesMetaData(object nothing)
        {
            LogMetaData.GetMetaData().ConfigureAwait(false).GetAwaiter().GetResult();
            LogMetaData.ParseMetaData();
        }

        public void LoadGDALImages(object nothing)
        {
            if (Settings.Instance.ContainsKey("GDALImageDir"))
            {
                try
                {
                    Utilities.GDAL.ScanDirectory(Settings.Instance["GDALImageDir"]);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }

        private Dictionary<string, string> ProcessCommandLine(string[] args)
        {
            Dictionary<string, string> cmdargs = new Dictionary<string, string>();
            string cmd = "";
            foreach (var s in args)
            {
                if (s.StartsWith("-") || s.StartsWith("/") || s.StartsWith("--"))
                {
                    cmd = s.TrimStart(new char[] {'-', '/', '-'}).TrimStart(new char[] {'-', '/', '-'});
                    continue;
                }

                if (cmd != "")
                {
                    cmdargs[cmd] = s;
                    log.Info("ProcessCommandLine: " + cmd + " = " + s);
                    cmd = "";
                    continue;
                }

                if (File.Exists(s))
                {
                    // we are not a command, and the file exists.
                    cmdargs["file"] = s;
                    log.Info("ProcessCommandLine: " + "file" + " = " + s);
                    continue;
                }

                log.Info("ProcessCommandLine: UnKnown = " + s);
            }

            return cmdargs;
        }

        private void BGFirmwareCheck(object state)
        {
            try
            {
                if (Settings.Instance["fw_check"] != DateTime.Now.ToShortDateString())
                {
                    APFirmware.GetList("https://firmware.oborne.me/manifest.json.gz");

                    Settings.Instance["fw_check"] = DateTime.Now.ToShortDateString();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void BGGetKIndex(object state)
        {
            try
            {
                // check the last kindex date
                if (Settings.Instance["kindexdate"] == DateTime.Now.ToShortDateString())
                {
                    KIndex_KIndex(Settings.Instance.GetInt32("kindex"), null);
                }
                else
                {
                    // get a new kindex
                    KIndex.KIndexEvent += KIndex_KIndex;
                    KIndex.GetKIndex();

                    Settings.Instance["kindexdate"] = DateTime.Now.ToShortDateString();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void BGNoFly(object state)
        {
            try
            {
                NoFly.NoFly.Scan();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }


        void KIndex_KIndex(object sender, EventArgs e)
        {
            CurrentState.KIndexstatic = (int) sender;
            Settings.Instance["kindex"] = CurrentState.KIndexstatic.ToString();
        }

        private void BGCreateMaps(object state)
        {
            // sort logs
            try
            {
                MissionPlanner.Log.LogSort.SortLogs(Directory.GetFiles(Settings.Instance.LogDir, "*.tlog"));

                MissionPlanner.Log.LogSort.SortLogs(Directory.GetFiles(Settings.Instance.LogDir, "*.rlog"));
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            try
            {
                // create maps
                Log.LogMap.MapLogs(Directory.GetFiles(Settings.Instance.LogDir, "*.tlog", SearchOption.AllDirectories));
                Log.LogMap.MapLogs(Directory.GetFiles(Settings.Instance.LogDir, "*.bin", SearchOption.AllDirectories));
                Log.LogMap.MapLogs(Directory.GetFiles(Settings.Instance.LogDir, "*.log", SearchOption.AllDirectories));

                if (File.Exists(tlogThumbnailHandler.tlogThumbnailHandler.queuefile))
                {
                    Log.LogMap.MapLogs(File.ReadAllLines(tlogThumbnailHandler.tlogThumbnailHandler.queuefile));

                    File.Delete(tlogThumbnailHandler.tlogThumbnailHandler.queuefile);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            try
            {
                if (File.Exists(tlogThumbnailHandler.tlogThumbnailHandler.queuefile))
                {
                    Log.LogMap.MapLogs(File.ReadAllLines(tlogThumbnailHandler.tlogThumbnailHandler.queuefile));

                    File.Delete(tlogThumbnailHandler.tlogThumbnailHandler.queuefile);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void checkupdate(object stuff)
        {
            if (Program.WindowsStoreApp)
                return;

            try
            {
                MissionPlanner.Utilities.Update.CheckForUpdate();
            }
            catch (Exception ex)
            {
                log.Error("Update check failed", ex);
            }
        }

        private void MainV2_Resize(object sender, EventArgs e)
        {
            // mono - resize is called before the control is created
            if (MyView != null)
                log.Info("myview width " + MyView.Width + " height " + MyView.Height);

            log.Info("this   width " + this.Width + " height " + this.Height);
        }

        private void MenuHelp_Click(object sender, EventArgs e)
        {
            MyView.ShowScreen("Help");
        }


        /// <summary>
        /// keyboard shortcuts override
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ConfigTerminal.SSHTerminal)
            {
                return false;
            }

            if (keyData == Keys.F12)
            {
                MenuConnect_Click(null, null);
                return true;
            }

            if (keyData == Keys.F2)
            {
                MenuFlightData_Click(null, null);
                return true;
            }

            if (keyData == Keys.F3)
            {
                MenuFlightPlanner_Click(null, null);
                return true;
            }

            if (keyData == Keys.F4)
            {
                MenuTuning_Click(null, null);
                return true;
            }

            if (keyData == Keys.F5)
            {
                comPort.getParamList();
                MyView.ShowScreen(MyView.current.Name);
                return true;
            }

            if (keyData == (Keys.Control | Keys.F)) // temp
            {
                Form frm = new temp();
                ThemeManager.ApplyThemeTo(frm);
                frm.Show();
                return true;
            }

            /*if (keyData == (Keys.Control | Keys.S)) // screenshot
            {
                ScreenShot();
                return true;
            }*/
            if (keyData == (Keys.Control | Keys.P))
            {
                new PluginUI().Show();
                return true;
            }

            if (keyData == (Keys.Control | Keys.G)) // nmea out
            {
                Form frm = new SerialOutputNMEA();
                ThemeManager.ApplyThemeTo(frm);
                frm.Show();
                return true;
            }

            if (keyData == (Keys.Control | Keys.X))
            {
                new GMAPCache().ShowUserControl();
                return true;
            }

            if (keyData == (Keys.Control | Keys.L)) // limits
            {
                //new DigitalSkyUI().ShowUserControl();

                new SpectrogramUI().Show();

                return true;
            }

            if (keyData == (Keys.Control | Keys.W)) // test ac config
            {
                new PropagationSettings().Show();

                return true;
            }

            if (keyData == (Keys.Control | Keys.Z))
            {
                //ScanHW.Scan(comPort);
                new Camera().test(MainV2.comPort);
                return true;
            }

            if (keyData == (Keys.Control | Keys.T)) // for override connect
            {
                try
                {
                    MainV2.comPort.Open(false);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(ex.ToString());
                }

                return true;
            }

            if (keyData == (Keys.Control | Keys.Y)) // for ryan beall and ollyw42
            {
                // write
                try
                {
                    MainV2.comPort.doCommand((byte) MainV2.comPort.sysidcurrent, (byte) MainV2.comPort.compidcurrent,
                        MAVLink.MAV_CMD.PREFLIGHT_STORAGE, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                }
                catch
                {
                    CustomMessageBox.Show("Invalid command");
                    return true;
                }

                //read
                ///////MainV2.comPort.doCommand(MAVLink09.MAV_CMD.PREFLIGHT_STORAGE, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                CustomMessageBox.Show("Done MAV_ACTION_STORAGE_WRITE");
                return true;
            }

            if (keyData == (Keys.Control | Keys.J))
            {
                new DevopsUI().ShowUserControl();

                return true;
            }

            if (ProcessCmdKeyCallback != null)
            {
                return ProcessCmdKeyCallback(ref msg, keyData);
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public delegate bool ProcessCmdKeyHandler(ref Message msg, Keys keyData);

        public event ProcessCmdKeyHandler ProcessCmdKeyCallback;

        public void changelanguage(CultureInfo ci)
        {
            log.Info("change lang to " + ci.ToString() + " current " +
                     Thread.CurrentThread.CurrentUICulture.ToString());

            if (ci != null && !Thread.CurrentThread.CurrentUICulture.Equals(ci))
            {
                Thread.CurrentThread.CurrentUICulture = ci;
                Settings.Instance["language"] = ci.Name;
                //System.Threading.Thread.CurrentThread.CurrentCulture = ci;

                HashSet<Control> views = new HashSet<Control> {this, FlightData, FlightPlanner, Simulation};

                foreach (Control view in MyView.Controls)
                    views.Add(view);

                foreach (Control view in views)
                {
                    if (view != null)
                    {
                        ComponentResourceManager rm = new ComponentResourceManager(view.GetType());
                        foreach (Control ctrl in view.Controls)
                        {
                            rm.ApplyResource(ctrl);
                        }

                        rm.ApplyResources(view, "$this");
                    }
                }
            }
        }


        public void ChangeUnits()
        {
            try
            {
                // dist
                if (Settings.Instance["distunits"] != null)
                {
                    switch (
                        (distances) Enum.Parse(typeof(distances), Settings.Instance["distunits"].ToString()))
                    {
                        case distances.Meters:
                            CurrentState.multiplierdist = 1;
                            CurrentState.DistanceUnit = "m";
                            break;
                        case distances.Feet:
                            CurrentState.multiplierdist = 3.2808399f;
                            CurrentState.DistanceUnit = "ft";
                            break;
                    }
                }
                else
                {
                    CurrentState.multiplierdist = 1;
                    CurrentState.DistanceUnit = "m";
                }

                // alt
                if (Settings.Instance["altunits"] != null)
                {
                    switch (
                        (distances) Enum.Parse(typeof(altitudes), Settings.Instance["altunits"].ToString()))
                    {
                        case distances.Meters:
                            CurrentState.multiplieralt = 1;
                            CurrentState.AltUnit = "m";
                            break;
                        case distances.Feet:
                            CurrentState.multiplieralt = 3.2808399f;
                            CurrentState.AltUnit = "ft";
                            break;
                    }
                }
                else
                {
                    CurrentState.multiplieralt = 1;
                    CurrentState.AltUnit = "m";
                }

                // speed
                if (Settings.Instance["speedunits"] != null)
                {
                    switch ((speeds) Enum.Parse(typeof(speeds), Settings.Instance["speedunits"].ToString()))
                    {
                        case speeds.meters_per_second:
                            CurrentState.multiplierspeed = 1;
                            CurrentState.SpeedUnit = "m/s";
                            break;
                        case speeds.fps:
                            CurrentState.multiplierspeed = 3.2808399f;
                            CurrentState.SpeedUnit = "fps";
                            break;
                        case speeds.kph:
                            CurrentState.multiplierspeed = 3.6f;
                            CurrentState.SpeedUnit = "kph";
                            break;
                        case speeds.mph:
                            CurrentState.multiplierspeed = 2.23693629f;
                            CurrentState.SpeedUnit = "mph";
                            break;
                        case speeds.knots:
                            CurrentState.multiplierspeed = 1.94384449f;
                            CurrentState.SpeedUnit = "kts";
                            break;
                    }
                }
                else
                {
                    CurrentState.multiplierspeed = 1;
                    CurrentState.SpeedUnit = "m/s";
                }
            }
            catch
            {
            }
        }

        private void CMB_baudrate_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(_connectionControl.CMB_baudrate.Text, out comPortBaud))
            {
                CustomMessageBox.Show(Strings.InvalidBaudRate, Strings.ERROR);
                return;
            }

            var sb = new StringBuilder();
            int baud = 0;
            for (int i = 0; i < _connectionControl.CMB_baudrate.Text.Length; i++)
                if (char.IsDigit(_connectionControl.CMB_baudrate.Text[i]))
                {
                    sb.Append(_connectionControl.CMB_baudrate.Text[i]);
                    baud = baud * 10 + _connectionControl.CMB_baudrate.Text[i] - '0';
                }

            if (_connectionControl.CMB_baudrate.Text != sb.ToString())
            {
                _connectionControl.CMB_baudrate.Text = sb.ToString();
            }

            try
            {
                if (baud > 0 && comPort.BaseStream.BaudRate != baud)
                    comPort.BaseStream.BaudRate = baud;
            }
            catch (Exception)
            {
            }
        }

        private void MainMenu_MouseLeave(object sender, EventArgs e)
        {
            if (_connectionControl.PointToClient(Control.MousePosition).Y < MainMenu.Height)
                return;

            this.SuspendLayout();

            panel1.Visible = false;

            this.ResumeLayout();
        }

        void menu_MouseEnter(object sender, EventArgs e)
        {
            this.SuspendLayout();
            panel1.Location = new Point(0, 0);
            panel1.Width = menu.Width;
            panel1.BringToFront();
            panel1.Visible = true;
            this.ResumeLayout();
        }

        private void autoHideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoHideMenu(autoHideToolStripMenuItem.Checked);

            Settings.Instance["menu_autohide"] = autoHideToolStripMenuItem.Checked.ToString();
        }

        void AutoHideMenu(bool hide)
        {
            autoHideToolStripMenuItem.Checked = hide;

            if (!hide)
            {
                this.SuspendLayout();
                panel1.Dock = DockStyle.Top;
                panel1.SendToBack();
                panel1.Visible = true;
                menu.Visible = false;
                MainMenu.MouseLeave -= MainMenu_MouseLeave;
                panel1.MouseLeave -= MainMenu_MouseLeave;
                toolStripConnectionControl.MouseLeave -= MainMenu_MouseLeave;
                this.ResumeLayout();
            }
            else
            {
                this.SuspendLayout();
                panel1.Dock = DockStyle.None;
                panel1.Visible = false;
                MainMenu.MouseLeave += MainMenu_MouseLeave;
                panel1.MouseLeave += MainMenu_MouseLeave;
                toolStripConnectionControl.MouseLeave += MainMenu_MouseLeave;
                menu.Visible = true;
                menu.SendToBack();
                this.ResumeLayout();
            }
        }

        private void MainV2_KeyDown(object sender, KeyEventArgs e)
        {
            Message temp = new Message();
            ProcessCmdKey(ref temp, e.KeyData);
            Console.WriteLine("MainV2_KeyDown " + e.ToString());
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=mich146%40hotmail%2ecom&lc=AU&item_name=Michael%20Oborne&no_note=0&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHostedGuest");
            }
            catch
            {
                CustomMessageBox.Show("Link open failed. check your default webpage association");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_HDR
        {
            public Int32 dbch_size;
            public Int32 dbch_devicetype;
            public Int32 dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_PORT
        {
            public int dbcp_size;
            public int dbcp_devicetype;
            public int dbcp_reserved; // MSDN say "do not use"

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcp_name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            public Int32 dbcc_size;
            public Int32 dbcc_devicetype;
            public Int32 dbcc_reserved;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            public Byte[]
                dbcc_classguid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }


        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CREATE:
                    try
                    {
                        DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE();
                        IntPtr devBroadcastDeviceInterfaceBuffer;
                        IntPtr deviceNotificationHandle = IntPtr.Zero;
                        Int32 size = 0;

                        // frmMy is the form that will receive device-change messages.


                        size = Marshal.SizeOf(devBroadcastDeviceInterface);
                        devBroadcastDeviceInterface.dbcc_size = size;
                        devBroadcastDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
                        devBroadcastDeviceInterface.dbcc_reserved = 0;
                        devBroadcastDeviceInterface.dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE.ToByteArray();
                        devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(size);
                        Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true);


                        deviceNotificationHandle = NativeMethods.RegisterDeviceNotification(this.Handle,
                            devBroadcastDeviceInterfaceBuffer, DEVICE_NOTIFY_WINDOW_HANDLE);

                        Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer);
                    }
                    catch
                    {
                    }

                    break;

                case WM_DEVICECHANGE:
                    // The WParam value identifies what is occurring.
                    WM_DEVICECHANGE_enum n = (WM_DEVICECHANGE_enum) m.WParam;
                    var l = m.LParam;
                    if (n == WM_DEVICECHANGE_enum.DBT_DEVICEREMOVEPENDING)
                    {
                        Console.WriteLine("DBT_DEVICEREMOVEPENDING");
                    }

                    if (n == WM_DEVICECHANGE_enum.DBT_DEVNODES_CHANGED)
                    {
                        Console.WriteLine("DBT_DEVNODES_CHANGED");
                    }

                    if (n == WM_DEVICECHANGE_enum.DBT_DEVICEARRIVAL ||
                        n == WM_DEVICECHANGE_enum.DBT_DEVICEREMOVECOMPLETE)
                    {
                        Console.WriteLine(((WM_DEVICECHANGE_enum) n).ToString());

                        DEV_BROADCAST_HDR hdr = new DEV_BROADCAST_HDR();
                        Marshal.PtrToStructure(m.LParam, hdr);

                        try
                        {
                            switch (hdr.dbch_devicetype)
                            {
                                case DBT_DEVTYP_DEVICEINTERFACE:
                                    DEV_BROADCAST_DEVICEINTERFACE inter = new DEV_BROADCAST_DEVICEINTERFACE();
                                    Marshal.PtrToStructure(m.LParam, inter);
                                    log.InfoFormat("Interface {0}", inter.dbcc_name);
                                    break;
                                case DBT_DEVTYP_PORT:
                                    DEV_BROADCAST_PORT prt = new DEV_BROADCAST_PORT();
                                    Marshal.PtrToStructure(m.LParam, prt);
                                    log.InfoFormat("port {0}", prt.dbcp_name);
                                    break;
                            }
                        }
                        catch
                        {
                        }

                        //string port = Marshal.PtrToStringAuto((IntPtr)((long)m.LParam + 12));
                        //Console.WriteLine("Added port {0}",port);
                    }

                    log.InfoFormat("Device Change {0} {1} {2}", m.Msg, (WM_DEVICECHANGE_enum) m.WParam, m.LParam);

                    if (DeviceChanged != null)
                    {
                        try
                        {
                            DeviceChanged((WM_DEVICECHANGE_enum) m.WParam);
                        }
                        catch
                        {
                        }
                    }

                    foreach (var item in MissionPlanner.Plugin.PluginLoader.Plugins)
                    {
                        item.Host.ProcessDeviceChanged((WM_DEVICECHANGE_enum) m.WParam);
                    }

                    break;
                case 0x86: // WM_NCACTIVATE
                    //var thing = Control.FromHandle(m.HWnd);

                    var child = Control.FromHandle(m.LParam);

                    if (child is Form)
                    {
                        log.Debug("ApplyThemeTo " + child.Name);
                        ThemeManager.ApplyThemeTo(child);
                    }

                    break;
                default:
                    //Console.WriteLine(m.ToString());
                    break;
            }

            base.WndProc(ref m);
        }

        const int DBT_DEVTYP_PORT = 0x00000003;
        const int WM_CREATE = 0x0001;
        const Int32 DBT_DEVTYP_HANDLE = 6;
        const Int32 DBT_DEVTYP_DEVICEINTERFACE = 5;
        const Int32 DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        const Int32 DIGCF_PRESENT = 2;
        const Int32 DIGCF_DEVICEINTERFACE = 0X10;
        const Int32 WM_DEVICECHANGE = 0X219;
        public static Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");


        public enum WM_DEVICECHANGE_enum
        {
            DBT_CONFIGCHANGECANCELED = 0x19,
            DBT_CONFIGCHANGED = 0x18,
            DBT_CUSTOMEVENT = 0x8006,
            DBT_DEVICEARRIVAL = 0x8000,
            DBT_DEVICEQUERYREMOVE = 0x8001,
            DBT_DEVICEQUERYREMOVEFAILED = 0x8002,
            DBT_DEVICEREMOVECOMPLETE = 0x8004,
            DBT_DEVICEREMOVEPENDING = 0x8003,
            DBT_DEVICETYPESPECIFIC = 0x8005,
            DBT_DEVNODES_CHANGED = 0x7,
            DBT_QUERYCHANGECONFIG = 0x17,
            DBT_USERDEFINED = 0xFFFF,
        }

        private void MainMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            foreach (ToolStripItem item in MainMenu.Items)
            {
                if (e.ClickedItem == item)
                {
                    item.BackColor = ThemeManager.ControlBGColor;
                }
                else
                {
                    try
                    {
                        item.BackColor = Color.Transparent;
                        item.BackgroundImage = displayicons.bg; //.BackColor = Color.Black;
                    }
                    catch
                    {
                    }
                }
            }
            //MainMenu.BackColor = Color.Black;
            //MainMenu.BackgroundImage = MissionPlanner.Properties.Resources.bgdark;
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // full screen
            if (fullScreenToolStripMenuItem.Checked)
            {
                this.TopMost = true;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.TopMost = false;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void readonlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainV2.comPort.ReadOnly = readonlyToolStripMenuItem.Checked;
        }

        private void connectionOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ConnectionOptions().Show(this);
        }

        private void MenuArduPilot_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://ardupilot.org/?utm_source=Menu&utm_campaign=MP");
            }
            catch
            {
                CustomMessageBox.Show("Failed to open url https://ardupilot.org");
            }
        }

        private void connectionListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();

            if (File.Exists(openFileDialog.FileName))
            {
                var lines = File.ReadAllLines(openFileDialog.FileName);

                Regex tcp = new Regex("tcp://(.*):([0-9]+)");
                Regex udp = new Regex("udp://(.*):([0-9]+)");
                Regex udpcl = new Regex("udpcl://(.*):([0-9]+)");
                Regex serial = new Regex("serial:(.*):([0-9]+)");

                ConcurrentBag<MAVLinkInterface> mavs = new ConcurrentBag<MAVLinkInterface>();

                Parallel.ForEach(lines, line =>
                        //foreach (var line in lines)
                    {
                        try
                        {
                            Console.WriteLine("Process port " + line);
                            MAVLinkInterface mav = new MAVLinkInterface();

                            if (tcp.IsMatch(line))
                            {
                                var matches = tcp.Match(line);
                                var tc = new TcpSerial();
                                tc.client = new TcpClient(matches.Groups[1].Value, int.Parse(matches.Groups[2].Value));
                                mav.BaseStream = tc;
                            }
                            else if (udp.IsMatch(line))
                            {
                                var matches = udp.Match(line);
                                var uc = new UdpSerial(new UdpClient(int.Parse(matches.Groups[2].Value)));
                                uc.Port = matches.Groups[2].Value;
                                mav.BaseStream = uc;
                            }
                            else if (udpcl.IsMatch(line))
                            {
                                var matches = udpcl.Match(line);
                                var udc = new UdpSerialConnect();
                                udc.Port = matches.Groups[2].Value;
                                udc.client = new UdpClient(matches.Groups[1].Value, int.Parse(matches.Groups[2].Value));
                                mav.BaseStream = udc;
                            }
                            else if (serial.IsMatch(line))
                            {
                                var matches = serial.Match(line);
                                var port = new Comms.SerialPort();
                                port.PortName = matches.Groups[1].Value;
                                port.BaudRate = int.Parse(matches.Groups[2].Value);
                                mav.BaseStream = port;
                                ((SerialPort)mav.BaseStream).espFix = Settings.Instance.GetBoolean("CHK_rtsresetesp32", false);
                                mav.BaseStream.Open();
                            }
                            else
                            {
                                return;
                            }

                            mavs.Add(mav);
                        }
                        catch
                        {
                        }
                    }
                );
                /*
                foreach (var mav in mavs)
                {
                    MainV2.instance.BeginInvoke((Action) delegate
                    {
                        doConnect(mav, "preset", "0", false, false);
                        Comports.Add(mav);
                    });
                }

                */

                Parallel.ForEach(mavs, mav =>
                {
                    Console.WriteLine("Process connect " + mav);
                    doConnect(mav, "preset", "0", false, false);
                    Comports.Add(mav);

                    try
                    {
                        Comports = Comports.Distinct().ToList();
                    }
                    catch { }
                });
            }
        }

        //Handle QV panel coloring from warning engine
        private void WarningEngine_QuickPanelColoring(string name, string color)
        {
            // return if we still initialize
            if (FlightData == null) return;

            //Find panel with
            foreach (var q in FlightData.tabQuick.Controls["tableLayoutPanelQuick"].Controls)
            {
                QuickView qv = (QuickView) q;

                //Get the data field name bind to the control
                var fieldname = qv.DataBindings[0].BindingMemberInfo.BindingField;

                if (fieldname == name)
                {

                    if (color == "NoColor")
                    {
                        qv.BackColor = ThemeManager.BGColor;
                        qv.numberColor = qv.numberColorBackup; //Restore original color from backup :)
                        qv.ForeColor = ThemeManager.TextColor;


                    }
                    else
                    {
                        qv.BackColor = Color.FromName(color);
                        // Ensure color is readable on the background
                        qv.numberColor = (((qv.BackColor.R + qv.BackColor.B + qv.BackColor.G) / 3) > 128)
                            ? Color.Black
                            : Color.White;
                        qv.ForeColor = qv.numberColor; //Same as the number
                    }

                    //We have our panel, color it and exit loop
                    break;
                }
            }
        }

        private string SelectUdpPort()
        {
            try
            {
                using (var dlg = new Form())
                {
                    dlg.Text = "选择UDP端口";
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dlg.MaximizeBox = false;
                    dlg.MinimizeBox = false;
                    dlg.Size = new Size(260, 150);

                    var label = new Label { Text = "输入监听端口:", Location = new Point(15, 20), Size = new Size(100, 20) };
                    var tb = new TextBox { Location = new Point(120, 18), Size = new Size(100, 22) };
                    var last = Settings.Instance.GetString("LastUDP_Port", "14550");
                    tb.Text = string.IsNullOrEmpty(last) ? "14550" : last;

                    var ok = new Button { Text = "确定", Location = new Point(60, 60), Size = new Size(60, 25), DialogResult = DialogResult.OK };
                    var cancel = new Button { Text = "取消", Location = new Point(140, 60), Size = new Size(60, 25), DialogResult = DialogResult.Cancel };

                    dlg.Controls.AddRange(new Control[] { label, tb, ok, cancel });
                    dlg.AcceptButton = ok;
                    dlg.CancelButton = cancel;

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        var chosen = tb.Text?.Trim();
                        // 简单校验：必须为数字
                        if (!string.IsNullOrEmpty(chosen) && !int.TryParse(chosen, out _))
                            return null;
                        if (!string.IsNullOrEmpty(chosen))
                            Settings.Instance["LastUDP_Port"] = chosen;
                        return chosen;
                    }

                    return null;
                }
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// 自动连接管理器 - 支持TCP地址自动切换
    /// </summary>
    public class AutoConnectManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AutoConnectManager));
        
        private bool _isEnabled = false;
        private bool _isManualDisconnect = false;
        private bool _isAutoConnecting = false;
        private string _primaryTcpHost = "";
        private string _primaryTcpPort = "5760";
        private string _backupTcpHost = "";
        private string _backupTcpPort = "5760";
        private DateTime _lastConnectionCheck = DateTime.Now;
        private DateTime _lastValidPacket = DateTime.Now;
        private System.Threading.Timer _connectionMonitorTimer;
        private bool _isReconnecting = false;
        private double _qualityThreshold = 0.7; // 0..1
        private double _qualityDifferenceThreshold = 0.1; // 质量差异阈值，避免频繁切换
        private int _qualityWindowSec = 3;
        private int _minSwitchIntervalSec = 10;
        private DateTime _lastSwitchUtc = DateTime.MinValue;
        private IDisposable _qualitySub;
        private IDisposable _passiveQualitySub;
        private MAVLinkInterface _passiveMav;
        private MissionPlanner.Comms.TcpSerial _passiveTcp;
        private double _passiveQuality = 0.0;
        private DateTime _lastQualityReport = DateTime.MinValue;
        private System.Threading.Timer _qualityReportTimer;
        public bool EnableDualListen { get; set; } = false;
        private bool _manualConnectedOnce = false;
        private const string _udpPortA = "14551";
        private const string _udpPortB = "14550";

        /// <summary>
        /// 是否启用自动连接
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        /// <summary>
        /// 是否正在自动连接
        /// </summary>
        public bool IsAutoConnecting
        {
            get { return _isAutoConnecting; }
            set { _isAutoConnecting = value; }
        }

        /// <summary>
        /// 主TCP地址
        /// </summary>
        public string PrimaryTcpHost
        {
            get { return _primaryTcpHost; }
            set { _primaryTcpHost = value; }
        }

        /// <summary>
        /// 备用TCP地址
        /// </summary>
        public string BackupTcpHost
        {
            get { return _backupTcpHost; }
            set { _backupTcpHost = value; }
        }

        public string GetPortForHost(string host)
        {
            if (string.IsNullOrEmpty(host)) return _primaryTcpPort;
            if (host == _primaryTcpHost) return _primaryTcpPort;
            if (host == _backupTcpHost) return _backupTcpPort;
            return _primaryTcpPort;
        }

        /// <summary>
        /// 当前使用的TCP地址
        /// </summary>
        public string CurrentTcpHost { get; set; } = "";

        /// <summary>
        /// 初始化自动连接管理器
        /// </summary>
        public void Initialize()
        {
            if (_connectionMonitorTimer != null)
            {
                _connectionMonitorTimer.Dispose();
            }
            //连接状态检查，参数1：状态检查函数，参数2：延迟时间，参数3：检查间隔
            _connectionMonitorTimer = new System.Threading.Timer(CheckConnectionStatus, null, 1000, 1000);

            // 订阅质量监控（仅活跃链路）。被动监听仅在手动连接完成后按需启动
            try
            {
                _qualitySub?.Dispose();
                var mav = MainV2.comPort;
                if (mav != null)
                {
                    _qualitySub = System.Reactive.Linq.Observable
                        .CombineLatest(
                            mav.WhenPacketReceived.Buffer(TimeSpan.FromSeconds(_qualityWindowSec), TimeSpan.FromSeconds(1)).Select(xs => xs.Sum()),
                            mav.WhenPacketLost.Buffer(TimeSpan.FromSeconds(_qualityWindowSec), TimeSpan.FromSeconds(1)).Select(xs => xs.Sum()),
                            (rx, lost) => new { rx, lost })
                        .Subscribe(v =>
                        {
                            double denom = v.rx + v.lost;
                            double quality = denom <= 0 ? 0 : v.rx / denom;
                            EvaluateQualityAndMaybeSwitch(quality);
                        });
                }

                // 注意：初始化时不立即启动被动监听，避免在手动连接完成前占用资源
            }
            catch { }
            
            // 设置定期质量报告定时器（每30秒报告一次）
            _qualityReportTimer?.Dispose();
            _qualityReportTimer = new System.Threading.Timer(_ => ReportQualityStatus(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
            
            // 在初始化完成后，如果已经手动建立过连接，且当前为UDP并启用双监听，
            // 则延迟启动一次被动监听，确保首次手动UDP连接后即可建立双端监听
            try
            {
                if (EnableDualListen && _manualConnectedOnce &&
                    (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerial ||
                     MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerialConnect))
                {
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                    {
                        try { SetupPassiveListener(); } catch { }
                    });
                }
            }
            catch { }

            log.Info("自动连接管理器初始化完成");
        }

        /// <summary>
        /// 在需要时（手动UDP连接完成后）触发被动监听的建立
        /// </summary>
        public void TriggerPassiveListenIfNeeded()
        {
            try
            {
                if (EnableDualListen && _manualConnectedOnce &&
                    (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerial ||
                     MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerialConnect))
                {
                    // SetupPassiveListener();
                }
            }
            catch { }
        }
        //更新sysid下拉菜单端口选项
        public static void UpdateSysidPortOptions()
        {
            //获取当前sysid下拉菜单
            
        }
        private void SetupPassiveListener()
        {
            try
            {
                // 清理旧的
                TeardownPassiveListener();

                if (!EnableDualListen || !_manualConnectedOnce)
                    return;

                // 根据当前活跃链接类型（TCP/UDP）选择被动监听的方式
                if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.TcpSerial)
                {
                    // 已禁用：TCP 被动监听，避免在 UDP 使用场景中被 TCP 自动检测/监听干扰
                    return;
                }
                else if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerial || MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerialConnect)
                {
                    // UDP 双监听：根据当前手动选择的本地监听端口，智能选择被动监听端口
                    var activeUdpPort = (MainV2.comPort.BaseStream as dynamic).Port as string;
                    string passivePort;
                    
                    // 智能端口选择逻辑：
                    // 1. 如果主动端口是14551，被动端口为14552
                    // 2. 如果主动端口是14552，被动端口为14551
                    // 3. 如果主动端口是其他端口，默认被动端口为14551
                    if (activeUdpPort == _udpPortA)
                    {
                        passivePort = _udpPortB; // 14552
                    }
                    else if (activeUdpPort == _udpPortB)
                    {
                        passivePort = _udpPortA; // 14551
                    }
                    else
                    {
                        // 用户输入的其他端口，默认被动监听14551
                        passivePort = _udpPortA; // 14551
                    }

                    log.Info($"UDP双端监听: {activeUdpPort} -> {passivePort}");

                    // 优先复用已有的同端口连接（通常由 AutoConnect 建立，避免端口占用冲突）
                    var existing = MainV2.Comports.FirstOrDefault(m =>
                        (m.BaseStream is MissionPlanner.Comms.UdpSerial us && us.Port == passivePort) ||
                        (m.BaseStream is MissionPlanner.Comms.UdpSerialConnect uc && uc.Port == passivePort));

                    if (existing != null)
                    {
                        _passiveMav = existing;
                        SetupPassiveQualityMonitoring();
                        // 已存在于 Comports，仅刷新下拉
                        try { MainV2._connectionControl?.UpdateSysIDS(); } catch { }
                    }
                    else
                    {
                        var udp = new MissionPlanner.Comms.UdpSerial();
                        udp.Port = passivePort;
                        udp.SuppressPrompts = true;

                        _passiveMav = new MAVLinkInterface { BaseStream = udp };
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try 
                            { 
                                udp.Open(); 
                                _passiveMav.Open(getparams: false, skipconnectedcheck: true, showui: false);
                                //开启被动端质量监控
                                SetupPassiveQualityMonitoring();
                                // 优化：后台线程处理去重，UI线程触发合并刷新
                                try
                                {
                                    // 后台线程生成新列表，避免UI阻塞
                                    var newComports = MainV2.Comports.Append(_passiveMav).Distinct().ToList();

                                    MainV2.instance.BeginInvoke((Action)delegate
                                    {
                                        try
                                        {
                                            MainV2.Comports = newComports;
                                            MainV2._connectionControl?.UpdateSysIDS();
                                            // 强制HUD主视图重绘（如有需要，可以具体到FlightData/myhud等）
                                            if (GCSViews.FlightData.myhud != null)
                                                GCSViews.FlightData.myhud.Invalidate();
                                        }
                                        catch { }
                                    });
                                }
                                catch { }
                            } 
                            catch (Exception ex)
                        {
                                log.Warn("Failed to open passive UDP port", ex);
                                try
                                {
                                        // 即使被动端口打开失败，也刷新一次下拉，避免停留在旧状态
                                        MainV2.instance?.BeginInvoke((Action)delegate
                                        {
                                                try { MainV2._connectionControl?.UpdateSysIDS(); } catch { }
                                        });
                                }
                                catch { }
                        }
                        });
                    }
                }
                else
                {
                    // 其他类型不做被动监听
                    return;
                }
            }
            catch (Exception ex)
            {
                log.Warn("Passive listener failed to start", ex);
            }
        }

        /// <summary>
        /// 关闭并移除被动监听，同时从 Comports 中删除并刷新 sysid 下拉
        /// </summary>
        private void TeardownPassiveListener()
        {
            try
            {
                try { _passiveQualitySub?.Dispose(); } catch { }
                try { _passiveTcp?.Close(); } catch { }
                try { _passiveMav?.Close(); } catch { }

                if (_passiveMav != null)
                {
                    try
                    {
                        MainV2.instance?.BeginInvoke((Action)delegate
                        {
                            try { MainV2.Comports.Remove(_passiveMav); } catch { }
                            MainV2._connectionControl?.UpdateSysIDS();
                        });
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                _passiveMav = null;
                _passiveTcp = null;
            }
        }

        /// <summary>
        /// 设置被动监听的质量监控
        /// </summary>
        private void SetupPassiveQualityMonitoring()
        {
            try
            {
                if (_passiveMav == null) return;

                // 清理旧的订阅
                _passiveQualitySub?.Dispose();

                // 订阅被动监听的质量监控
                _passiveQualitySub = System.Reactive.Linq.Observable
                    .CombineLatest(
                        _passiveMav.WhenPacketReceived.Buffer(TimeSpan.FromSeconds(_qualityWindowSec), TimeSpan.FromSeconds(1)).Select(xs => xs.Sum()),
                        _passiveMav.WhenPacketLost.Buffer(TimeSpan.FromSeconds(_qualityWindowSec), TimeSpan.FromSeconds(1)).Select(xs => xs.Sum()),
                        (rx, lost) => new { rx, lost })
                    .Subscribe(v =>
                    {
                        double denom = v.rx + v.lost;
                        _passiveQuality = denom <= 0 ? 0 : v.rx / denom;
                        
                        // 定期报告被动监听质量
                        if (DateTime.Now - _lastQualityReport > TimeSpan.FromSeconds(5))
                        {
                            log.Info($"被动端质量检测: RX={v.rx}, Lost={v.lost}, Quality={_passiveQuality:0.00}");
                            _lastQualityReport = DateTime.Now;
                        }
                    });

                log.Info("Passive quality monitoring started");
            }
            catch (Exception ex)
            {
                log.Warn("Failed to setup passive quality monitoring", ex);
            }
        }

        /// <summary>
        /// 当用户通过 sysid 切换主动端口时调用：在双监听下交换主动/被动角色
        /// </summary>
        /// <param name="oldActive">切换前的主动端口</param>
        /// <param name="newActive">切换后的主动端口（MainV2.comPort）</param>
        public void OnActivePortChanged(MAVLinkInterface oldActive, MAVLinkInterface newActive)
        {
            try
            {
                if (!EnableDualListen)
                    return;

                // 重新绑定主动链路质量订阅到 newActive
                try
                {
                    _qualitySub?.Dispose();
                    if (newActive != null)
                    {
                        _qualitySub = System.Reactive.Linq.Observable
                            .CombineLatest(
                                newActive.WhenPacketReceived.Buffer(TimeSpan.FromSeconds(_qualityWindowSec), TimeSpan.FromSeconds(1)).Select(xs => xs.Sum()),
                                newActive.WhenPacketLost.Buffer(TimeSpan.FromSeconds(_qualityWindowSec), TimeSpan.FromSeconds(1)).Select(xs => xs.Sum()),
                                (rx, lost) => new { rx, lost })
                            .Subscribe(v =>
                            {
                                double denom = v.rx + v.lost;
                                double quality = denom <= 0 ? 0 : v.rx / denom;
                                EvaluateQualityAndMaybeSwitch(quality);
                            });
                    }
                }
                catch { }

                // 若用户选中了当前的被动端口，则交换角色：原主动 -> 被动
                if (_passiveMav == newActive)
                {
                    _passiveMav = oldActive;
                    SetupPassiveQualityMonitoring();
                    _lastSwitchUtc = DateTime.UtcNow;
                    log.Info("Active/Passive swapped due to sysid selection");
                }
                else
                {
                    // 如果不是选中被动端，确保仍然保持被动监听存在于另一端
                    try { SetupPassiveListener(); } catch { }
                }
            }
            catch (Exception ex)
            {
                log.Warn("OnActivePortChanged failed", ex);
            }
        }

        /// <summary>
        /// 定期报告质量状态
        /// </summary>
        private void ReportQualityStatus()
        {
            try
            {
                if (!_isEnabled || MainV2.comPort?.BaseStream?.IsOpen != true)
                    return;

                var activePort = GetCurrentPortInfo();
                var passivePort = GetPassivePortInfo();
                
                // 获取被动端口的质量信息 - 检查被动监听是否已启动并正在接收数据
                bool passiveConnected = _passiveMav?.BaseStream?.IsOpen == true && _passiveQualitySub != null;
                // 保护性调试日志，避免空引用
                log.Info($"_passiveMav: {_passiveMav}");
                var bs = _passiveMav?.BaseStream;
                log.Info($"_passiveMav.BaseStream: {bs}");
                log.Info($"_passiveMav.BaseStream.IsOpen: {(bs is null ? "<null>" : (bs.IsOpen ? "True" : "False"))}");
                log.Info($"_passiveQualitySub: {_passiveQualitySub}");
                // 如果启用了双监听且被动端口已连接，显示两个端口的质量
                if (EnableDualListen && passiveConnected)
                {
                    log.Info($"UDP Dual Port Quality - Active: {activePort} ({GetCurrentQuality():0.00}) | Passive: {passivePort} ({_passiveQuality:0.00})");
                }
                // 如果启用了双监听但被动端口未连接，显示连接状态
                else if (EnableDualListen)
                {
                    log.Info($"UDP Single Port Quality - Active: {activePort} ({GetCurrentQuality():0.00}) | Passive: {passivePort} (Not Connected)");
                }
                // 单端口模式
                else
                {
                    log.Info($"UDP Single Port Quality - Active: {activePort} ({GetCurrentQuality():0.00})");
                }
            }
            catch (Exception ex)
            {
                log.Warn("Error reporting quality status", ex);
            }
        }

        /// <summary>
        /// 获取当前端口信息
        /// </summary>
        private string GetCurrentPortInfo()
        {
            try
            {
                if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.TcpSerial tcp)
                    return $"TCP {tcp.Host}:{tcp.Port}";
                else if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerial udp)
                    return $"UDP {udp.Port}";
                else if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerialConnect udpCl)
                    return $"UDPCl {udpCl.Port}";
                else
                    return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 获取被动端口信息
        /// </summary>
        private string GetPassivePortInfo()
        {
            try
            {
                if (_passiveTcp != null)
                    return $"TCP {_passiveTcp.Host}:{_passiveTcp.Port}";
                else if (_passiveMav?.BaseStream is MissionPlanner.Comms.UdpSerial udp)
                    return $"UDP {udp.Port}";
                else
                    return "Not Available";
            }
            catch
            {
                return "Not Available";
            }
        }

        /// <summary>
        /// 获取当前质量（简化版本）
        /// </summary>
        private double GetCurrentQuality()
        {
            try
            {
                if (MainV2.comPort?.MAV?.cs != null)
                {
                    return MainV2.comPort.MAV.cs.linkqualitygcs / 100.0;
                }
                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// 设置TCP地址配置
        /// </summary>
        /// <param name="primaryHost">主TCP地址</param>
        /// <param name="primaryPort">主端口</param>
        /// <param name="backupHost">备用TCP地址</param>
        /// <param name="backupPort">备端口</param>
        public void SetTcpAddresses(string primaryHost, string primaryPort, string backupHost, string backupPort)
        {
            _primaryTcpHost = primaryHost;
            _primaryTcpPort = string.IsNullOrWhiteSpace(primaryPort) ? _primaryTcpPort : primaryPort;
            _backupTcpHost = backupHost;
            _backupTcpPort = string.IsNullOrWhiteSpace(backupPort) ? _backupTcpPort : backupPort;
            CurrentTcpHost = primaryHost;
            log.Info($"AutoConnectManager configured - Primary: {primaryHost}:{_primaryTcpPort}, Backup: {backupHost}:{_backupTcpPort}");
        }

        public void SetQualityPolicy(double threshold, int windowSec, int minSwitchIntervalSec, double qualityDifferenceThreshold = 0.1)
        {
            _qualityThreshold = Math.Max(0.0, Math.Min(1.0, threshold));
            _qualityWindowSec = Math.Max(1, windowSec);
            _minSwitchIntervalSec = Math.Max(1, minSwitchIntervalSec);
            _qualityDifferenceThreshold = Math.Max(0.0, Math.Min(0.5, qualityDifferenceThreshold));
            
            log.Info($"Quality policy updated - Threshold: {_qualityThreshold:0.00}, Window: {_qualityWindowSec}s, MinSwitch: {_minSwitchIntervalSec}s, DiffThreshold: {_qualityDifferenceThreshold:0.00}");
        }

        /// <summary>
        /// 启用自动连接
        /// </summary>
        public void EnableAutoConnect()
        {
            _isEnabled = true;
            _isManualDisconnect = false;
            log.Info("AutoConnectManager enabled");
        }

        /// <summary>
        /// 禁用自动连接
        /// </summary>
        public void DisableAutoConnect()
        {
            _isEnabled = false;
            
            // 清理质量监控订阅
            _qualitySub?.Dispose();
            _passiveQualitySub?.Dispose();
            
            // 清理定时器
            _qualityReportTimer?.Dispose();
            
            // 清理被动监听
            TeardownPassiveListener();
            
            log.Info("AutoConnectManager disabled");
        }

        /// <summary>
        /// 标记为手动断开连接
        /// </summary>
        public void MarkManualDisconnect()
        {
            _isManualDisconnect = true;
            
            // 保存当前连接的地址和端口，用于下次连接时的默认值
            if (!string.IsNullOrEmpty(CurrentTcpHost))
            {
                // 保存到设置中，用于下次连接
                Settings.Instance["LastTCP_Host"] = CurrentTcpHost;
                Settings.Instance["LastTCP_Port"] = GetPortForHost(CurrentTcpHost);
                log.Info($"Saved last TCP connection: {CurrentTcpHost}:{GetPortForHost(CurrentTcpHost)}");
            }
            
            // 注意：不清空CurrentTcpHost，保持上次连接的地址用于下次连接
            
            log.Info("Marked as manual disconnect");

            // 关闭被动监听以释放资源
            TeardownPassiveListener();
        }

        /// <summary>
        /// 标记为自动连接
        /// </summary>
        public void MarkAutoConnect()
        {
            _isManualDisconnect = false;
            _isAutoConnecting = true;
        }

        /// <summary>
        /// 标记为手动连接
        /// </summary>
        public void MarkManualConnect()
        {
            _isManualDisconnect = false;
            _isAutoConnecting = false;
            _manualConnectedOnce = true;
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        private void CheckConnectionStatus(object state)
        {
            if (!_isEnabled || _isManualDisconnect || _isReconnecting)
                return;

            try
            {
                // 检查当前连接状态
                if (MainV2.comPort?.BaseStream?.IsOpen == true)
                {
                    // 更新最后有效数据包时间
                    if (MainV2.comPort.MAV?.lastvalidpacket != null)
                    {
                        _lastValidPacket = MainV2.comPort.MAV.lastvalidpacket;
                    }

                    // 检查是否长时间没有收到数据包（超过2秒认为连接断开）
                    if ((DateTime.UtcNow - _lastValidPacket).TotalSeconds > 2)
                    {
                        log.Warn("连接丢失，没有收到有效数据包");
                        AttemptUdpReconnect();
                    }
                }
                else
                {
                    // 连接已断开，根据连接类型尝试重连
                    if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.TcpSerial)
                    {
                        // TCP连接断开，尝试重连到备用TCP地址
                        if (!string.IsNullOrEmpty(CurrentTcpHost))
                        {
                            log.Warn("TCP connection is closed, attempting reconnect");
                            AttemptReconnect();
                        }
                    }
                    else if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerial || 
                             MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerialConnect)
                    {
                        // UDP连接断开，尝试切换到备用UDP端口
                        log.Warn("UDP connection is closed, attempting to switch to passive port");
                        AttemptUdpReconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error in connection status check", ex);
            }
        }

        /// <summary>
        /// 尝试重新连接
        /// </summary>
        private void AttemptReconnect()
        {
            if (_isReconnecting)
                return;

            _isReconnecting = true;

            try
            {
                // 确定要尝试的下一个地址
                string nextHost = GetNextTcpHost();
                
                if (!string.IsNullOrEmpty(nextHost))
                {
                    string nextPort = GetPortForHost(nextHost);
                    log.Info($"Attempting connection to: {nextHost}:{nextPort}");
                    CurrentTcpHost = nextHost;
                    ConnectToTcp(nextHost, nextPort);
                    // 切换后（且已手动连接过）更新被动监听到另一端
                    if (_manualConnectedOnce)
                        SetupPassiveListener();
                }
                else
                {
                    log.Warn("No valid TCP host available for reconnection");
                }
            }
            catch (Exception ex)
            {
                log.Error("Error during reconnect attempt", ex);
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        /// <summary>
        /// 尝试UDP重连（切换到被动端口）
        /// </summary>
        private void AttemptUdpReconnect()
        {
            if (_isReconnecting)
                return;

            _isReconnecting = true;

            try
            {
                // 检查是否有被动监听可用
                if (_passiveMav != null && _passiveMav.BaseStream?.IsOpen == true)
                {
                    log.Info("Switching to passive UDP port due to active port failure");
                    SwitchToPassivePort();
                }
                else
                {
                    // 如果没有被动监听，尝试重新设置
                    if (EnableDualListen && _manualConnectedOnce)
                    {
                        log.Info("Attempting to restart UDP dual listen");
                        SetupPassiveListener();
                        
                        // 等待一段时间让被动监听建立
                        System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                        {
                            if (_passiveMav != null && _passiveMav.BaseStream?.IsOpen == true)
                            {
                                log.Info("Passive listener established, switching to passive port");
                                SwitchToPassivePort();
                            }
                            else
                            {
                                log.Warn("Failed to establish passive UDP listener");
                            }
                        });
                    }
                    else
                    {
                        log.Warn("No passive UDP listener available for reconnection");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error during UDP reconnect attempt", ex);
            }
            finally
            {
                _isReconnecting = false;
            }
        }


        /// <summary>
        /// 获取下一个要尝试的TCP地址
        /// </summary>
        private string GetNextTcpHost()
        {
            // 如果当前没有设置当前主机，优先尝试主地址
            if (string.IsNullOrEmpty(CurrentTcpHost))
            {
                if (!string.IsNullOrEmpty(_primaryTcpHost))
                    return _primaryTcpHost;
                else if (!string.IsNullOrEmpty(_backupTcpHost))
                    return _backupTcpHost;
            }
            
            // 如果当前使用主地址，尝试备用地址
            if (CurrentTcpHost == _primaryTcpHost && !string.IsNullOrEmpty(_backupTcpHost))
            {
                return _backupTcpHost;
            }
            
            // 如果当前使用备用地址，尝试主地址
            if (CurrentTcpHost == _backupTcpHost && !string.IsNullOrEmpty(_primaryTcpHost))
            {
                return _primaryTcpHost;
            }
            
            // 如果当前地址不在配置中，尝试主地址
            if (!string.IsNullOrEmpty(_primaryTcpHost))
            {
                return _primaryTcpHost;
            }
            
            // 最后尝试备用地址
            return _backupTcpHost;
        }

        /// <summary>
        /// 连接到指定的TCP地址
        /// </summary>
        private void ConnectToTcp(string host, string port)
        {
            try
            {
                // 断开当前连接
                if (MainV2.comPort?.BaseStream?.IsOpen == true)
                {
                    MainV2.comPort.Close();
                }

                // 创建新的TCP连接
                var tcpSerial = new MissionPlanner.Comms.TcpSerial();
                tcpSerial.Host = host;
                tcpSerial.Port = port;
                tcpSerial.autoReconnect = false; // 我们手动管理重连

                // 设置到主连接端口
                MainV2.comPort.BaseStream = tcpSerial;
                
                // 直接调用TcpSerial的Open方法，避免重复弹窗
                // 由于我们已经设置了Host和Port，不会弹出输入框
                tcpSerial.Open();

                // 更新当前TCP地址
                CurrentTcpHost = host;
                _lastSwitchUtc = DateTime.UtcNow;
                
                // 确保连接状态正确更新
                MainV2.instance.BeginInvoke((Action)delegate
                {
                    // 通过公共方法更新连接状态
                    MainV2.instance.UpdateConnectionStatus(true);
                });
                
                log.Info($"Successfully connected to {host}:{port}");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to connect to {host}:{port}", ex);
                throw;
            }
        }

        private void EvaluateQualityAndMaybeSwitch(double quality)
        {
            if (!_isEnabled || _isManualDisconnect || _isReconnecting)
                return;

            // 检查UDP连接是否断开
            if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerial || 
                MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerialConnect)
            {
                if (MainV2.comPort?.BaseStream?.IsOpen != true)
                {
                    // UDP连接断开，尝试切换到被动端口
                    log.Warn("UDP connection lost, attempting to switch to passive port");
                    AttemptUdpReconnect();
                    return;
                }
            }
            else if (MainV2.comPort?.BaseStream?.IsOpen != true)
            {
                return;
            }

            try
            {
                // 定期报告主动链路质量
                if (DateTime.Now - _lastQualityReport > TimeSpan.FromSeconds(5))
                {
                    log.Info($"Active Quality Monitor: Quality={quality:0.00}, Threshold={_qualityThreshold:0.00}");
                    _lastQualityReport = DateTime.Now;
                }

                // 如果启用双监听，并且备用链路质量更好，则主动切换
                if (EnableDualListen && _passiveMav != null)
                {
                    // 使用实际的被动质量数据
                    double passiveQuality = _passiveQuality;

                    // 详细的双端质量对比日志
                    log.Debug($"Dual Listen Quality Check - Active: {quality:0.00}, Passive: {passiveQuality:0.00}, Threshold: {_qualityThreshold:0.00}, DiffThreshold: {_qualityDifferenceThreshold:0.00}");

                    // 使用智能切换决策算法
                    if (ShouldSwitchToPassive(quality, passiveQuality))
                    {
                        if ((DateTime.UtcNow - _lastSwitchUtc).TotalSeconds >= _minSwitchIntervalSec)
                        {
                            // 判断是UDP双监听还是TCP双监听
                            if (MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerial || 
                                MainV2.comPort?.BaseStream is MissionPlanner.Comms.UdpSerialConnect)
                            {
                                // UDP双监听：切换到被动端口
                                log.Warn($"UDP Switch: {quality:0.00} -> {passiveQuality:0.00}");
                                SwitchToPassivePort();
                                return;
                            }
                            // 禁用：TCP 双监听切换逻辑，避免影响UDP场景
                            else
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        // 记录不切换的原因
                        if (quality < _qualityThreshold && passiveQuality < _qualityThreshold)
                        {
                            log.Info($"Both active ({quality:0.00}) and passive ({passiveQuality:0.00}) quality below threshold ({_qualityThreshold:0.00}) - no switch");
                        }
                        else if (passiveQuality <= quality + _qualityDifferenceThreshold)
                        {
                            log.Debug($"Passive quality ({passiveQuality:0.00}) not significantly better than active ({quality:0.00}) - no switch");
                        }
                    }
                }

                // if (quality < _qualityThreshold)
                // {
                //     if ((DateTime.UtcNow - _lastSwitchUtc).TotalSeconds < _minSwitchIntervalSec)
                //         return;

                //     string target = GetAlternateHost();
                //     if (!string.IsNullOrEmpty(target))
                //     {
                //         string port = GetPortForHost(target);
                //         log.Warn($"Link quality {quality:0.00} < threshold {_qualityThreshold:0.00}, switching to {target}:{port}");
                //         AttemptReconnect();
                //     }
                // }
            }
            catch (Exception ex)
            {
                log.Error("Error evaluating link quality", ex);
            }
        }

        private string GetAlternateHost()
        {
            if (CurrentTcpHost == _primaryTcpHost && !string.IsNullOrEmpty(_backupTcpHost)) return _backupTcpHost;
            if (CurrentTcpHost == _backupTcpHost && !string.IsNullOrEmpty(_primaryTcpHost)) return _primaryTcpHost;
            if (!string.IsNullOrEmpty(_backupTcpHost)) return _backupTcpHost;
            return _primaryTcpHost;
        }

        /// <summary>
        /// 获取被动监听的端口（用于UDP双监听切换）
        /// </summary>
        private string GetPassivePort()
        {
            try
            {
                if (_passiveMav?.BaseStream is MissionPlanner.Comms.UdpSerial udp)
                {
                    return udp.Port;
                }
                else if (_passiveMav?.BaseStream is MissionPlanner.Comms.UdpSerialConnect udpCl)
                {
                    return udpCl.Port;
                }
                else if (_passiveTcp != null)
                {
                    return _passiveTcp.Port;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 智能切换决策：选择质量更高的连接
        /// </summary>
        private bool ShouldSwitchToPassive(double activeQuality, double passiveQuality)
        {
            // 1. 如果主动质量低于阈值，被动质量高于阈值，则切换
            if (activeQuality < _qualityThreshold && passiveQuality >= _qualityThreshold)
            {
                return true;
            }

            // 2. 如果被动质量显著高于主动质量（超过差异阈值），则切换
            if (passiveQuality > activeQuality + _qualityDifferenceThreshold)
            {
                return true;
            }

            // 3. 如果主动质量很低（低于阈值的一半），被动质量相对较好，则切换
            if (activeQuality < _qualityThreshold * 0.5 && passiveQuality > _qualityThreshold * 0.7)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 切换到被动监听端口（UDP双监听模式）
        /// </summary>
        private void SwitchToPassivePort()
        {
            try
            {
                var passivePort = GetPassivePort();
                if (string.IsNullOrEmpty(passivePort))
                {
                    log.Warn("No passive port available for switching");
                    return;
                }

                log.Info($"UDP Switch to: {passivePort}");

                // 断开当前连接
                if (MainV2.comPort?.BaseStream?.IsOpen == true)
                {
                    MainV2.comPort.Close();
                }

                // 在占用被动端口作为新的主动端口之前，先关闭被动监听以释放端口
                try
                {
                    if (_passiveMav != null)
                    {
                        try { _passiveMav.Close(); } catch {}
                        try { _passiveMav.BaseStream?.Close(); } catch {}
                    }
                }
                catch {}

                // 创建新的UDP连接
                var udp = new MissionPlanner.Comms.UdpSerial();
                udp.Port = passivePort;
                udp.SuppressPrompts = true;

                // 设置到主连接端口
                MainV2.comPort.BaseStream = udp;
                udp.Open();

                _lastSwitchUtc = DateTime.UtcNow;

                // 更新连接状态
                MainV2.instance.BeginInvoke((Action)delegate
                {
                    MainV2.instance.UpdateConnectionStatus(true);
                });

                // 重新设置被动监听（现在原来的主动端口变成被动端口）
                SetupPassiveListener();

                log.Info($"UDP Switch completed: {passivePort}");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to switch to passive port", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _connectionMonitorTimer?.Dispose();
            _connectionMonitorTimer = null;
        }
    }
}
