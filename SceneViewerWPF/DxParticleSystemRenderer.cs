using System;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D10;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D10.Buffer;
using Device = SlimDX.Direct3D10.Device;

namespace SceneViewerWPF
{
    public class DxParticleSystemRenderer : IDisposable
    {
        private readonly Device _dxDevice;
        private readonly int _maxParticles;

        private bool _firstRun;

        private float _gameTime;
        private float _timeStep;
        private float _age;

        private Vector4 _emitPosW;
        private Vector4 _emitDirW;

        private Buffer _initVB;
        private Buffer _drawVB;
        private Buffer _streamOutVB;

        private ShaderResourceView _texArrayRV;
        private ShaderResourceView _randomTexRV;

        private Effect _effect;
        private EffectTechnique _streamOutTech;
        private EffectTechnique _drawTech;

        private EffectMatrixVariable _viewProjVar;
        private EffectScalarVariable _gameTimeVar;
        private EffectScalarVariable _timeStepVar;
        private EffectVectorVariable _eyePosVar;
        private EffectVectorVariable _emitPosVar;
        private EffectVectorVariable _emitDirVar;
        private EffectResourceVariable _texArrayVar;
        private EffectResourceVariable _randomTexVar;

        private InputLayout _vertexLayout;

        public Vector4 EmitterPosition
        {
            get { return _emitPosW; }
            set { _emitPosW = value; }
        }

        public Vector4 EmitterDirection
        {
            get { return _emitDirW; }
            set { _emitDirW = value; }
        }

        public DxParticleSystemRenderer(Device device, ShaderResourceView texArrayRV, int maxParticles)
        {
            _dxDevice = device;
            _maxParticles = maxParticles;

            _texArrayRV = texArrayRV;
            BuildRandomTexture(); 
            LoadEffect(@"Assets\fire.fx");
            BuildVertexBuffer();

            Reset();
        }

        private void BuildRandomTexture()
        {
            // 
            // Create the random data.
            //
            var rand = new Random();
            var randomValues = new Vector4[1024];
            for (int i = 0; i < 1024; ++i)
            {
                randomValues[i] = new Vector4(rand.RandF(-1.0f, 1.0f),
                                              rand.RandF(-1.0f, 1.0f),
                                              rand.RandF(-1.0f, 1.0f),
                                              rand.RandF(-1.0f, 1.0f));
            }

            //
            // Create the texture.
            //
            var texDesc = new Texture1DDescription
                              {
                                  Width = 1024,
                                  MipLevels = 1,
                                  Format = Format.R32G32B32A32_Float,
                                  Usage = ResourceUsage.Immutable,
                                  BindFlags = BindFlags.ShaderResource,
                                  CpuAccessFlags = CpuAccessFlags.None,
                                  OptionFlags = ResourceOptionFlags.None,
                                  ArraySize = 1
                              };

            var ds = new DataStream(randomValues, true, false);
            var randomTex = new Texture1D(_dxDevice, texDesc, ds);
            ds.Close();

            //
            // Create the resource view.
            //
            var viewDesc = new ShaderResourceViewDescription
                               {
                                   Format = texDesc.Format,
                                   Dimension = ShaderResourceViewDimension.Texture1D,
                                   MipLevels = texDesc.MipLevels,
                                   MostDetailedMip = 0
                               };

            _randomTexRV = new ShaderResourceView(_dxDevice, randomTex, viewDesc);

            randomTex.Dispose();
        }

        private void LoadEffect(string fileName)
        {
            _effect = Effect.FromFile(_dxDevice, fileName, "fx_4_0",
                ShaderFlags.None, EffectFlags.None, null, null);

            _streamOutTech = _effect.GetTechniqueByName("StreamOutTech");
            _drawTech = _effect.GetTechniqueByName("DrawTech");

            _viewProjVar = _effect.GetVariableByName("gViewProj").AsMatrix();
            _gameTimeVar = _effect.GetVariableByName("gGameTime").AsScalar();
            _timeStepVar = _effect.GetVariableByName("gTimeStep").AsScalar();
            _eyePosVar = _effect.GetVariableByName("gEyePosW").AsVector();
            _emitPosVar = _effect.GetVariableByName("gEmitPosW").AsVector();
            _emitDirVar = _effect.GetVariableByName("gEmitDirW").AsVector();
            _texArrayVar = _effect.GetVariableByName("gTexArray").AsResource();
            _randomTexVar = _effect.GetVariableByName("gRandomTex").AsResource();

            ShaderSignature signature = _streamOutTech.GetPassByIndex(0).Description.Signature;
            _vertexLayout = new InputLayout(_dxDevice, signature,
                new[] {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0), 
                    new InputElement("VELOCITY", 0, Format.R32G32B32_Float, 12, 0),
                    new InputElement("SIZE", 0, Format.R32G32_Float, 24, 0),
                    new InputElement("AGE", 0, Format.R32_Float, 32, 0),
                    new InputElement("TYPE", 0, Format.R32_UInt, 36, 0)
            });

        }

        private void BuildVertexBuffer()
        {
            //
            // Create the buffer to kick-off the particle system.
            //
            var vbd = new BufferDescription
                          {
                              Usage = ResourceUsage.Default,
                              SizeInBytes = Marshal.SizeOf(typeof (ParticleVertex))*1,
                              BindFlags = BindFlags.VertexBuffer,
                              CpuAccessFlags = CpuAccessFlags.None,
                              OptionFlags = ResourceOptionFlags.None
                          };

            // The initial particle emitter has type 0 and age 0.  The rest
            // of the particle attributes do not apply to an emitter.
            var p = new ParticleVertex
                                   {
                                       Type = 0,
                                       Age = 0.0f
                                   };

            using (var ds = new DataStream(Marshal.SizeOf(p), true, true))
            {
                ds.Write(p);
                ds.Position = 0;
             
                _initVB = new Buffer(_dxDevice, ds, vbd);
            }

            //
            // Create the ping-pong buffers for stream-out and drawing.
            //
            vbd.SizeInBytes = Marshal.SizeOf(typeof(ParticleVertex)) * _maxParticles;
            vbd.BindFlags = BindFlags.VertexBuffer | BindFlags.StreamOutput;

            _drawVB = new Buffer(_dxDevice, vbd);
            _streamOutVB = new Buffer(_dxDevice, vbd);
        }

        public void Reset()
        {
            _firstRun = true;
            _age = 0f;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleVertex
        {
            // ReSharper disable MemberCanBePrivate.Local
            // ReSharper disable FieldCanBeMadeReadOnly.Local
            public Vector3 InitialPosW;
            public Vector3 InitialVelW;
            public Vector2 SizeW;
            public float Age;
            public uint Type;
            // ReSharper restore MemberCanBePrivate.Local
            // ReSharper restore FieldCanBeMadeReadOnly.Local
        }

        public void Update(float dt, float gameTime)
        {
            _gameTime = gameTime;
	        _timeStep = dt;
            _age += dt;
        }

        public void Render(DxCamera camera)
        {
            //
            // Set constants.
            //
            _viewProjVar.SetMatrix(camera.View * camera.Projection);
            _gameTimeVar.Set(_gameTime);
            _timeStepVar.Set(_timeStep);
            _eyePosVar.Set(camera.Eye);
            _emitPosVar.Set(_emitPosW);
            _emitDirVar.Set(_emitDirW);
            _texArrayVar.SetResource(_texArrayRV);
            _randomTexVar.SetResource(_randomTexRV);
            
            //
            // Set IA stage.
            //
            _dxDevice.InputAssembler.SetInputLayout(_vertexLayout);
            _dxDevice.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.PointList);
            int stride = Marshal.SizeOf(typeof(ParticleVertex));
            const int offset = 0;

            // On the first pass, use the initialization VB. Otherwise, use
            // the VB that contains the current particle list.
            _dxDevice.InputAssembler.SetVertexBuffers(0,
                new VertexBufferBinding(_firstRun ? _initVB : _drawVB, stride, offset));

            //
            // Draw the current particle list using stream output only to update
            // them. The updated vertices are streamed out to the target VB.
            //
            _dxDevice.StreamOutput.SetTargets(
                new StreamOutputBufferBinding(_streamOutVB, offset));

            for(int p = 0; p < _streamOutTech.Description.PassCount; ++p)
            {
                _streamOutTech.GetPassByIndex(p).Apply();
                if( _firstRun )
                {
                    _dxDevice.Draw(1, 0);
                    _firstRun = false;
                }
                else
                {
                    _dxDevice.DrawAuto();
                }
            }

            // done streaming out--unbind the vertex buffer
            _dxDevice.StreamOutput.SetTargets(null);

            // ping-pong the vertex buffers
            var tmpVB = _drawVB;
            _drawVB = _streamOutVB;
            _streamOutVB = tmpVB;

            //
            // Draw the updated particle system we just streamed out.
            //
            _dxDevice.InputAssembler.SetVertexBuffers(0,
                new VertexBufferBinding(_drawVB, stride, offset));
            
            for(int p = 0; p < _drawTech.Description.PassCount; ++p)
            {
                _drawTech.GetPassByIndex(p).Apply();
                _dxDevice.DrawAuto();
            }
        }

        ~DxParticleSystemRenderer()
        {
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
        }
    }
}