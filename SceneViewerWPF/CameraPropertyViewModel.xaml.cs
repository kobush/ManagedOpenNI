using System;
using System.Windows.Media.Media3D;
using HelixToolkit;

namespace SceneViewerWPF
{
    public class CameraPropertyViewModel : ViewModelBase
    {
        private Vector3D _lookDirection;
        private Point3D _eyePosition;
        private Vector3D _upDirection;
        private double _nearPlane;
        private double _farPlane;
        private double _fieldOfView;

        private CameraMode _selectedCameraMode;
        private CameraPositionMode _selectedCameraPosition;

        public CameraMode SelectedCameraMode
        {
            get { return _selectedCameraMode; }
            set
            {
                if (_selectedCameraMode != value)
                {
                    _selectedCameraMode = value;

                    RaisePropertyChanged("SelectedCameraMode");
                    InvokeSelectedCameraModeChanged();
                }
            }
        }

        public event EventHandler SelectedCameraModeChanged;

        private void InvokeSelectedCameraModeChanged()
        {
            EventHandler handler = SelectedCameraModeChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public CameraPositionMode SelectedCameraPosition
        {
            get { return _selectedCameraPosition; }
            set
            {
                if (_selectedCameraPosition != value)
                {
                    _selectedCameraPosition = value;
                    RaisePropertyChanged("SelectedCameraPosition");
                    InvokeSelectedCameraPositionChanged();
                }
            }
        }

        public event EventHandler SelectedCameraPositionChanged;

        public void InvokeSelectedCameraPositionChanged()
        {
            EventHandler handler = SelectedCameraPositionChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public Vector3D LookDirection
        {
            get { return _lookDirection; }
            set
            {
                _lookDirection = value;
                RaisePropertyChanged("LookDirection");
                RaisePropertyChanged("LookDirectionText");
            }
        }

        public string LookDirectionText
        {
            get { return LookDirection.Format("f2"); }
        }


        public Point3D EyePosition
        {
            get { return _eyePosition; }
            set 
            {
                _eyePosition = value;
                RaisePropertyChanged("EyePosition");
                RaisePropertyChanged("EyePositionText");
            }
        }

        public string EyePositionText
        {
            get { return EyePosition.Format("f2"); }
        }


        public Vector3D UpDirection
        {
            get { return _upDirection; }
            set
            {
                _upDirection = value;
                RaisePropertyChanged("UpDirection");
                RaisePropertyChanged("UpDirectionText");
            }
        }

        public string UpDirectionText
        {
            get { return UpDirection.Format("f2"); }
        }

        public double NearPlane
        {
            get {
                return _nearPlane;
            }
            set {
                _nearPlane = value;
                RaisePropertyChanged("NearPlane");
            }
        }

        public double FarPlane
        {
            get {
                return _farPlane;
            }
            set {
                _farPlane = value;
                RaisePropertyChanged("FarPlane");
            }
        }

        public double FieldOfView
        {
            get {
                return _fieldOfView;
            }
            set
            {
                _fieldOfView = value;
                RaisePropertyChanged("FieldOfView");
            }
        }

        public void UpdateFromCamera(PerspectiveCamera camera)
        {
            LookDirection = camera.LookDirection;
            EyePosition = camera.Position;
            UpDirection = camera.UpDirection;
            NearPlane = camera.NearPlaneDistance;
            FarPlane = camera.FarPlaneDistance;
            FieldOfView = camera.FieldOfView;
        }
    }
}