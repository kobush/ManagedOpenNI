using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

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
        private double? _ratioMin;
        private double? _ratioMax;

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
            handImage.Source = null;
        }

        void OnTracker_UpdateViewPort(object sender, EventArgs e)
        {
            waitText.Visibility = Visibility.Collapsed;

            image.Source = _tracker.RgbImageSource;
            depth.Source = _tracker.DepthImageSource;
            scene.Source = _tracker.SceneImageSource;
            handImage.Source = _tracker.HandImageSource;
            fpsText.Text = _tracker.FramesPerSecond.ToString("F1");


            // update hand detector state
            var hand = _tracker.HandsDetector.Hands.FirstOrDefault();
            if (hand != null)
            {
                activeHand.Text = hand.Id.ToString();

                var pos = hand.ProjectedPosition;
                handPosition.Text = string.Format("{0:f1}, {1:f1}, {2:f1}",
                                                  pos.X, pos.Y, pos.Z);

                handHullArea.Text = hand.HullArea.ToString("f3");
                handBlobArea.Text = hand.BlobArea.ToString("f3");

                double? ratio = null;
                if (hand.BlobArea > 0 && hand.HullArea > 0)
                {
                    ratio = hand.BlobArea/hand.HullArea;
                    if (_ratioMin == null || ratio < _ratioMin)
                        _ratioMin = ratio;
                    if (_ratioMax == null || ratio > _ratioMax)
                        _ratioMax = ratio;
                }

                if (ratio != null)
                    handAreaRatio.Text = ratio.Value.ToString("P");
                if (_ratioMin.HasValue)
                    handAreaRatioMin.Text = _ratioMin.Value.ToString("P");
                if (_ratioMax.HasValue)
                    handAreaRatioMax.Text = _ratioMax.Value.ToString("P");

                var threshold = thresholdSlider.Value / 100.0;
                thresholdValue.Text = threshold.ToString("P");

                if (ratio == null)
                {
                    answer.Text = "";
                }
                else
                {
                    if (ratio.Value > threshold)
                    {
                        answer.Text = "closed";
                        answer.Foreground = Brushes.Red;
                    }
                    else
                    {
                        answer.Text = "open";
                        answer.Foreground = Brushes.Green;
                    }
                }
            }
        }

        private void ResetRatioClick(object sender, RoutedEventArgs e)
        {
            _ratioMin = null;
            _ratioMax = null;
        }
    }
}
