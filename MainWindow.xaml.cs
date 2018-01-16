using Microsoft.Kinect;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

using Button =  System.Windows.Controls.Button;
namespace KinectStreams
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        
        #region Members

        Mode _mode = Mode.Depth;
        LabelMode _labelMode = LabelMode.NONE;
        Boolean isPlaying = false;
        Boolean fileLocation = false;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;

        bool _displayBody = false;

        /*
         * TO MAKE IT EXE: 
         * 1. Change it from Debug to Release( dropdown close to green Start button, not AnyCPU part)
         * 2. Got to (Task Bar) Build-> Build Solution

        /***
         * TODO Syldawg change folder location.
         * NOTE!!!! You MUST put in A 'Wash' 'Contact' and 'None' subfolders
         **/
        // Images being saved in Release folders, e.g. Debug/Data/... 

        static string data = System.IO.Directory.GetCurrentDirectory() + @"\Data";
        static string pathToRgbFolder = data + @"\RGB";
        static string pathToDepthFolder = data + @"\Depth";
        static string pathToInfraredFolder = data + @"\Infrared";

        static int rgbSesh = 0;
        static int depthSesh = 0;
        static int infraSesh = 0;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
            
            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Color)
                    {
                        camera.Source = frame.ToBitmap(pathToRgbFolder, _labelMode, isPlaying, rgbSesh);
                    }
                }
            }

            // Depth
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Depth)
                    {
                        camera.Source = frame.ToBitmap(pathToDepthFolder, _labelMode, isPlaying, depthSesh);
                    }
                }
            }

            // Infrared
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Infrared)
                    {
                        camera.Source = frame.ToBitmap(pathToInfraredFolder, _labelMode, isPlaying, infraSesh);
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                // Draw skeleton.
                                if (_displayBody)
                                {
                                    canvas.DrawSkeleton(body);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Color;
            ResetButtonColors(new Button[] { btn_Depth, btn_Infra });
            btn_Color.Background = Brushes.LightSkyBlue;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Depth;
            ResetButtonColors(new Button[] { btn_Color, btn_Infra });
            btn_Depth.Background = Brushes.LightSkyBlue;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Infrared;
            ResetButtonColors(new Button[] { btn_Color, btn_Depth });
            btn_Infra.Background = Brushes.LightSkyBlue;
        }

        private void Body_Click(object sender, RoutedEventArgs e)
        {
            _displayBody = !_displayBody;
        }

        private void Wash_Click(object sender, RoutedEventArgs e)
        {
            _labelMode = LabelMode.WASH;

            ResetButtonColors(new Button[] { btn_Contact, btn_Neither });
           
            Button button = sender as Button;
            button.Background = Brushes.LightGreen;

        }

        private void Contact_Click(object sender, RoutedEventArgs e)
        {
            _labelMode = LabelMode.CONTACT;

            ResetButtonColors(new Button[] { btn_Neither, btn_Wash });

            Button button = sender as Button;
            button.Background = Brushes.LightGreen;
        }

        private void None_Click(object sender, RoutedEventArgs e)
        {
            _labelMode = LabelMode.NONE;

            ResetButtonColors(new Button[] { btn_Wash, btn_Contact });

            Button button = sender as Button;
            button.Background = Brushes.LightGreen;
        }

        #region Key Binding
        public void Contact_Method(Object sender, ExecutedRoutedEventArgs e)
        {
            _labelMode = LabelMode.CONTACT;

            ResetButtonColors(new Button[] { btn_Wash, btn_Neither });
            btn_Contact.Background = Brushes.LightGreen;
        }

        public void Wash_Method(Object sender, ExecutedRoutedEventArgs e)
        {
            _labelMode = LabelMode.WASH;

            ResetButtonColors(new Button[] { btn_Contact, btn_Neither });
            btn_Wash.Background = Brushes.LightGreen;
        }

        public void Neither_Method(Object sender, ExecutedRoutedEventArgs e)
        {
            _labelMode = LabelMode.NONE;

            ResetButtonColors(new Button[] { btn_Wash, btn_Contact });
            btn_Neither.Background = Brushes.LightGreen;
        }

        public void Color_Method(Object sender, ExecutedRoutedEventArgs e)
        {
            _mode = Mode.Color;
            ResetButtonColors(new Button[] { btn_Depth, btn_Infra });
            btn_Color.Background = Brushes.LightSkyBlue;
        }

        public void Depth_Method(Object sender, ExecutedRoutedEventArgs e)
        {
            _mode = Mode.Depth;
            ResetButtonColors(new Button[] { btn_Color, btn_Infra });
            btn_Depth.Background = Brushes.LightSkyBlue;
        }

        public void Infra_Method(Object sender, ExecutedRoutedEventArgs e)
        {
            _mode = Mode.Infrared;
            ResetButtonColors(new Button[] { btn_Color, btn_Depth });
            btn_Infra.Background = Brushes.LightSkyBlue;
        }
        #endregion


        private void Play_Click(object sender, RoutedEventArgs e) {
            if(!fileLocation)
            {
                if(!savePrompt())
                {
                    System.Windows.Forms.MessageBox.Show("Save directory not specified.", "Error Title", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            isPlaying = true;

            ResetButtonColors(new Button[] { pause });

            Button button = sender as Button;
            button.Background = Brushes.DarkRed;
        }

        private void Pause_Click(object sender, RoutedEventArgs e) {
            isPlaying = false;

            ResetButtonColors(new Button[] { play});

            Button button = sender as Button;
            button.Background = Brushes.DarkRed;

            switch(_mode)
            {
                case Mode.Color:
                    rgbSesh++;
                    break;
                case Mode.Depth:
                    depthSesh++;
                    break;
                case Mode.Infrared:
                    infraSesh++;
                    break;
                
            }

        }
        #endregion

        #region Save dialog region
        private void Save_Click(object sender, System.EventArgs e)
        {
            savePrompt();
        }
        #endregion

        #region Helper Functions
        public void ResetButtonColors(Button[] includedButtons)
        {
            foreach(Button btn in includedButtons)
            {
                btn.Background = Brushes.White;
            }
        }
        #endregion

        private Boolean savePrompt()
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    data = fbd.SelectedPath;
                    pathToRgbFolder = data + @"\RGB";
                    pathToDepthFolder = data + @"\Depth";
                    pathToInfraredFolder = data + @"\Infrared";
                }
                else
                {
                    System.Windows.Forms.DialogResult dr = System.Windows.Forms.MessageBox.Show("Do you want to save files in defualt folder?",
                      "Save File", MessageBoxButtons.YesNo);
                    switch (dr)
                    {
                        case System.Windows.Forms.DialogResult.Yes: break;
                        case System.Windows.Forms.DialogResult.No: return fileLocation;
                    }
                }

                fileLocation = true;
            }

            // Find the last session number 
            if(Directory.Exists(pathToRgbFolder))
            {
                var rgb = Directory.GetDirectories(pathToRgbFolder).OrderByDescending(filename => filename);
                if (!MissingExtensions.IsNullOrEmpty(rgb))
                    rgbSesh = extractNum(rgb.First()) + 1;
                
            }
            if (Directory.Exists(pathToDepthFolder))
            {
                var depth = Directory.GetDirectories(pathToDepthFolder).OrderByDescending(filename => filename);
                if (!MissingExtensions.IsNullOrEmpty(depth))
                    depthSesh = extractNum(depth.First()) + 1;
            }

            if(Directory.Exists(pathToInfraredFolder))
            {
                var infra = Directory.GetDirectories(pathToInfraredFolder).OrderByDescending(filename => filename);
                if (!MissingExtensions.IsNullOrEmpty(infra))
                    infraSesh = extractNum(infra.First()) + 1;
            }

            return fileLocation;
        }

        public int extractNum(string input)
        {
            var stack = new Stack<char>();

            for (var i = input.Length - 1; i >= 0; i--)
            {
                if (!char.IsNumber(input[i]))
                {
                    break;
                }

                stack.Push(input[i]);
            }

            var result = new string(stack.ToArray());

            return Convert.ToInt32(result);
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }
    }

    // Generic class for extension
    public static class MissingExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }
    }

    public enum Mode
    {
        Color,
        Depth,
        Infrared
    }

    
}
