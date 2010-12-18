using System;
using SlimDX;
using SlimDX.Direct3D10;
using Buffer = SlimDX.Direct3D10.Buffer;
using Device = SlimDX.Direct3D10.Device;

namespace SceneViewerWPF
{
    public class DxKinectPointsCloud : IDisposable
    {
        private readonly Device _dxDevice;
        private int _xRes;
        private int _yRes;
        private Buffer _vertexBuffer;
        private int _vertexCount;
        private Vector4[] _vertices;

        public DxKinectPointsCloud(Device dxDevice)
        {
            _dxDevice = dxDevice;
        }

        public void Init(KinectData data)
        {
            _xRes = data.XRes;
            _yRes = data.YRes;

            CreateVertexBuffer();
        }


        private void CreateVertexBuffer()
        {
            _vertexCount = _xRes*_yRes;

            _vertices = new Vector4[_vertexCount*2];

            _vertexBuffer = new Buffer(_dxDevice, _vertexCount * 32, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
            
            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            vertexStream.WriteRange(_vertices);
            _vertexBuffer.Unmap();
        }

        public void Update(KinectData data)
        {
            var pixel_size_ = (float)data.ZeroPlanePixelSize;
            var F_ = (float)data.ZeroPlaneDistance;

            var scale = 0.1f; // from mm to cm!

            int imagePtr = 0;
            int depthPtr = 0;
            int vertexPtr = 0;
            for (int v = 0; v < _yRes; v++)
            {
                for (int u = 0; u < _xRes; u++)
                {
                    var depth = data.DepthMap[depthPtr++];

                    if (depth != 0)
                    {
                        var pX = (u - 320) * depth * pixel_size_ * scale / F_;
                        var pY = (v - 240) * depth * pixel_size_ * scale / F_;
                        var pZ = -depth * scale; 
/*
                    var pX = _xRes / 2f - x; // mirror
                    var pY = y - _yRes/2f;
                    var pZ = (float)-depth;
*/
                        _vertices[vertexPtr++] = new Vector4(pX, pY, pZ, 1f);

                        var r = data.ImageMap[imagePtr++]/255f; // R
                        var g = data.ImageMap[imagePtr++]/255f; // G
                        var b = data.ImageMap[imagePtr++]/255f; // B
                        _vertices[vertexPtr++] = new Vector4(r, g, b, 1f);
                    }
                    else
                    {
                        imagePtr += 3;
                        _vertices[vertexPtr++] = new Vector4(0, 0, 0, 0);
                        _vertices[vertexPtr++] = new Vector4(0, 0, 0, 0);
                    }
                }
            }

            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            vertexStream.WriteRange(_vertices);
            _vertexBuffer.Unmap();
        }

        public void Render()
        {
            if (_vertexBuffer == null || _vertexCount == 0)
                return;

            _dxDevice.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.PointList);
            _dxDevice.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, 32, 0)); // 32  = Size of one vertex;

            _dxDevice.Draw(_vertexCount, 0);
        }

        public void Dispose()
        {
            if (_vertexBuffer != null)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = null;
            }
        }
    }
}