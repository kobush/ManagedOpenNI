using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SlimDX;

namespace SceneViewerWPF
{
    public class ViewModelLocator
    {
        static LightPropertyViewModel _lightPropertyViewModel;

        public LightPropertyViewModel LightPropertyViewModel
        {
            get
            {
                if (_lightPropertyViewModel == null)
                    _lightPropertyViewModel = new LightPropertyViewModel();

                return _lightPropertyViewModel;
            }
        }
    }

    public class LightPropertyViewModel : INotifyPropertyChanged
    {
        private Point3D _position = new Point3D(0, 0, 0f);
        private Vector3D _dir = new Vector3D(0, 0, 1);
        private Color _ambient = Color.FromScRgb(1f, 0.4f, 0.4f, 0.4f);
        private Color _diffuse = Color.FromScRgb(1f, 1f, 1f, 1f);
        private Color _specular = Color.FromScRgb(1f, 1f, 1f, 1f);

        public Point3D Position
        {
            get { return _position; }
            set
            {
                _position = value;
                RaisePropertyChanged("Position");
                RaisePropertyChanged("PositionText");
            }
        }

        public string PositionText
        {
            get { return _position.Format("f1"); }
        }

        public Vector3D Direction
        {
            get { return _dir; }
            set
            {
                _dir = value;
                RaisePropertyChanged("Direction");
                RaisePropertyChanged("DirectionText");
            }
        }

        public string DirectionText
        {
            get { return _dir.Format("f1"); }
        }

        public Color Ambient
        {
            get { return _ambient; }
            set 
            { 
                _ambient = value;
                RaisePropertyChanged("Ambient");
            }
        }

        public Color Diffuse
        {
            get { return _diffuse; }
            set
            {
                _diffuse = value;
                RaisePropertyChanged("Diffuse");
            }
        }

        public Color Specular
        {
            get { return _specular; }
            set
            {
                _specular = value;
                RaisePropertyChanged("Specular");
            }
        }

        //TODO: attenuation
        // SpotPower
        // Range

        private DxLightType _lightType;
        private bool _headlight;

        public DxLightType LightType
        {
            get { return _lightType; }
            set
            {
                _lightType = value;
                RaisePropertyChanged("LightType");
            }
        }

        public bool Headlight
        {
            get {
                return _headlight;
            }
            set {
                _headlight = value;
                RaisePropertyChanged("Headlight");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public DxLight GetLight()
        {
            DxLight light = new DxLight
                                {
                                    Type = LightType,
                                    Position = Position.ToVector3(),
                                    Direction = Direction.ToVector3(),
                                    Ambient = Ambient.ToVector4(),
                                    Diffuse = Diffuse.ToVector4(),
                                    Specular = Specular.ToVector4(),
                                    Attenuation = new Vector3(0.0f, 0.005f, 0.0f),
                                    SpotPower = 0.001f,
                                    Range = 1000f
                                };
            return light;
        }
    }
}