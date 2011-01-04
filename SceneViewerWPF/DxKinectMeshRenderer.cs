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
    public class DxKinectMeshRenderer : IKinectPointsCloudRenderer
    {
        #region Private fields

        private readonly Device _dxDevice;

        // indicates if renderer was already initialized from source fame
        private bool _initialized; 

        private int _xRes;
        private int _yRes;
        
        private int _vertexCount;
        private Buffer _vertexBuffer;

        private int _indexCount;
        private Buffer _indexBuffer;

        private Effect _effect;
        private EffectTechnique _renderTech;
        private InputLayout _inputLayout;

        private EffectVectorVariable _eyePosWVar;
        private EffectMatrixVariable _viewProjVar;
        private EffectMatrixVariable _worldVar;

        private Texture2D _imageTexture;
        private ShaderResourceView _imageTextureRV;
        private EffectResourceVariable _imageMapVar;

        private Buffer _depthMapBuffer;
        private ShaderResourceView _depthMapBufferRV;
        private EffectResourceVariable _depthMapVar;

        private Buffer _sceneMapBuffer;
        private ShaderResourceView _sceneMapBufferRV;
        private EffectResourceVariable _sceneMapVar;

        private float _focalLengthDepth;
        private float _focalLengthImage;

        private EffectScalarVariable _focalLengthDepthVar;
        private EffectScalarVariable _focalLengthImageVar;
        private EffectVectorVariable _resVar;

        private Matrix _depthToRgb;
        private EffectMatrixVariable _depthToRgbVar;

        private EffectVariable _lightVariable;
        private EffectVectorVariable _fillColorVar;
        private EffectScalarVariable _userAlphaVar;
        private EffectScalarVariable _backAlphaVar;

        #endregion


        [StructLayout(LayoutKind.Sequential)]
        public struct PointVertex
        {
            public short X;
            public short Y;

            public PointVertex(short x, short y)
            {
                X = x;
                Y = y;
            }

            public static int SizeOf
            {
                get { return Marshal.SizeOf(typeof(PointVertex)); }
            }
        }

        public DxKinectMeshRenderer(Device dxDevice)
        {
            _dxDevice = dxDevice;

            LoadEffect(@"Assets\kinectpoints_mesh.fx");
        }

        private void LoadEffect(string shaderFileName)
        {
            _effect = Effect.FromFile(_dxDevice, shaderFileName, "fx_4_0",
                                      ShaderFlags.None, EffectFlags.None, null, null);

            _renderTech = _effect.GetTechniqueByName("Render"); //C++ Comparaison// technique = effect->GetTechniqueByName( "Render" );

            _eyePosWVar = _effect.GetVariableByName("gEyePosW").AsVector();
            _viewProjVar = _effect.GetVariableByName("gViewProj").AsMatrix();
            _worldVar = _effect.GetVariableByName("gWorld").AsMatrix();
            
            _fillColorVar = _effect.GetVariableByName("gFillColor").AsVector();
            _lightVariable = _effect.GetVariableByName("gLight");
            _userAlphaVar = _effect.GetVariableByName("gUserAlpha").AsScalar();
            _backAlphaVar = _effect.GetVariableByName("gBackAlpha").AsScalar();

            _imageMapVar = _effect.GetVariableByName("gImageMap").AsResource();
            _depthMapVar = _effect.GetVariableByName("gDepthMap").AsResource();
            _sceneMapVar = _effect.GetVariableByName("gSceneMap").AsResource();

            _resVar = _effect.GetVariableByName("gRes").AsVector();
            _depthToRgbVar = _effect.GetVariableByName("gDepthToRgb").AsMatrix();
            _focalLengthDepthVar = _effect.GetVariableByName("gFocalLengthDepth").AsScalar();
            _focalLengthImageVar = _effect.GetVariableByName("gFocalLengthImage").AsScalar();

            ShaderSignature signature = _renderTech.GetPassByIndex(0).Description.Signature;
            _inputLayout = new InputLayout(_dxDevice, signature,
                                           new[] { new InputElement("POSITION", 0, SlimDX.DXGI.Format.R16G16_SInt, 0, 0)
                                                 });
        }

        public void Init(KinectFrame frame, KinectCameraInfo cameraInfo)
        {
            _initialized = true;

            // store variables
            _xRes = cameraInfo.XRes;
            _yRes = cameraInfo.YRes;

            _focalLengthImage = (float)cameraInfo.FocalLengthImage;
            _focalLengthDepth = (float)cameraInfo.FocalLengthDetph;
            _depthToRgb = cameraInfo.DepthToRgb;

            CreateVertexBuffer();
            CreateIndexBuffer();
            CreateTextures();
        }

        private void CreateVertexBuffer()
        {
            _vertexCount = _xRes*_yRes;

            var vertexStream = new DataStream(_vertexCount*PointVertex.SizeOf, true, true);
            
            // store pixel coordinates in each vertex
            for (short y = 0; y < _yRes; y++)
            {
                for (short x = 0; x < _xRes; x++)
                {
                    var pt = new PointVertex(x, y);
                    vertexStream.Write(pt);
                }
            }
            vertexStream.Position = 0;

            // create vertex buffer
            _vertexBuffer = new Buffer(_dxDevice, vertexStream,
                                       _vertexCount*PointVertex.SizeOf, ResourceUsage.Immutable,
                                       BindFlags.VertexBuffer,
                                       CpuAccessFlags.None, ResourceOptionFlags.None);

            vertexStream.Close();
        }

        private void CreateIndexBuffer()
        {
            _indexCount = (_xRes - 1)*(_yRes - 1)*6;
            var indexStream = new DataStream(_indexCount* sizeof(uint), true, true);

            // create index buffer
            for (int y = 0; y < _yRes -1; y++)
            {
                for (int x = 0; x < _xRes -1; x++)
                {
                    // first triangle
                    indexStream.Write(y*_xRes + x);
                    indexStream.Write(y * _xRes + x + 1);
                    indexStream.Write((y + 1) * _xRes + x);

                    // second triangle
                    indexStream.Write((y + 1)*_xRes + x);
                    indexStream.Write(y * _xRes + x + 1);
                    indexStream.Write((y + 1) * _xRes + x + 1);
                }
            }

            indexStream.Position = 0;

            _indexBuffer = new Buffer(_dxDevice, indexStream, _indexCount*sizeof (uint), ResourceUsage.Immutable,
                                      BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None);

            indexStream.Close();
        }

        private void CreateTextures()
        {
            // RGB image texture goes here
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

            _imageTextureRV = new ShaderResourceView(_dxDevice, _imageTexture);


            // depth map buffer
            _depthMapBuffer = new Buffer(_dxDevice, _vertexCount * sizeof(ushort), ResourceUsage.Dynamic,
                                         BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.None);

            _depthMapBufferRV = new ShaderResourceView(_dxDevice, _depthMapBuffer,
                                                       new ShaderResourceViewDescription
                                                           {
                                                               Dimension = ShaderResourceViewDimension.Buffer,
                                                               Format = Format.R16_UInt,
                                                               ElementOffset = 0,
                                                               ElementWidth = _vertexCount,
                                                           });

            // scene map buffer
            _sceneMapBuffer = new Buffer(_dxDevice, _vertexCount * sizeof(ushort), ResourceUsage.Dynamic,
                                         BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.None);

            _sceneMapBufferRV = new ShaderResourceView(_dxDevice, _sceneMapBuffer,
                                                       new ShaderResourceViewDescription
                                                           {
                                                               Dimension = ShaderResourceViewDimension.Buffer,
                                                               Format = Format.R16_UInt,
                                                               ElementOffset = 0,
                                                               ElementWidth = _vertexCount,
                                                           });

        }

        public void Update(KinectFrame frame, KinectCameraInfo cameraInfo)
        {
            if (!_initialized)
                Init(frame, cameraInfo);

            // update texture
            if (_imageTexture != null)
            {
                var imageRect = _imageTexture.Map(0, MapMode.WriteDiscard, MapFlags.None);
                var imageMap = frame.ImageMap;
                var imagePtr = 0;

                // need to convert from RGB24 to RGBA32
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
                    }
                }
                _imageTexture.Unmap(0);
            }

            // update depth map
            if (_depthMapBuffer != null)
            {
                using (DataStream depthStream = _depthMapBuffer.Map(MapMode.WriteDiscard, MapFlags.None))
                {
                    depthStream.WriteRange(frame.DepthMap);
                    _depthMapBuffer.Unmap();
                }

/*                var depthMap = frame.DepthMap;

                for (int v = 0; v < _yRes; v++)
                {
                    for (int u = 0; u < _xRes; u++)
                    {
                        float depth = GetAvgDepth(v, u, depthMap);
                        depthStream.Write(depth);
                    }
                }
 */

                
            }

            // update scene map
            if (_sceneMapBuffer != null)
            {
                using (DataStream sceneStream = _sceneMapBuffer.Map(MapMode.WriteDiscard, MapFlags.None))
                {
                    sceneStream.WriteRange(frame.SceneMap);
                    _sceneMapBuffer.Unmap();
                }
            }
        }

        private float GetAvgDepth(int v, int u, short[] depthMap)
        {
            var sum = 0f; var num = 0;

            for (int y = v-1; y <= v+1; y++)
            {
                for (int x = u-1; x < u+1; x++)
                {
                    // is point in bounds
                    if (y < 0 || y >= _yRes || x < 0 || x >= _xRes) 
                        continue;

                    var d = depthMap[y*_xRes + x];
                    if (d > 0)
                    {
                        sum += d;
                        num++;
                    }
                }
            }

            if (num > 0) 
                return sum/ (float)num;
            
            return 0f;
        }

        public void Update(float dt, float time)
        {
            // ignore
        }

        public void Render(DxKinectPointsCloud pc, DxCamera camera)
        {
            if (_vertexBuffer == null || _vertexCount == 0)
                return;

            // set camera variables
            _eyePosWVar.Set(camera.Eye);
            _viewProjVar.SetMatrix(camera.View*camera.Projection);
            
            // set instance variables
            _worldVar.SetMatrix(pc.World);
            _fillColorVar.Set(pc.FillColor);
            _userAlphaVar.Set(pc.UserAlpha);
            _backAlphaVar.Set(pc.BackgroundAlpha);
            
            // set light
            pc.Light.SetEffectVariable(_lightVariable);

            _resVar.Set(new Vector2(_xRes, _yRes));
            _focalLengthDepthVar.Set(_focalLengthDepth);
            _focalLengthImageVar.Set(_focalLengthImage);
            _depthToRgbVar.SetMatrix(_depthToRgb);

            _depthMapVar.SetResource(_depthMapBufferRV);
            _sceneMapVar.SetResource(_sceneMapBufferRV);
            _imageMapVar.SetResource(_imageTextureRV);

            // set IA inputs
            _dxDevice.InputAssembler.SetInputLayout(_inputLayout);
            _dxDevice.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _dxDevice.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
            _dxDevice.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, PointVertex.SizeOf, 0));

            // render
            for (int p = 0; p < _renderTech.Description.PassCount; p++)
            {
                _renderTech.GetPassByIndex(p).Apply();
                _dxDevice.DrawIndexed(_indexCount, 0, 0);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DxKinectMeshRenderer()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (_depthMapBuffer != null)
            {
                _depthMapBuffer.Dispose();
                _depthMapBuffer = null;
            }

            if (_depthMapBufferRV != null)
            {
                _depthMapBufferRV.Dispose();
                _depthMapBufferRV = null;
            }
            
            if (_sceneMapBuffer != null)
            {
                _sceneMapBuffer.Dispose();
                _sceneMapBuffer = null;
            }

            if (_sceneMapBufferRV != null)
            {
                _sceneMapBufferRV.Dispose();
                _sceneMapBufferRV = null;
            }

            if (_imageTexture != null)
            {
                _imageTexture.Dispose();
                _imageTexture = null;
            }

            if (_imageTextureRV != null)
            {
                _imageTextureRV.Dispose();
                _imageTextureRV = null;
            }

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