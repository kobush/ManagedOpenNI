using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xn;

namespace NiSimpleViewerWPF
{
    internal unsafe class DepthHistogram
    {
        private float[] _depthHist;
        const int MaxDepth = 10000;

        public void Update(DepthMetaData depthMeta)
        {
            if (_depthHist == null)
                _depthHist = new float[MaxDepth];

            Array.Clear(_depthHist, 0, _depthHist.Length);

            int numPoints = 0;

            ushort* ptrDepth = (ushort*)depthMeta.DepthMapPtr;
            for (int y = 0; y < depthMeta.YRes; y++)
            {
                for (int x = 0; x < depthMeta.XRes; x++)
                {
                    if (*ptrDepth != 0)
                    {
                        _depthHist[*ptrDepth]++;
                        numPoints++;
                    }
                    ptrDepth++;
                }
            }

            for (int i = 1; i < MaxDepth; i++)
            {
                _depthHist[i] += _depthHist[i - 1];
            }

            if (numPoints > 0)
            {
                for (int nIndex = 1; nIndex < MaxDepth; nIndex++)
                {
                    _depthHist[nIndex] = Int16.MaxValue * (1.0f - (_depthHist[nIndex] / numPoints));
                }
            }
        }

        public void Paint(DepthMetaData depthMeta, WriteableBitmap b)
        {
            if (b.Format == PixelFormats.Gray16)
                PaintGray16(b, depthMeta);
            else if (b.Format == PixelFormats.Pbgra32)
                PaintPbgra32(b, depthMeta);
        }

        private void PaintPbgra32(WriteableBitmap b, DepthMetaData depthMeta)
        {
            b.Lock();

            ushort* pDepthRow = (ushort*)depthMeta.DepthMapPtr;

            int nTexMapX = b.BackBufferStride;
            byte* pTexRow = (byte*)b.BackBuffer + depthMeta.YOffset * nTexMapX;

            for (int y = 0; y < depthMeta.YRes; y++)
            {
                ushort* pDepth = pDepthRow;
                byte* pTex = pTexRow + depthMeta.XOffset;

                for (int x = 0; x < depthMeta.XRes; x++)
                {
                    if (*pDepth != 0)
                    {
                        // paint as yellow
                        byte val = (byte) (_depthHist[*pDepth]/256);
                        pTex[0] = 0;    // B
                        pTex[1] = val;  // G
                        pTex[2] = val;  // R
                        pTex[3] = 255;  // A
                    }
                    else
                    {
                        pTex[0] = 0;  // B
                        pTex[1] = 0;  // G
                        pTex[2] = 0;  // R
                        pTex[3] = 0;  // A
                    }

                    pDepth++;
                    pTex+=4;
                }
                pDepthRow += depthMeta.XRes;
                pTexRow += nTexMapX;
            }

            b.AddDirtyRect(new Int32Rect(0, 0, b.PixelWidth, b.PixelHeight));
            b.Unlock();
        }

        private void PaintGray16(WriteableBitmap b, DepthMetaData depthMeta)
        {
            b.Lock();

            ushort* pDepthRow = (ushort*) depthMeta.DepthMapPtr;

            int nTexMapX = b.BackBufferStride / (b.Format.BitsPerPixel / 8);
            ushort* pTexRow = (ushort*) b.BackBuffer + depthMeta.YOffset*nTexMapX;

            for (int y = 0; y < depthMeta.YRes; y++)
            {
                ushort* pDepth = pDepthRow;
                ushort* pTex = pTexRow + depthMeta.XOffset;

                for (int x = 0; x < depthMeta.XRes; x++)
                {
                    if (*pDepth != 0)
                    {
                        *pTex = (ushort) _depthHist[*pDepth];
                    }
                    else
                    {
                        *pTex = 0;
                    }

                    pDepth++;
                    pTex++;
                }
                pDepthRow += depthMeta.XRes;
                pTexRow += nTexMapX;
            }

            b.AddDirtyRect(new Int32Rect(0, 0, b.PixelWidth, b.PixelHeight));
            b.Unlock();
        }
    }
}