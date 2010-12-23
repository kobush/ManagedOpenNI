using System;
using System.Text;
using System.Windows.Media.Media3D;
using SlimDX;

namespace SceneViewerWPF
{
    internal static class D3DExtensions
    {
        public static Vector4 ToVector4(this System.Windows.Media.Color thisColor)
        {
            return new Vector4(thisColor.ScR, thisColor.ScG, thisColor.ScB, thisColor.ScA);
        }

        public static Vector3 ToVector3(this System.Windows.Media.Media3D.Vector3D thisVector)
        {
            return new Vector3((float) thisVector.X, (float) thisVector.Y, (float) thisVector.Z);
        }

        public static Vector3 ToVector3(this System.Windows.Media.Media3D.Point3D thisPoint)
        {
            return new Vector3((float) thisPoint.X, (float) thisPoint.Y, (float) thisPoint.Z);
        }

        public static float RandF(this Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * Math.Abs(max - min);
        }

        public static void SetFromWpfCamera(this DxCamera thisCamera, System.Windows.Media.Media3D.PerspectiveCamera wpfCamera)
        {
            thisCamera.At = (wpfCamera.Position + wpfCamera.LookDirection).ToVector3();
            thisCamera.Eye = wpfCamera.Position.ToVector3();
            thisCamera.Up = wpfCamera.UpDirection.ToVector3();

            thisCamera.NearDistance = (float) wpfCamera.NearPlaneDistance;
            thisCamera.FarDistance = (float) wpfCamera.FarPlaneDistance;
            thisCamera.FOV = (float)DegreeToRadian(wpfCamera.FieldOfView);
        }

        public static double DegreeToRadian(this double angle)
        {
            return Math.PI*angle/180.0;
        }

        public static double RadianToDegree(this double angle)
        {
            return angle*(180.0/Math.PI);
        }

        public static string Format(this Point3D pt, string format="F3")
        {
            var sb = new StringBuilder();
            sb.Append(pt.X.ToString(format));
            sb.Append("; ");
            sb.Append(pt.Y.ToString(format));
            sb.Append("; ");
            sb.Append(pt.Z.ToString(format));
            return sb.ToString();
        }
        
        public static string Format(this Vector3D pt, string format="F3")
        {
            var sb = new StringBuilder();
            sb.Append(pt.X.ToString(format));
            sb.Append("; ");
            sb.Append(pt.Y.ToString(format));
            sb.Append("; ");
            sb.Append(pt.Z.ToString(format));
            return sb.ToString();
        }
    }
}