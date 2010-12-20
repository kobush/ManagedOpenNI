using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.Direct3D10;

namespace SceneViewerWPF
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DxLight
    {
        private Vector3 _pos;
        private float ___pad1;
        private Vector3 _dir;
        private float ___pad2;
        private Vector4 _ambient;
        private Vector4 _diffuse;
        private Vector4 _specular;
        private Vector3 _att;
        private float _spotPow;
        private float _range;
        private int _type;

// ReSharper disable ConvertToAutoProperty
        public Vector3 Position
        {
            get { return _pos; }
            set { _pos = value; }
        }

        public Vector3 Direction
        {
            get { return _dir; }
            set { _dir = value; }
        }

        public Vector4 Ambient
        {
            get { return _ambient; }
            set { _ambient = value; }
        }

        public Vector4 Diffuse
        {
            get { return _diffuse; }
            set { _diffuse = value; }
        }

        public Vector4 Specular
        {
            get { return _specular; }
            set { _specular = value; }
        }

        public Vector3 Attenuation
        {
            get { return _att; }
            set { _att = value; } 
        }

        public float SpotPower
        {
            get { return _spotPow; }
            set { _spotPow = value; }
        }

        public float Range
        {
            get { return _range; }
            set { _range = value; }
        }

        public DxLightType Type
        {
            get { return (DxLightType) _type; }
            set { _type = (int) value; }
        }
// ReSharper restore ConvertToAutoProperty

        public static int Size
        {
            get { return Marshal.SizeOf(typeof(DxLight)); }
        }

        public void SetEffectVariable(EffectVariable l)
        {
            l.GetMemberByName("pos").AsVector().Set(_pos);
            l.GetMemberByName("dir").AsVector().Set(_dir);
            l.GetMemberByName("ambient").AsVector().Set(_ambient);
            l.GetMemberByName("diffuse").AsVector().Set(_diffuse);
            l.GetMemberByName("spec").AsVector().Set(_specular);
            l.GetMemberByName("att").AsVector().Set(_att);
            l.GetMemberByName("spotPower").AsScalar().Set(_spotPow);
            l.GetMemberByName("range").AsScalar().Set(_range);
            l.GetMemberByName("type").AsScalar().Set(_type);
        }
    }

    public enum DxLightType : int
    {
        None = 0,
        Parallel = 1,
        Point = 2,
        Spot = 3
    }
}