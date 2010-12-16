using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ManagedNiteEx;

namespace NiSimpleViewerWPF
{
    internal unsafe class DepthHistogram
    {
        private float[] _depthHist;
        const int MaxDepth = 10000;

        public void Update(XnMDepthMetaData depthMeta)
        {
            if (_depthHist == null)
                _depthHist = new float[MaxDepth];

            Array.Clear(_depthHist, 0, _depthHist.Length);

            int numPoints = 0;

            short* ptrDepth = (short*)depthMeta.Data;
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

        public void Paint(XnMDepthMetaData depthMeta, WriteableBitmap b)
        {
            if (b.Format == PixelFormats.Gray16)
                PaintGray16(b, depthMeta);
            else if (b.Format == PixelFormats.Pbgra32)
                PaintPbgra32(b, depthMeta);
        }

        private void PaintPbgra32(WriteableBitmap b, XnMDepthMetaData depthMeta)
        {
            b.Lock();

            short* pDepthRow = (short*)depthMeta.Data;

            int nTexMapX = b.BackBufferStride;
            byte* pTexRow = (byte*)b.BackBuffer + depthMeta.YOffset * nTexMapX;

            for (int y = 0; y < depthMeta.YRes; y++)
            {
                short* pDepth = pDepthRow;
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

        private void PaintGray16(WriteableBitmap b, XnMDepthMetaData depthMeta)
        {
            b.Lock();

            short* pDepthRow = (short*) depthMeta.Data;

            int nTexMapX = b.BackBufferStride / (b.Format.BitsPerPixel / 8);
            short* pTexRow = (short*) b.BackBuffer + depthMeta.YOffset*nTexMapX;

            for (int y = 0; y < depthMeta.YRes; y++)
            {
                short* pDepth = pDepthRow;
                short* pTex = pTexRow + depthMeta.XOffset;

                for (int x = 0; x < depthMeta.XRes; x++)
                {
                    if (*pDepth != 0)
                    {
                        *pTex = (short) _depthHist[*pDepth];
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