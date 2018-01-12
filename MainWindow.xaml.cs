using Microsoft.Kinect;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        static string Data = System.IO.Directory.GetCurrentDirectory() + @"\Data";
        static string pathToRgbFolder = Data + @"\RGB";
        static string pathToDepthFolder = Data + @"\Depth";
        static string pathToInfraredFolder = Data + @"\Infrared";

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
                        camera.Source = frame.ToBitmap(pathToRgbFolder, _labelMode, isPlaying);
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
                        camera.Source = frame.ToBitmap(pathToDepthFolder, _labelMode, isPlaying);
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
                        camera.Source = frame.ToBitmap(pathToInfraredFolder, _labelMode, isPlaying);
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
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Depth;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Infrared;
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
        private void Play_Click(object sender, RoutedEventArgs e) {
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
    }

    
    public enum Mode
    {
        Color,
        Depth,
        Infrared
    }

    
}
