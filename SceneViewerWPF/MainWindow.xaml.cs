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
using xn;
using Point3D = System.Windows.Media.Media3D.Point3D;

namespace SceneViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private class UserModel
        {
            public ModelVisual3D Model { get; private set; }
            private readonly SphereVisual3D _centerOfMass;

            private readonly Dictionary<SkeletonJoint, SphereVisual3D> _joints 
                = new Dictionary<SkeletonJoint, SphereVisual3D>();

            private readonly Dictionary<Tuple<SkeletonJoint, SkeletonJoint>, PipeVisual3D> _lines
                = new Dictionary<Tuple<SkeletonJoint, SkeletonJoint>, PipeVisual3D>();

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

                _centerOfMass = new SphereVisual3D
                                    {
                                        Center = new Point3D(0, 0, 0),
                                        Material = Materials.Yellow,
                                        Radius = 25,
                                        PhiDiv = 15,
                                        ThetaDiv = 30,
                                        Transform = new TranslateTransform3D()
                                    };

                group.Children.Add(_centerOfMass);

                Model = group;
            }


            public void Update(KinectUserInfo user)
            {
                // TODO: optimize
                var trans = ((TranslateTransform3D)_centerOfMass.Transform);
                trans.OffsetX = user.CenterOfMass.X;
                trans.OffsetY = user.CenterOfMass.Y;
                trans.OffsetZ = user.CenterOfMass.Z;

                if (user.IsCalibrating)
                    _centerOfMass.Material = Materials.Red;
                else if (user.IsTracking)
                    _centerOfMass.Material = Materials.Green;
                else
                    _centerOfMass.Material = Materials.Yellow;

                if (user.IsTracking)
                {
                    GetJoint(user, SkeletonJoint.Head);
                    GetJoint(user, SkeletonJoint.Neck);

                    GetJoint(user, SkeletonJoint.LeftShoulder);
                    GetJoint(user, SkeletonJoint.LeftElbow);
                    GetJoint(user, SkeletonJoint.LeftHand);

                    GetJoint(user, SkeletonJoint.RightShoulder);
                    GetJoint(user, SkeletonJoint.RightElbow);
                    GetJoint(user, SkeletonJoint.RightHand);

                    GetJoint(user, SkeletonJoint.Torso);

                    GetJoint(user, SkeletonJoint.LeftHip);
                    GetJoint(user, SkeletonJoint.LeftKnee);
                    GetJoint(user, SkeletonJoint.LeftFoot);

                    GetJoint(user, SkeletonJoint.RightHip);
                    GetJoint(user, SkeletonJoint.RightKnee);
                    GetJoint(user, SkeletonJoint.RightFoot);

                    DrawLine(user, SkeletonJoint.Head, SkeletonJoint.Neck);

                    DrawLine(user, SkeletonJoint.LeftShoulder, SkeletonJoint.Torso);
                    DrawLine(user, SkeletonJoint.RightShoulder, SkeletonJoint.Torso);

                    DrawLine(user, SkeletonJoint.Neck, SkeletonJoint.LeftShoulder);
                    DrawLine(user, SkeletonJoint.LeftShoulder, SkeletonJoint.LeftElbow);
                    DrawLine(user, SkeletonJoint.LeftElbow, SkeletonJoint.LeftHand);

                    DrawLine(user, SkeletonJoint.Neck, SkeletonJoint.RightShoulder);
                    DrawLine(user, SkeletonJoint.RightShoulder, SkeletonJoint.RightElbow);
                    DrawLine(user, SkeletonJoint.RightElbow, SkeletonJoint.RightHand);

                    DrawLine(user, SkeletonJoint.LeftHip, SkeletonJoint.Torso);
                    DrawLine(user, SkeletonJoint.RightHip, SkeletonJoint.Torso);
                    DrawLine(user, SkeletonJoint.LeftHip, SkeletonJoint.RightHip);

                    DrawLine(user, SkeletonJoint.LeftHip, SkeletonJoint.LeftKnee);
                    DrawLine(user, SkeletonJoint.LeftKnee, SkeletonJoint.LeftFoot);

                    DrawLine(user, SkeletonJoint.RightHip, SkeletonJoint.RightKnee);
                    DrawLine(user, SkeletonJoint.RightKnee, SkeletonJoint.RightFoot);
                }
            }

            private void DrawLine(KinectUserInfo user, SkeletonJoint p1, SkeletonJoint p2)
            {
                PipeVisual3D lineVisual = null;
                bool isVisible = false;

                SkeletonJointPosition p1Data, p2Data;
                if (user.IsTracking &&
                    user.Joints.TryGetValue(p1, out p1Data) && 
                    user.Joints.TryGetValue(p2, out p2Data))
                {
                    if (p1Data.fConfidence != 0 && p2Data.fConfidence != 0)
                    {
                        isVisible = true;

                        var key = new Tuple<SkeletonJoint, SkeletonJoint>(p1, p2);
                        if (_lines.TryGetValue(key, out lineVisual) == false)
                        {
                            lineVisual = new PipeVisual3D
                                             {
                                                 Material = Materials.Gold, 
                                                 Diameter = 8, 
                                                 ThetaDiv = 18
                                             };

                            _lines[key] = lineVisual;
                        }

                        // update position
                        lineVisual.Point1 = p1Data.position.ToPoint3D();
                        lineVisual.Point2 = p2Data.position.ToPoint3D();
                    }
                }

                // remove visual
                if (lineVisual != null)
                {
                    if (isVisible)
                    {
                        if (!Model.Children.Contains(lineVisual))
                            Model.Children.Add(lineVisual);
                    }
                    else
                    {
                        Model.Children.Remove(lineVisual);
                    }
                }
            }

            private void GetJoint(KinectUserInfo user, SkeletonJoint joint)
            {
                SphereVisual3D jointVisual = null;

                SkeletonJointPosition jointData;
                if (user.IsTracking && user.Joints.TryGetValue(joint, out jointData))
                {
                    if (jointData.fConfidence > 0)
                    {
                        if (_joints.TryGetValue(joint, out jointVisual) == false)
                        {
                            // joints are indicated by smaller blue balls
                            jointVisual = new SphereVisual3D
                                              {
                                                  Center = new Point3D(0, 0, 0),
                                                  Material = Materials.Blue,
                                                  Radius = 16, 
                                                  PhiDiv = 15,
                                                  ThetaDiv = 30,
                                                  Transform = new TranslateTransform3D()
                                              };

                            _joints[joint] = jointVisual;
                        }
                    
                        SetTransformFromPoint(jointVisual, jointData.position);

                        if (!Model.Children.Contains(jointVisual))
                            Model.Children.Add(jointVisual);

                        return;
                    }
                }

                if (jointVisual != null)
                {
                    if (Model.Children.Contains(jointVisual))
                        Model.Children.Remove(jointVisual);
                }
            }

            private void SetTransformFromPoint(Visual3D visual3D, xn.Point3D point)
            {
                var trans = ((TranslateTransform3D)visual3D.Transform);
                trans.OffsetX = point.X;
                trans.OffsetY = point.Y;
                trans.OffsetZ = point.Z;
            }
        }

        private KinectTracker _kinectTracker;
        private D3DImageSlimDX _dxImageContainer;
        private readonly Stopwatch _timer = new Stopwatch();
        private readonly FrameCounter _frameCounter = new FrameCounter();
        private DxScene _dxScene;

        private readonly List<UserModel> _users = new List<UserModel>();
        private PlaneVisual3D _floorVisual;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            _floorVisual = new PlaneVisual3D();
            _floorVisual.Width = _floorVisual.Length = 10/KinectToHelixScale;
            _floorVisual.BackMaterial = Materials.DarkGray;
            _floorVisual.Material = (Material) FindResource("FloorMaterial");
            _floorVisual.Transform = new MatrixTransform3D(GetKinectToHelixTransform().ToMatrix3D());

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
            var frame = tracker.CurrentFrame;

            var invisibleUsers = new List<UserModel>(_users);
            foreach (var user in frame.Users)
            {
                // update user model
                var model = _users.FirstOrDefault(u => u.Id == user.Id);
                if (model == null)
                {
                    model = new UserModel(user.Id);
                    model.World = GetKinectToHelixTransform().ToMatrix3D();
                    _users.Add(model);
                }
                else
                {
                    invisibleUsers.Remove(model);
                }
                model.Update(user);

                // show in view
                if (!helixView.Children.Contains(model.Model))
                    helixView.Children.Add(model.Model);
            }

            foreach (var model in invisibleUsers)
            {
                helixView.Children.Remove(model.Model);
                _users.Remove(model);
            }

            if (frame.Floor != null)
            {
                var plane = frame.Floor.Value;
                _floorVisual.Origin = new Point3D(plane.ptPoint.X, plane.ptPoint.Y, plane.ptPoint.Z);
                _floorVisual.Normal = new Vector3D(plane.vNormal.X, plane.vNormal.Y, plane.vNormal.Z);

                if (!helixView.Children.Contains(_floorVisual))
                    helixView.Children.Add(_floorVisual);
            }
            else
            {
                if (helixView.Children.Contains(_floorVisual))
                    helixView.Children.Remove(_floorVisual);
            }

            if (freezeUpdates.IsChecked != true)
            {
                _dxScene.PointsCloudRenderer.Update(frame, tracker.CameraInfo);
            }
        }

        private void OnKinectTrackingCompleted(object sender, EventArgs e)
        {
            busyIndicator.IsBusy = false;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
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

        const double KinectToHelixScale = 1.0 / 1000.0;

        private static SlimDX.Matrix GetKinectToHelixTransform()
        {
            var scale = (float)KinectToHelixScale;
            var world = SlimDX.Matrix.Scaling(scale, scale, scale);
            world *= SlimDX.Matrix.RotationZ(D3DExtensions.DegreeToRadian(-90));
            world *= SlimDX.Matrix.RotationY(D3DExtensions.DegreeToRadian(-90));

            return world;
        }

        private void UpdateRenderProperties()
        {
            //if (renderProps.Scale != null)
            //    _dxScene.PointsCloud.Scale = renderProps.Scale.Value;

            _dxScene.PointsCloud.World = GetKinectToHelixTransform();

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
