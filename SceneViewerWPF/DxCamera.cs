using System;
using SlimDX;

namespace SceneViewerWPF
{
    public class DxCamera
    {
        private Matrix _view;
        private Matrix _projection;

        public Matrix Projection
        {
            get { return _projection; }
        }

        public Matrix View
        {
            get { return _view; }
        }

        public Vector3 Eye { get; set; }
        public Vector3 At { get; set; }
        public Vector3 Up { get; set; }

        public float FarDistance { get; set; }
        public float NearDistance { get; set; }

        public float FOV { get; set; }

        public DxCamera()
        {
            Eye = new Vector3(5.0f, 3.0f, -10.0f); // Camera Position
            At = new Vector3(0.0f, 0.0f, 0.0f); // Camera Target / Direction
            Up = new Vector3(0.0f, 1.0f, 0.0f);

            NearDistance = 0.1f;
            FarDistance = 100.0f;
            FOV = (float) System.Math.PI*0.5f;
        }

        public void Update(float clientWidth, float clientHeight)
        {
            // update projection
            _view = Matrix.LookAtRH(Eye, At, Up);
            _projection = Matrix.PerspectiveFovRH(FOV, clientWidth / clientHeight, NearDistance, FarDistance);
            /* //C++//
            D3DXMatrixLookAtLH( &g_View, &Eye, &At, &Up );
            D3DXMatrixPerspectiveFovLH( &g_Projection, ( float )D3DX_PI * 0.5f, width / ( FLOAT )height, 0.1f, 100.0f );
            */
            //C++//
        }
    }
}