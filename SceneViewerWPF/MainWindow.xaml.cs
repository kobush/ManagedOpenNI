using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit;
using NiSimpleViewerWPF;
using SlimDX;
using SlimDX.Windows;
using Matrix = System.Windows.Media.Matrix;

namespace SceneViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class UserModel
        {
            public ModelVisual3D Model { get; private set; }
            private readonly SphereVisual3D _centerOfMass;

            public Matrix3D World
            {
                get { return ((MatrixTransform3D) Model.Transform).Matrix; }
                set { ((MatrixTransform3D) Model.Transform).Matrix = value; }
            }

            public int Id { get; private set; }
            
            public UserModel(int id)
            {
                Id = id;

                var group = new ModelVisual3D();
                group.Transform = new MatrixTransform3D();

                _centerOfMass = new SphereVisual3D();
                _centerOfMass.Center = new Point3D(0,0,0);
                _centerOfMass.Material = Materials.Yellow;
                _centerOfMass.Radius = 25; // about 5cm diameter
                _centerOfMass.Transform = new TranslateTransform3D();

                group.Children.Add(_centerOfMass);

                Model = group;
            }


            public void Update(KinectUserInfo userInfo)
            {
                // TODO: optimize
                var trans = ((TranslateTransform3D)_centerOfMass.Transform);
                trans.OffsetX = userInfo.CenterOfMass.X;
                trans.OffsetY = -userInfo.CenterOfMass.Y;
                trans.OffsetZ = userInfo.CenterOfMass.Z;
            }
        }

        private KinectTracker _kinectTracker;
        private D3DImageSlimDX _dxImageContainer;
        private readonly Stopwatch _timer = new Stopwatch();
        private readonly FrameCounter _frameCounter = new FrameCounter();
        private DxScene _dxScene;

        private readonly List<UserModel> _users = new List<UserModel>();
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            cameraProps.SelectedCameraModeChanged += OnSelectedCameraModeChanged;
            renderProps.SelectedRenderTechChanged += OnSelectedRenderTechChanged;
            renderProps.RenderPropertyChanged += OnRendererPropertyChanged;
            lightProps.ViewModel.PropertyChanged += OnLightPropertyChanged;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;

            _dxImageContainer = new D3DImageSlimDX();
            _dxImageContainer.IsFrontBufferAvailableChanged += _D3DImageContainer_IsFrontBufferAvailableChanged;

            dxImage.Source = _dxImageContainer;

            _dxScene = new DxScene();
            _dxImageContainer.SetBackBufferSlimDX(_dxScene.SharedTexture);

            // position camera 1m and slightly above kinect
            helixView.Camera.Position = new Point3D(1, 0, 0.25);
            // look at point 1m in front camera
            helixView.Camera.LookDirection = new Point3D(-1, 0, 0) - helixView.Camera.Position;
            // set 1km depth of view
            helixView.Camera.FarPlaneDistance = 1000;  
            helixView.CameraChanged += delegate { UpdateCameraPosition(); };
            UpdateCameraPosition();

            UpdateRenderProperties();
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

            var tracker = ((KinectTracker)sender);

            foreach (var user in tracker.CurrentFrame.Users)
            {
                // update user model
                var model = _users.FirstOrDefault(u => u.Id == user.Id);
                if (model == null)
                {
                    model = new UserModel(user.Id);
                    _users.Add(model);

                    // show in view
                    helixView.Children.Add(model.Model);
                }
                model.Update(user);
                model.World = GetKinectToHelixMatix().ToMatrix3D();
                //TODO: remove not visible models
            }

            if (freezeUpdates.IsChecked != true)
            {
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

        private void UpdateCameraPosition()
        {
            var camera = (PerspectiveCamera) helixView.Camera;

            cameraProps.UpdateCamera(camera);

            if (lightProps.ViewModel.Headlight)
            {
                lightProps.ViewModel.Position = camera.Position;
                lightProps.ViewModel.Direction = camera.LookDirection;
            }

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
            UpdateRenderProperties();
        }

        private SlimDX.Matrix GetKinectToHelixMatix()
        {
            var scale = 1/1000f;
            var world = SlimDX.Matrix.Scaling(scale, -scale, scale);
            world *= SlimDX.Matrix.RotationZ(D3DExtensions.DegreeToRadian(-90));
            world *= SlimDX.Matrix.RotationY(D3DExtensions.DegreeToRadian(-90));

            return world;
        }

        private void UpdateRenderProperties()
        {
            //if (renderProps.Scale != null)
            //    _dxScene.PointsCloud.Scale = renderProps.Scale.Value;

            _dxScene.PointsCloud.World = GetKinectToHelixMatix();

            _dxScene.PointsCloud.FillColor =
                new Vector4(renderProps.FillColor.ScR,
                            renderProps.FillColor.ScG,
                            renderProps.FillColor.ScB,
                            (float) (1.0 - renderProps.TextureAlpha));

            _dxScene.PointsCloud.UserAlpha = (float) renderProps.UserAlpha;
            _dxScene.PointsCloud.BackgroundAlpha = (float) renderProps.BackgroundAlpha;
        }

        private void OnLightPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (_dxScene != null)
            {
                _dxScene.PointsCloud.Light = lightProps.ViewModel.GetLight();
            }
        }
    }
}
