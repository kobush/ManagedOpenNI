using System;
using SlimDX;

namespace SceneViewerWPF
{
    internal static class D3DExtensions
    {
        public static Vector3 ToVector3(this System.Windows.Media.Media3D.Vector3D thisVector)
        {
            return new Vector3((float) thisVector.X, (float) thisVector.Y, (float) thisVector.Z);
        }

        public static Vector3 ToVector3(this System.Windows.Media.Media3D.Point3D thisPoint)
        {
            return new Vector3((float) thisPoint.X, (float) thisPoint.Y, (float) thisPoint.Z);
        }

        public static void SetFromWpfCamera(this DxCamera thisCamera, System.Windows.Media.Media3D.PerspectiveCamera perspectiveCamera)
        {
            thisCamera.At = perspectiveCamera.LookDirection.ToVector3();
            thisCamera.Eye = perspectiveCamera.Position.ToVector3();
            thisCamera.Up = perspectiveCamera.UpDirection.ToVector3();

            thisCamera.NearDistance = (float) perspectiveCamera.NearPlaneDistance;
            thisCamera.FarDistance = (float) perspectiveCamera.FarPlaneDistance;
            thisCamera.FOV = (float)DegreeToRadian(perspectiveCamera.FieldOfView);
        }

        public static double DegreeToRadian(double angle)
        {
            return Math.PI*angle/180.0;
        }

        public static double RadianToDegree(double angle)
        {
            return angle*(180.0/Math.PI);
        }
    }
}