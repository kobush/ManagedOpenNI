using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit;
using NiSimpleViewerWPF;
using SlimDX;
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

            cameraProps.SelectedCameraModeChanged += OnSelectedCameraModeChanged;
            renderProps.SelectedRenderTechChanged += OnSelectedRenderTechChanged;
            renderProps.RenderPropertyChanged += OnRendererPropertyChanged;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;

            _dxImageContainer = new D3DImageSlimDX();
            _dxImageContainer.IsFrontBufferAvailableChanged += _D3DImageContainer_IsFrontBufferAvailableChanged;

            dxImage.Source = _dxImageContainer;

            _dxScene = new DxScene();
            _dxImageContainer.SetBackBufferSlimDX(_dxScene.SharedTexture);

            renderProps.Scale = _dxScene.PointsCloudRenderer.Scale;

            // init camera
            helixView.Camera.Position = new Point3D(0,25,-100);
            helixView.Camera.LookDirection = new Point3D(0, 0, 100) - helixView.Camera.Position;
            helixView.Camera.UpDirection = new Vector3D(0,1,0);
            helixView.Camera.FarPlaneDistance = 2000; // this is about 20 meters
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
            busyIndicator.IsBusy = false;

            if (_dxScene == null) return;
            var tracker = ((KinectTracker) sender);
            _dxScene.PointsCloudRenderer.Init(tracker.CurrentFrame, tracker.CameraInfo);
        }

        private void OnKinectTrackingUpdated(object sender, EventArgs e)
        {
            if (_dxScene == null) return;

            if (freezeUpdates.IsChecked != true)
            {
                var tracker = ((KinectTracker)sender);
                _dxScene.PointsCloudRenderer.Update(tracker.CurrentFrame, tracker.CameraInfo);
            }
        }

        private void OnKinectTrackingCompleted(object sender, EventArgs e)
        {
            busyIndicator.IsBusy = false;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_kinectTracker != null)
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

            _dxScene.Wireframe = renderProps.Wireframe;

            SlimDX.Direct3D10.Texture2D lastTexture = _dxScene.SharedTexture;

            _dxScene.Render((float) time.TotalSeconds, 
                (int)helixView.ActualWidth, (int)helixView.ActualHeight);

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

            cameraProps.UpdateCamera(camera);

            if (_dxScene != null)
            {
                // sync view with position of the WPF camera from HelixView
                _dxScene.Camera.SetFromWpfCamera(camera);
            }
        }

        private void OnSelectedCameraModeChanged(object sender, EventArgs e)
        {
            helixView.CameraMode = cameraProps.SelectedCameraMode;
        }

        private void OnSelectedRenderTechChanged(object sender, EventArgs e)
        {
            _dxScene.CrateKinectPointsRenderer(renderProps.SelectedRenderTech);
        }

        private void OnRendererPropertyChanged(object sender, EventArgs e)
        {
            if (renderProps.Scale != null)
                _dxScene.PointsCloudRenderer.Scale = renderProps.Scale.Value;

            _dxScene.PointsCloudRenderer.FillColor =
                new Vector4(renderProps.FillColor.ScR,
                            renderProps.FillColor.ScG,
                            renderProps.FillColor.ScB,
                            (float) (1.0 - renderProps.TextureAlpha));
        }
    }
}
