using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xn;

namespace NiSimpleViewerWPF
{
    public class SceneMap
    {
        // define few fancy colors
        private Color[] _colors = new Color[]
                                     {
                                         Colors.Red, Colors.Blue, Colors.Green, Colors.Violet, Colors.Orange,
                                         Colors.Pink, Colors.Magenta, Colors.Lime, Colors.Yellow, Colors.Indigo
                                     };

        Dictionary<int, Color> _labelMap = new Dictionary<int, Color>();

        public void Update(SceneMetaData sceneMeta)
        {

        }
            
        public void Paint(SceneAnalyzer sceneNode, DepthMetaData sceneMeta, WriteableBitmap b)
        {
            b.Lock();

            unsafe
            {
                ushort* pLabelRow = (ushort*)sceneNode.GetLabelMapPtr();

                int nTexMapX = b.BackBufferStride;
                byte* pTexRow = (byte*)b.BackBuffer + sceneMeta.YOffset * nTexMapX;

                for (int y = 0; y < sceneMeta.YRes; y++)
                {
                    ushort* pLabel = pLabelRow;
                    byte* pTex = pTexRow + sceneMeta.XOffset;

                    for (int x = 0; x < sceneMeta.XRes; x++)
                    {
                        //var label = sceneMeta.GetLabel((uint) x, (uint) y);
                        var label = (*pLabel);
                        if (label != 0)
                        {
                            var c = _colors[label%_colors.Length];
                            pTex[0] = c.B;  // B
                            pTex[1] = c.G;  // G
                            pTex[2] = c.R;  // R
                            pTex[3] = c.A;  // A
                        }
                        else
                        {
                            pTex[0] = 0;  // B
                            pTex[1] = 0;  // G
                            pTex[2] = 0;  // R
                            pTex[3] = 0;  // A
                        }
                        pLabel++;
                        pTex += 4;
                    }
                    pLabelRow += sceneMeta.XRes;
                    pTexRow += nTexMapX;
                }
            }
            b.AddDirtyRect(new Int32Rect(0, 0, b.PixelWidth, b.PixelHeight));
            b.Unlock();

        }
    }
}