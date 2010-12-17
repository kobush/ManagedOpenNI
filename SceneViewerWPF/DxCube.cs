using System;
using SlimDX;
using SlimDX.Direct3D10;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D10.Buffer;
using MapFlags = SlimDX.Direct3D10.MapFlags;

namespace SceneViewerWPF
{
    public class DxCube : IDisposable
    {
        private readonly SlimDX.Direct3D10.Device _dxDevice;

        private Buffer _indexBuffer;
        private Buffer _vertexBuffer;

        public DxCube(SlimDX.Direct3D10.Device device)
        {
            _dxDevice = device;

            CreateVertexBuffer();
            CreateIndexBuffer();
        }

        // Create VertexBuffer
        private void CreateVertexBuffer()
        {
            // 8 * 32 WHY ? : 1 Float = 4bytes, so Vector4(floatX,floatY,floatZ,floatW) = 4 float = 16 bytes. 
            // See above there are 8 vertices and each Vertex contain 2 * Vector4. Then 8 * (16 bytes + 16 bytes) = 8 * 32
            _vertexBuffer = new Buffer(_dxDevice, 8 * 32, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);

            DataStream vertexStream = _vertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None);
            vertexStream.WriteRange(new[] 
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

        public void Prepare()
        {
            _dxDevice.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _dxDevice.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0); // R16_UInt = Size of one index , here is short = 16
            _dxDevice.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, 32, 0)); // 32  = Size of one vertex;
        }

        public void Render()
        {
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