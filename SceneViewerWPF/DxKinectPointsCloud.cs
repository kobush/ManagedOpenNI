using System;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.D3DCompiler;
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

        private Effect _effect;
        private EffectTechnique _technique;
        private EffectPass _effectPass;
        private EffectVectorVariable _eyePosWVariable;
        private EffectMatrixVariable _viewProjVariable;
        private InputLayout _inputLayout;

        public DxKinectPointsCloud(Device dxDevice)
        {
            _dxDevice = dxDevice;

            LoadEffect(@"Assets\kinectpoints_simple.fx");
        }

        public void Init(KinectData data)
        {
            _xRes = data.XRes;
            _yRes = data.YRes;

            CreateVertexBuffer();
        }


        private void LoadEffect(string shaderFileName)
        {
            _effect = Effect.FromFile(_dxDevice, shaderFileName, "fx_4_0",
               ShaderFlags.None, EffectFlags.None, null, null);

            _technique = _effect.GetTechniqueByName("Render"); //C++ Comparaison// technique = effect->GetTechniqueByName( "Render" );
            _effectPass = _technique.GetPassByIndex(0);

            _eyePosWVariable = _effect.GetVariableByName("gEyePosW").AsVector();
            _viewProjVariable = _effect.GetVariableByName("gViewProj").AsMatrix();

            ShaderSignature signature = _effectPass.Description.Signature;
/*
            _inputLayout = new InputLayout(_dxDevice, signature, 
                new[] {
                    new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32_Float, 0, 0), // 3*4 = 12
                    new InputElement("SIZE", 0, SlimDX.DXGI.Format.R32G32_Float, 12, 0), // 2*4 = 8 -> 20
                    new InputElement("COLOR", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 20, 0) // 4*4 -> 36
            });
*/
            _inputLayout = new InputLayout(_dxDevice, signature,
                new[] {
                    new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 0, 0), // 4*4 = 16
                    new InputElement("COLOR", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 16, 0) // 4*4 -> 36
            });
        }

/*        [StructLayout(LayoutKind.Sequential)]
        public struct PointVertex
        {
            public Vector3 Position;
            public Vector2 PixelSize;
            public Vector4 Color;

            public static int Size
            {
                get { return Marshal.SizeOf(typeof(PointVertex)); }
            }
        }*/

        private void CreateVertexBuffer()
        {
            _vertexCount = _xRes*_yRes;

            _vertices = new Vector4[_vertexCount * 2];

            _vertexBuffer = new Buffer(_dxDevice, 
                _vertexCount * 32, ResourceUsage.Dynamic, BindFlags.VertexBuffer, 
                CpuAccessFlags.Write, ResourceOptionFlags.None);
            
            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            vertexStream.WriteRange(_vertices);
            _vertexBuffer.Unmap();
        }

        public void Update(KinectData data)
        {
            var zeroPlanePixelSize = (float)data.ZeroPlanePixelSize * 2f;
            var f = (float)data.ZeroPlaneDistance;

            var scale = 0.1f; // from mm to cm!

            int imagePtr = 0;
            int depthPtr = 0;
            int vertexPtr = 0;
            for (int v = 0; v < _yRes; v++)
            {
                for (int u = 0; u < _xRes; u++)
                {
                    var depth = data.DepthMap[depthPtr++];
                    //var point = _vertices[vertexPtr++];

                    if (depth != 0)
                    {
                        var pixelSize = depth*zeroPlanePixelSize*scale/f;
                        var pX = (u - 320) * pixelSize;
                        var pY = (v - 240) * pixelSize;
                        var pZ = -depth * scale; 
/*
                    var pX = _xRes / 2f - x; // mirror
                    var pY = y - _yRes/2f;
                    var pZ = (float)-depth;
*/
/*
                        point.Position = new Vector3(pX, pY, pZ);
                        point.PixelSize = new Vector2(pixelSize, pixelSize);
*/
                        _vertices[vertexPtr++] = new Vector4(pX, pY, pZ, pixelSize * 1.1f);

                        var r = data.ImageMap[imagePtr++]/255f; // R
                        var g = data.ImageMap[imagePtr++]/255f; // G
                        var b = data.ImageMap[imagePtr++]/255f; // B
                        //point.Color = new Vector4(r, g, b, 1f);
                        _vertices[vertexPtr++] = new Vector4(r, g, b, 1f);
                    }
                    else
                    {
                        imagePtr += 3;
                        //point.Position = new Vector3(0, 0, 0);
                        //point.Color = new Vector4(0, 0, 0, 0);
                        _vertices[vertexPtr++] = new Vector4(0, 0, 0, 0);
                        _vertices[vertexPtr++] = new Vector4(0, 0, 0, 0);
                    }
                }
            }

            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            vertexStream.WriteRange(_vertices);
            _vertexBuffer.Unmap();
        }

        public void Render(DxCamera camera)
        {
            if (_vertexBuffer == null || _vertexCount == 0)
                return;

            _eyePosWVariable.Set(camera.Eye);
            _viewProjVariable.SetMatrix(camera.View * camera.Projection);
            _effectPass.Apply();

            _dxDevice.InputAssembler.SetInputLayout(_inputLayout);
            _dxDevice.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.PointList);
            _dxDevice.InputAssembler.SetVertexBuffers(0, 
                new VertexBufferBinding(_vertexBuffer, 32, 0));

            _dxDevice.Draw(_vertexCount, 0);
        }

        public void Dispose()
        {
            if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }

            if (_inputLayout != null)
            {
                _inputLayout.Dispose();
                _inputLayout = null;
            }

            if (_vertexBuffer != null)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = null;
            }
        }
    }
}