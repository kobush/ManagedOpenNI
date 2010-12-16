using System;
using System.Windows;

namespace NiSimpleViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (sender, args) => InitTracker();
        }

        private KinectTracker _tracker;

        private void InitTracker()
        {
            _tracker = new KinectTracker();
            _tracker.UpdateViewPort += OnTracker_UpdateViewPort;
            _tracker.TrackinkgCompleted += OnTracker_TrackinkgCompleted;
            _tracker.StartTracking();
        }

        void OnTracker_TrackinkgCompleted(object sender, EventArgs e)
        {
            image.Source = null;
            depth.Source = null;
            scene.Source = null;
        }

        void OnTracker_UpdateViewPort(object sender, EventArgs e)
        {
            waitText.Visibility = Visibility.Collapsed;

            image.Source = _tracker.RgbImageSource;
            depth.Source = _tracker.DepthImageSource;
            scene.Source = _tracker.SceneImageSource;
            fpsText.Text = _tracker.FramesPerSecond.ToString("F1");
        }
    }
}
