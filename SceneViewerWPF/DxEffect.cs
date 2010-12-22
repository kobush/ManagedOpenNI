using System;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D10;

namespace SceneViewerWPF
{
    public class DxEffect : IDisposable
    {
        private readonly Device _dxDevice;
        private Effect _effect;
        private EffectTechnique _technique;
        private EffectPass _effectPass;

        private InputLayout _inputLayout;

        private EffectMatrixVariable _worldVariable;
        private EffectMatrixVariable _viewVariable;
        private EffectMatrixVariable _projectionVariable;

        public DxEffect(SlimDX.Direct3D10.Device device, string shaderFileName = "scene.fx")
        {
//            using (var bytecode = ShaderBytecode.CompileFromFile(shaderFileName, "VShader", vs))

            _dxDevice = device;
            _effect = Effect.FromFile(_dxDevice, shaderFileName, "fx_4_0", 
                ShaderFlags.None, EffectFlags.None, null, null);

            _technique = _effect.GetTechniqueByName("Render"); //C++ Comparaison// technique = effect->GetTechniqueByName( "Render" );
            _effectPass = _technique.GetPassByIndex(0);

            _worldVariable = _effect.GetVariableByName("World").AsMatrix(); //C++ Comparaison// worldVariable = effect->GetVariableByName( "World" )->AsMatrix();
            _viewVariable = _effect.GetVariableByName("View").AsMatrix();
            _projectionVariable = _effect.GetVariableByName("Projection").AsMatrix();


            ShaderSignature signature = _effectPass.Description.Signature;
            _inputLayout = new InputLayout(_dxDevice, signature, new[] 
                                                                     {
                                                                         //C++...//      { "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D10_INPUT_PER_VERTEX_DATA, 0 },
                                                                         new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 0, 0), // 8bits = 1 bytes, so 32bits = 4 bytes, then R32+G32+B32+A32 = 4+4+4+4 = 16 bytes
                                                                         new InputElement("COLOR", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 16, 0) // 16 = total bytes from the element before, here it's "POSITION"
                                                                     });
            _effect.Optimize();

        }

        public void Prepare(Matrix view, Matrix projection)
        {
            // Update WorldViewProjection variable
            _dxDevice.InputAssembler.SetInputLayout(_inputLayout);
            _viewVariable.SetMatrix(view);
            _projectionVariable.SetMatrix(projection);

        }

        public void Render(Matrix world)
        {
            _worldVariable.SetMatrix(world);
            _effectPass.Apply();
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
        }
    }


}