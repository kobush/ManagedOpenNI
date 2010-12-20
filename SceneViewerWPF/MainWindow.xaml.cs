using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit;
using NiSimpleViewerWPF;
using SlimDX.Windows;

namespace SceneViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectTracker _kinectTracker;
        private D3DImageSlimDX _dxImageContainer;
        private Stopwatch _timer = new Stopwatch();
        private FrameCounter _frameCounter = new FrameCounter();
        private DxScene _dxScene;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _dxImageContainer = new D3DImageSlimDX();
            _dxImageContainer.IsFrontBufferAvailableChanged += _D3DImageContainer_IsFrontBufferAvailableChanged;

            dxImage.Source = _dxImageContainer;

            _dxScene = new DxScene();
            _dxImageContainer.SetBackBufferSlimDX(_dxScene.SharedTexture);

            helixView.Camera.Position = new Point3D(0,25,-100);
            helixView.Camera.LookDirection = new Point3D(0, 0, 100) - helixView.Camera.Position;
            helixView.Camera.UpDirection = new Vector3D(0,1,0);
            helixView.CameraChanged += delegate { UpdateCameraDisplay(); };
            UpdateCameraDisplay();

            BeginRenderingScene();

            // setup tracker
            _kinectTracker = new KinectTracker();
            _kinectTracker.TrackinkgStarted += OnKinectTrackinkgStarted;
            _kinectTracker.TrackingUpdated += OnKinectTrackingUpdated;
            _kinectTracker.TrackinkgCompleted += OnKinectTrackingCompleted;
            _kinectTracker.StartTracking();
        }

        void OnKinectTrackinkgStarted(object sender, EventArgs e)
        {
            if (_dxScene == null) return;
            _dxScene.PointsCloud.Init(((KinectTracker)sender).CurrentData);
        }

        private void OnKinectTrackingUpdated(object sender, EventArgs e)
        {
            if (_dxScene == null) return;
            _dxScene.PointsCloud.Update(((KinectTracker)sender).CurrentData);
        }

        private void OnKinectTrackingCompleted(object sender, EventArgs e)
        {
            
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _kinectTracker.StopTracking();

            StopRenderingScene();

            if (_dxScene != null)
            {
                _dxScene.Dispose();
                _dxScene = null;
            }

            if (_dxImageContainer != null)
            {
                _dxImageContainer.Dispose();
                _dxImageContainer = null;
            }
        }

        void _D3DImageContainer_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // This fires when the screensaver kicks in, the machine goes into sleep or hibernate
            // and any other catastrophic losses of the d3d device from WPF's point of view
            if (_dxImageContainer.IsFrontBufferAvailable)
            {
                BeginRenderingScene();
            }
            else
            {
                StopRenderingScene();
            }
        }

        private void BeginRenderingScene()
        {
            if (_dxImageContainer.IsFrontBufferAvailable)
            {
                SlimDX.Direct3D10.Texture2D texture = _dxScene.SharedTexture;
                _dxImageContainer.SetBackBufferSlimDX(texture);

                CompositionTarget.Rendering += OnRendering;

                _timer.Start();
            }
        }

        private void StopRenderingScene()
        {
            _timer.Stop();
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            var re = (RenderingEventArgs)e;
            var time = re.RenderingTime;

            SlimDX.Direct3D10.Texture2D lastTexture = _dxScene.SharedTexture;

            _dxScene.Render(time.Milliseconds, (int)helixView.ActualWidth, (int)helixView.ActualHeight);

            // output buffer could change because of size change
            if (lastTexture != _dxScene.SharedTexture)
            {
                _dxImageContainer.SetBackBufferSlimDX(_dxScene.SharedTexture);
            }

            _dxImageContainer.InvalidateD3DImage();

            _frameCounter.AddFrame();
            textFrameRate.Text = _frameCounter.FramesPerSecond.ToString("f1");
        }

        private void UpdateCameraDisplay()
        {
            var camera = (PerspectiveCamera) helixView.Camera;

            cameraDir.Text = camera.LookDirection.Format("f1");
            cameraEye.Text = camera.Position.Format("f1");
            cameraUp.Text = camera.UpDirection.Format("f1");

            if (_dxScene != null)
            {
                // sync view with position of the WPF camera from HelixView
                _dxScene.Camera.SetFromWpfCamera(camera);
            }
        }

        private void D3DImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void OnCameraModeSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem)cameraMode.SelectedItem;
            helixView.CameraMode = (CameraMode) item.Tag;
        }
    }
}
