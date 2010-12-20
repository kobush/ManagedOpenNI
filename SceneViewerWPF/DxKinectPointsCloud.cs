using System;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D10;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D10.Buffer;
using Device = SlimDX.Direct3D10.Device;
using MapFlags = SlimDX.Direct3D10.MapFlags;

namespace SceneViewerWPF
{
    public class DxKinectPointsCloud : IDisposable
    {
        private readonly Device _dxDevice;
        private int _xRes;
        private int _yRes;
        private Buffer _vertexBuffer;
        private int _vertexCount;
        private PointVertex[] _vertices;

        private Effect _effect;
        private EffectTechnique _technique;
        private EffectPass _effectPass;
        private InputLayout _inputLayout;
        private EffectVectorVariable _eyePosWVariable;
        private EffectMatrixVariable _viewProjVariable;
        private EffectVariable _lightVariable;
        private EffectVectorVariable _fillColorVariable;
        private DxLight _light;

        private Texture2D _imageTexture;
        private ShaderResourceView _imageTextureView;
        private EffectResourceVariable _imageMapResource;

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
            CreateTextures();

            _light = new DxLight
            {
                Type = DxLightType.None,
                Position = new Vector3(0, 0, 0),
                Direction = new Vector3(0, 0, -1),
                Ambient = new Vector4(0.4f, 0.4f, 0.4f, 1.0f),
                Diffuse = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                Specular = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                Attenuation = new Vector3(0.0f, 1.0f, 0.0f),
                SpotPower = 64f,
                Range = 10000f
            };
        }


        private void LoadEffect(string shaderFileName)
        {
            _effect = Effect.FromFile(_dxDevice, shaderFileName, "fx_4_0",
               ShaderFlags.None, EffectFlags.None, null, null);

            _technique = _effect.GetTechniqueByName("Render"); //C++ Comparaison// technique = effect->GetTechniqueByName( "Render" );
            _effectPass = _technique.GetPassByIndex(0);

            _eyePosWVariable = _effect.GetVariableByName("gEyePosW").AsVector();
            _viewProjVariable = _effect.GetVariableByName("gViewProj").AsMatrix();
            
            _fillColorVariable = _effect.GetVariableByName("gFillColor").AsVector();
            _lightVariable = _effect.GetVariableByName("gLight");

            _imageMapResource = _effect.GetVariableByName("gImageMap").AsResource();

            ShaderSignature signature = _effectPass.Description.Signature;
            _inputLayout = new InputLayout(_dxDevice, signature, 
                new[] {
                    new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32_Float, 0, 0), // 3*4 = 12
                    new InputElement("SIZE", 0, SlimDX.DXGI.Format.R32G32_Float, 12, 0),
                    new InputElement("TEXCOORD", 0, SlimDX.DXGI.Format.R32G32_Float, 20, 0) // 4*4 -> 36
            });
/*
            _inputLayout = new InputLayout(_dxDevice, signature,
                new[] {
                    new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 0, 0), // 4*4 = 16
                    new InputElement("COLOR", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 16, 0) // 4*4 -> 36
            });
*/
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PointVertex
        {
            public Vector3 pos;
            public Vector2 size;
            public Vector2 texC;

            public static int SizeOf
            {
                get { return Marshal.SizeOf(typeof(PointVertex)); }
            }
        }

        private void CreateTextures()
        {
            var descTex = new Texture2DDescription
                              {
                                  BindFlags = BindFlags.ShaderResource,
                                  CpuAccessFlags = CpuAccessFlags.Write,
                                  Format = SlimDX.DXGI.Format.R8G8B8A8_UNorm,
                                  //Format = SlimDX.DXGI.Format.R32G32B32A32_Float,
                                  OptionFlags = ResourceOptionFlags.None,
                                  SampleDescription = new SampleDescription(1, 0),
                                  Width = _xRes,
                                  Height = _yRes,
                                  MipLevels = 1,
                                  ArraySize = 1,
                                  Usage = ResourceUsage.Dynamic
                              };

            _imageTexture = new Texture2D(_dxDevice, descTex);

            _imageTextureView = new ShaderResourceView(_dxDevice, _imageTexture);
        }

        private void CreateVertexBuffer()
        {
            _vertexCount = _xRes*_yRes;

            _vertices = new PointVertex[_vertexCount];

            _vertexBuffer = new Buffer(_dxDevice,
                _vertexCount * PointVertex.SizeOf, ResourceUsage.Dynamic, BindFlags.VertexBuffer, 
                CpuAccessFlags.Write, ResourceOptionFlags.None);
            
            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            vertexStream.WriteRange(_vertices);
            _vertexBuffer.Unmap();
        }

        public void Update(KinectData data)
        {
            var zeroPlanePixelSize = (float)data.ZeroPlanePixelSize * 2f;
            var f = (float)data.ZeroPlaneDistance;

            const float scale = 0.1f; // Scale from mm to cm!

            var imageRect = _imageTexture.Map(0, MapMode.WriteDiscard, MapFlags.None);
            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);

            var imageMap = data.ImageMap;
            var depthMap = data.DepthMap;
            int imagePtr = 0;
            int depthPtr = 0;
            int vertexPtr = 0;
            for (int v = 0; v < _yRes; v++)
            {
                for (int u = 0; u < _xRes; u++)
                {
                    byte r = imageMap[imagePtr++];
                    byte g = imageMap[imagePtr++];
                    byte b = imageMap[imagePtr++];
                    byte a = 255;

                    int argb = (a << 24) + (b << 16) + (g << 8) + r;
                    
                    imageRect.Data.Write(argb);
/*
                    imageRect.Data.WriteByte(data.ImageMap[imagePtr++]); // R
                    imageRect.Data.WriteByte(data.ImageMap[imagePtr++]); // G
                    imageRect.Data.WriteByte(data.ImageMap[imagePtr++]); // B
                    imageRect.Data.WriteByte(255); // A
*/
/*
                    imageRect.Data.Write(data.ImageMap[imagePtr++] / 255f); // R
                    imageRect.Data.Write(data.ImageMap[imagePtr++] / 255f); // G
                    imageRect.Data.Write(data.ImageMap[imagePtr++] / 255f); // B
                    imageRect.Data.Write(1f); // A
*/


                    var depth = depthMap[depthPtr++];

                    float pixelSize = 0f;
                    var pos = new Vector3();
                    var color = new Vector4();
                    if (depth != 0)
                    {
                        pixelSize = depth*zeroPlanePixelSize*scale/f;
                        var pX = (u - _xRes/2f) * pixelSize;
                        var pY = (_yRes/2f - v) * pixelSize;
                        var pZ = depth * scale; // scale depth from mm to cm!

                        pos = new Vector3(pX, pY, pZ);
/*
                        _vertices[vertexPtr++] = new Vector4(pX, pY, pZ, 
                            pixelSize * 1.2f); // add small overlap
*/

/*
                        var r = data.ImageMap[imagePtr++]/255f; // R
                        var g = data.ImageMap[imagePtr++]/255f; // G
                        var b = data.ImageMap[imagePtr++]/255f; // B
                        color = new Vector4(r, g, b, 1f);
*/
                        //_vertices[vertexPtr++] = new Vector4(r, g, b, 1f);
                    }
                    else
                    {
                        //imagePtr += 3;
                        //point.Position = new Vector4(0, 0, 0, 0);
                        //point.Color = new Vector4(0, 0, 0, 0);
                        //_vertices[vertexPtr++] = new Vector4(0, 0, 0, 0);
                        //_vertices[vertexPtr++] = new Vector4(0, 0, 0, 0);
                    }

                    vertexStream.Write(pos);
                    vertexStream.Write(new Vector2(pixelSize * 1.2f));
                    vertexStream.Write(new Vector2((float)u / _xRes, (float)v / _yRes));

/*
                    _vertices[vertexPtr++] = new PointVertex
                                                 {
                                                     pos = pos,
                                                     size = new Vector2(pixelSize *1.2f),
                                                     texC = new Vector2((float)u/_xRes, (float)v/_yRes),
                                                 };
*/
                }
            }

            _imageTexture.Unmap(0);

            //vertexStream.WriteRange(_vertices);
/*
            for (int i = 0; i < _vertices.Length; i++)
            {
                vertexStream.Write(_vertices[i].Position);
                vertexStream.Write(_vertices[i].Color);
            }
*/
            _vertexBuffer.Unmap();
        }

        public void Render(DxCamera camera)
        {
            if (_vertexBuffer == null || _vertexCount == 0)
                return;

            _light.SetEffectVariable(_lightVariable);

            _imageMapResource.SetResource(_imageTextureView);

            _eyePosWVariable.Set(camera.Eye);
            _viewProjVariable.SetMatrix(camera.View*camera.Projection);
            //_fillColorVariable.Set(new Vector4(1.0f, 0.2f, 0.2f, 1.0f));

/*            using (DataStream lightStream = _perFrameBuffer.Map(MapMode.WriteDiscard, MapFlags.None))
            {
                lightStream.Write(light);
                lightStream.Position = 0; // rewind
            }

            _perFrameVariable.SetConstantBuffer(_perFrameBuffer);*/

            _effectPass.Apply();

            _dxDevice.InputAssembler.SetInputLayout(_inputLayout);
            _dxDevice.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.PointList);
            _dxDevice.InputAssembler.SetVertexBuffers(0, 
                new VertexBufferBinding(_vertexBuffer, PointVertex.SizeOf, 0));

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