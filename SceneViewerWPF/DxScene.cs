using System;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D10;
using SlimDX.Direct3D10_1;
using SlimDX.DXGI;
using SlimDX.Windows;
using Device = SlimDX.Direct3D10_1.Device1;
using Font = SlimDX.Direct3D10.Font;
using SlimDX.D3DCompiler;

namespace SceneViewerWPF
{
    class DxScene : IDisposable
    {
        private Device _dxDevice;
        private RenderTargetView _dxRenderView;
        private DepthStencilView _dxDepthStencilView;
        
        private Font _dxFont;
        //private DxEffect _dxEffect;
        private DxCube _dxCube;

        private IKinectPointsCloudRenderer _kinectPointsRenderer;
        private DxKinectPointsCloud _kinectPoints = new DxKinectPointsCloud();

        private DxParticleSystemRenderer _fire;
        private DxTextureManager _textureManager;
        private float _lastTime;

        public int ClientHeight { get; set; }
        public int ClientWidth { get; set; }

        public DxCamera Camera { get; set; }
        
        public Texture2D SharedTexture { get; private set; }
        public Texture2D DepthTexture { get; private set; }

        private bool _wireframe;

        public bool Wireframe
        {
            get { return _wireframe; }
            set { _wireframe = value; }
        }

        public DxScene()
        {
            ClientWidth = 100;
            ClientHeight = 100;
            Camera = new DxCamera();

            InitD3D();
        }

        public void Dispose()
        {
            DestroyD3D();
        }

        private void InitD3D()
        {
            _dxDevice = new Device(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_0);

            EnsureOutputBuffers();

            FontDescription fontDesc = new FontDescription
                                           {
                                               Height = 24,
                                               Width = 0,
                                               Weight = 0,
                                               MipLevels = 1,
                                               IsItalic = false,
                                               CharacterSet = FontCharacterSet.Default,
                                               Precision = FontPrecision.Default,
                                               Quality = FontQuality.Default,
                                               PitchAndFamily = FontPitchAndFamily.Default | FontPitchAndFamily.DontCare,
                                               FaceName = "Times New Roman"
                                           };

            _dxFont = new Font(_dxDevice, fontDesc);

            _textureManager = new DxTextureManager(_dxDevice);

//            _dxEffect = new DxEffect(_dxDevice);
            _dxCube = new DxCube(_dxDevice);
            CrateKinectPointsRenderer(KinectPointsRendererType.Billboard);

            ShaderResourceView texArray = _textureManager.CreateTexArray("flares", @"Assets\flare0.dds");
            _fire = new DxParticleSystemRenderer(_dxDevice, texArray, 500);

            _dxDevice.Flush();
        }

        public IKinectPointsCloudRenderer PointsCloudRenderer
        {
            get { return _kinectPointsRenderer; }
        }

        public DxKinectPointsCloud PointsCloud
        {
            get { return _kinectPoints; }
        }

        public void CrateKinectPointsRenderer(KinectPointsRendererType rendererType)
        {
            var oldRenderer = _kinectPointsRenderer;

            // create new renderer
            if (rendererType == KinectPointsRendererType.Billboard)
                _kinectPointsRenderer = new DxKinectPointsCloudRenderer(_dxDevice);
            else if (rendererType == KinectPointsRendererType.Mesh)
                _kinectPointsRenderer = new DxKinectMeshRenderer(_dxDevice);

            // destroy old renderer
            if (oldRenderer != null)
            {
                oldRenderer.Dispose();
            }
        }

        private void EnsureOutputBuffers()
        {
            if (SharedTexture == null || 
                SharedTexture.Description.Width != ClientWidth || 
                SharedTexture.Description.Height != ClientHeight)
            {
                if (SharedTexture != null)
                {
                    SharedTexture.Dispose();
                    SharedTexture = null;
                }


                var colordesc = new Texture2DDescription
                {
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = ClientWidth,
                    Height = ClientHeight,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    OptionFlags = ResourceOptionFlags.Shared, // needed for D3DImage
                    CpuAccessFlags = CpuAccessFlags.None,
                    ArraySize = 1
                };

                if (_dxRenderView != null)
                {
                    _dxRenderView.Dispose();
                    _dxRenderView = null;
                }

                SharedTexture = new Texture2D(_dxDevice, colordesc);

                var descRtv = new RenderTargetViewDescription();
                if (colordesc.SampleDescription.Count > 1)
                    descRtv.Dimension = RenderTargetViewDimension.Texture2DMultisampled;
                else
                    descRtv.Dimension = RenderTargetViewDimension.Texture2D;

                _dxRenderView = new RenderTargetView(_dxDevice, SharedTexture, descRtv);
            }


            if (DepthTexture == null ||
                DepthTexture.Description.Width != ClientWidth ||
                DepthTexture.Description.Height != ClientHeight)
            {
                if (DepthTexture != null)
                {
                    DepthTexture.Dispose();
                    DepthTexture = null;
                }

                var depthDesc = new Texture2DDescription
                                    {
                                        BindFlags = BindFlags.DepthStencil,
                                        Format = Format.D32_Float_S8X24_UInt,
                                        Width = ClientWidth,
                                        Height = ClientHeight,
                                        MipLevels = 1,
                                        SampleDescription = new SampleDescription(1, 0), // not using multisampling
                                        Usage = ResourceUsage.Default,
                                        OptionFlags = ResourceOptionFlags.None,
                                        CpuAccessFlags = CpuAccessFlags.None,
                                        ArraySize = 1
                                    };

                // create depth texture
                DepthTexture = new Texture2D(_dxDevice, depthDesc);

                if (_dxDepthStencilView != null)
                {
                    _dxDepthStencilView.Dispose();
                    _dxDepthStencilView = null;
                }

                var descDsv = new DepthStencilViewDescription();
                descDsv.Format = depthDesc.Format;
                if (depthDesc.SampleDescription.Count > 1)
                {
                    descDsv.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                }
                else
                {
                    descDsv.Dimension = DepthStencilViewDimension.Texture2D;
                }
                descDsv.MipSlice = 0;

                // create depth/stencil view
                _dxDepthStencilView = new DepthStencilView(_dxDevice, DepthTexture, descDsv);
            }
        }

        private void DestroyD3D()
        {
            _textureManager.Dispose();
            _dxCube.Dispose();
            //_dxEffect.Dispose();
            _kinectPointsRenderer.Dispose();


            if (_dxFont != null)
            {
                _dxFont.Dispose();
                _dxFont = null;
            }

            if (DepthTexture != null)
            {
                DepthTexture.Dispose();
                DepthTexture = null;
            }

            if (SharedTexture != null)
            {
                SharedTexture.Dispose();
                SharedTexture = null;
            }

            if (_dxRenderView != null)
            {
                _dxRenderView.Dispose();
                _dxRenderView = null;
            }
                
            if (_dxDepthStencilView != null)
            {
                _dxDepthStencilView.Dispose();
                _dxDepthStencilView = null;
            }

            if (_dxDevice != null)
            {
                _dxDevice.Dispose();
                _dxDevice = null;
            }
        }

        public void Render(float gameTime, int width, int height)
        {
            var dt = gameTime - _lastTime;
            _lastTime = gameTime;

            ClientWidth = width;
            ClientHeight = height;

            BeginScene();
            Camera.Update(ClientWidth, ClientHeight);

            _kinectPointsRenderer.Update(dt, gameTime);
            _kinectPointsRenderer.Render(_kinectPoints, Camera);

/*
            _fire.Update(dt, gameTime);
            _fire.Render(Camera);
*/

/*
            _dxCube.Prepare();
            var xRes = 30;
            var yRes = 30;

*/


/*
            var black = new Color4(1f, 0, 0, 0);
            var rect = new Rectangle(5, 5 + arg % 100, 0, 0);
            _dxFont.Draw(null, "Hello from Direct3D", rect, FontDrawFlags.NoClip, black);
*/

            EndScene();
        }

        private void BeginScene()
        {
            // make sure buffers are initialized with current size
            EnsureOutputBuffers();

            // bind the views to the output merger stage
            _dxDevice.OutputMerger.SetTargets(_dxDepthStencilView, _dxRenderView);

            // clear buffers
            _dxDevice.ClearDepthStencilView(_dxDepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            _dxDevice.ClearRenderTargetView(_dxRenderView, new Color4(0f, 0f, 0f, 0f)); // uses transparent fill color

            // set rasterization parameters
            var rsd = new RasterizerStateDescription
            {
                //IsAntialiasedLineEnabled = true,
                IsFrontCounterclockwise = true,
                CullMode = CullMode.None,
                FillMode = (_wireframe) ? FillMode.Wireframe : FillMode.Solid
            };

            RasterizerState rsdState = RasterizerState.FromDescription(_dxDevice, rsd);
            _dxDevice.Rasterizer.State = rsdState;

            // set viewport
            _dxDevice.Rasterizer.SetViewports(new Viewport(0, 0, ClientWidth, ClientHeight, 0.0f, 1.0f));
        }

        private void EndScene()
        {
            _dxDevice.Flush();
        }
    }

}
