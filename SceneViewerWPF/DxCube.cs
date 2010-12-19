using System;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D10;
using Buffer = SlimDX.Direct3D10.Buffer;
using Device = SlimDX.Direct3D10.Device;
using Format = SlimDX.DXGI.Format;
using MapFlags = SlimDX.Direct3D10.MapFlags;

namespace SceneViewerWPF
{
    public class DxCube : IDisposable
    {
        private readonly Device _dxDevice;

        private Buffer _indexBuffer;
        private Buffer _vertexBuffer;

        private Effect _effect;
        private EffectTechnique _technique;
        private EffectPass _effectPass;
        private InputLayout _inputLayout;

        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vector3 _pos;
            public Vector3 _normal;
            public Vector2 _texC;

            public Vertex(float x, float y, float z, float nx, float ny, float nz, float u, float v)
            {
                _pos = new Vector3(x, y, z);
                _normal = new Vector3(nx, ny, nz);
                _texC = new Vector2(u, v);
            }

            public static int Size
            {
                get { return Marshal.SizeOf(typeof(Vertex)); }
            }
        }

        public DxCube(Device device)
        {
            _dxDevice = device;

       //     LoadEffect();
            CreateVertexBuffer();
            CreateIndexBuffer();
        }

        private void LoadEffect(string shaderFileName = @"Assets\light.fx")
        {

            _effect = Effect.FromFile(_dxDevice, shaderFileName, "fx_4_0", ShaderFlags.None, EffectFlags.None, null, null);
            _technique = _effect.GetTechniqueByName("Render"); //C++ Comparaison// technique = effect->GetTechniqueByName( "Render" );
            _effectPass = _technique.GetPassByIndex(0);

            ShaderSignature signature = _effectPass.Description.Signature;
            _inputLayout = new InputLayout(_dxDevice, signature, 
                new[] {
                    new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 0, 0), 
                    new InputElement("NORMAL", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 16, 0), 
                    new InputElement("TEXCOORD", 0, SlimDX.DXGI.Format.R32G32_Float, 32, 0)
                });
        }

        // Create VertexBuffer
        private void CreateVertexBuffer()
        {
            // 8 * 32 WHY ? : 1 Float = 4bytes, so Vector4(floatX,floatY,floatZ,floatW) = 4 float = 16 bytes. 
            // See above there are 8 vertices and each Vertex contain 2 * Vector4. Then 8 * (16 bytes + 16 bytes) = 8 * 32
//            _vertexBuffer = new Buffer(_dxDevice, 8 * 32, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);

/*            vertexStream.WriteRange(new[] 
                                        {
                                            // Color ((R)ed, (G)reen, (B)lue, (A)lpha)   *Note Alpha used to blending (Transparency)

                                            // Position   X      Y      Z     W     and  Color   R     G     B     A
                                            new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // First Square  Vertex 1 (0)
                                            new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), //               Vertex 2 (1)
                                            new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), //               Vertex 3 (2)
                                            new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), //               Vertex 4 (3)
                
                                            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // Second Square Vertex 5 (4)
                                            new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), //               Vertex 6 (5)
                                            new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), //               Vertex 7 (6)
                                            new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f)  //               Vertex 8 (7)
                                        });*/

            var numVertices = 8;
            var numFaces = 12;

            _vertexBuffer = new Buffer(_dxDevice, numVertices * Vertex.Size, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);

            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            vertexStream.WriteRange(new[]
                                        {
                                            new Vertex(-1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f),
                                            new Vertex(-1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f),
                                            new Vertex( 1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f),
                                            new Vertex( 1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f),

                                            new Vertex(-1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f),
                                            new Vertex( 1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f),
                                            new Vertex( 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f),
                                            new Vertex(-1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f)
                                        });
            _vertexBuffer.Unmap();
        }

        private void CreateIndexBuffer()
        {
            // Why 36 * sizeof(short) ? : See above there are 36 Indices * sizeof(short) : short = 16bits = 2 bytes
            _indexBuffer = new Buffer(_dxDevice, 36 * sizeof(short), ResourceUsage.Dynamic, BindFlags.IndexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);

            DataStream indexStream = _indexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            indexStream.WriteRange(new short[] 
                                       {    
                                           3,1,0, // Top        Face 1  Triangle  1
                                           2,1,3, //                    Triangle  2
                
                                           0,5,4, // Front      Face 2  Triangle  3
                                           1,5,0, //                    Triangle  4
                
                                           3,4,7, // Left Side  Face 3  Triangle  5
                                           0,4,3, //                    Triangle  6
                
                                           1,6,5, // Right Side Face 4  Triangle  7
                                           2,6,1, //                    Triangle  8
                
                                           2,7,6, // Back       Face 5  Triangle  9
                                           3,7,2, //                    Triangle 10
                
                                           6,4,5, // Bottom     Face 6  Triangle 11
                                           7,4,6, //                    Triangle 12
                                       });
            _indexBuffer.Unmap();
        }

        public void Render()
        {
            _dxDevice.InputAssembler.SetInputLayout(_inputLayout);
/*
            _viewVariable.SetMatrix(view);
            _projectionVariable.SetMatrix(projection);
*/

            _effectPass.Apply();


            _dxDevice.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _dxDevice.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0); // R16_UInt = Size of one index , here is short = 16
            _dxDevice.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, 32, 0)); // 32  = Size of one vertex;
            _dxDevice.DrawIndexed(36, 0, 0);
        }

        public void Dispose()
        {

            if (_vertexBuffer != null)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = null;
            }
            if (_indexBuffer != null)
            {
                _indexBuffer.Dispose();
                _indexBuffer = null;
            }
        }
    }
}