using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using xn;

using Image = AForge.Imaging.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace NiSimpleViewerWPF
{
    unsafe public class HandsDetector
    {
        private UnmanagedImage _blobImage;
        private UnmanagedImage _thresholdOutput;
        private Int32Rect _sourceRect;
        HandsCollection _hands = new HandsCollection();

        GrahamConvexHull _hullFinder = new GrahamConvexHull();

        private int uSize;
        private int vSize;
        private int uCenter;
        private int vCenter;
        private float zeroPlaneDistance;
        private float zeroPlanePixelSize;
        private float focalLength;
        private Blob[] _blobs;
        private List<IntPoint> _hull;


        public void Init(DepthGenerator depthNode, DepthMetaData depthMetaData)
        {
            _thresholdOutput = UnmanagedImage.FromManagedImage(
                    Image.CreateGrayscaleImage(depthMetaData.FullXRes, depthMetaData.FullYRes));

            _blobImage = UnmanagedImage.Create(depthMetaData.FullXRes, depthMetaData.FullYRes, 
                PixelFormat.Format32bppArgb);

            _sourceRect = new Int32Rect(depthMetaData.XOffset, depthMetaData.YOffset,
                depthMetaData.XRes, depthMetaData.YRes);

            zeroPlaneDistance = depthNode.GetIntProperty("ZPD");
            zeroPlanePixelSize = (float) depthNode.GetRealProperty("ZPPS");

            focalLength = zeroPlaneDistance/(zeroPlanePixelSize*2f);
            
            uSize = depthMetaData.XRes;
            vSize = depthMetaData.YRes;
            uCenter = uSize/2;
            vCenter = vSize/2;
        }

        private Point3D ConvertProjecetedPositionToRealWorld(Point3D point)
        {
            float P2R = zeroPlanePixelSize*(1280f/uSize)/zeroPlaneDistance;

            var result = new Point3D();
            var pixelSize = point.Z * P2R;
            result.X = pixelSize * (point.X - uCenter);
            result.Y = pixelSize * (vCenter - point.Y);
            result.Z = point.Z;

            return result;
        }
/*
        private Point3D ConvertRealWorldToProjectedPosition(Point3D point)
        {
            var result = new Point3D();
            var pixelSize = point.Z * _focalLength;
            result.X = (float) (point.X * pixelSize);
            result.Y = (float) (point.Y * pixelSize);
            result.Z = point.Z;
            return result;
        }*/

        public void Update(DepthMetaData depthMetaData)
        {
            _blobs = null;
            _hull = null;

            if (_hands.Count == 0)
                return;

            // for now recognize only one hand (later should iterate for all)
            var hand = _hands.First();

            var pt = ConvertProjecetedPositionToRealWorld(hand.ProjectedPosition);

            var F1 = ((hand.ProjectedPosition.X - uCenter) * hand.RealWorldPosition.Z) / hand.RealWorldPosition.X;
            var F2 = ((vCenter - hand.ProjectedPosition.Y) * hand.RealWorldPosition.Z) / hand.RealWorldPosition.Y;

            //Debug.Print("F1={0:f2} F2={1:f2}", F1, F2);

            // include only 16cm depth around hand point
            var minDepth = hand.RealWorldPosition.Z - 80;
            var maxDepth = hand.RealWorldPosition.Z + 80;

            // scale from real world to projected
            var R2P = (focalLength/hand.ProjectedPosition.Z);
            // hand should be about 24cm in real world
            var h = 240f * R2P;
            var w = 240f * R2P;
            var bbox = new Rectangle((int) (hand.ProjectedPosition.X - w/2), (int) (hand.ProjectedPosition.Y - h/2), (int) w, (int) h);

            using (var depthImage = new UnmanagedImage(depthMetaData.DepthMapPtr,
                depthMetaData.FullXRes, depthMetaData.FullYRes,
                depthMetaData.FullXRes * depthMetaData.BytesPerPixel,
                PixelFormat.Format16bppGrayScale))
            {
                ApplyThreshold(depthImage, _thresholdOutput, (ushort)minDepth, (ushort)maxDepth, bbox);
            }
/*
            // threshold at about 1.5m from kinect
            //TODO: set threshold range from hand recognizer
            var filter = new Threshold(3000);
            filter.Apply(depthImage, _thresholdOutput);
*/

            // now detect blobs
            var blobCounter = new BlobCounter();
            //optional: set blob size thresholds based on expected hand size

            blobCounter.MinWidth = (int) (60*R2P);
            blobCounter.MinHeight = (int) (60*R2P);
            blobCounter.CoupledSizeFiltering = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;
            blobCounter.ProcessImage(_thresholdOutput);

            // clear image
            //Drawing.FillRectangle(_blobImage, Rectangle.Empty, Color.Transparent);

            _blobs = blobCounter.GetObjectsInformation();

            List<IntPoint> leftPoints, rightPoints, edgePoints;

            foreach (var blob in _blobs)
            {
                blobCounter.GetBlobsLeftAndRightEdges(blob, out leftPoints, out rightPoints);

                edgePoints = new List<IntPoint>();
                edgePoints.AddRange(leftPoints);
                edgePoints.AddRange(rightPoints);

                _hull = _hullFinder.FindHull(edgePoints);
            }

            SobelEdgeDetector edgeDetector = new SobelEdgeDetector( );
            //CannyEdgeDetector edgeDetector = new CannyEdgeDetector();
            //DifferenceEdgeDetector edgeDetector = new DifferenceEdgeDetector();
            //HomogenityEdgeDetector edgeDetector = new HomogenityEdgeDetector();
            edgeDetector.ApplyInPlace(_thresholdOutput);

        }

        private unsafe void ApplyThreshold(UnmanagedImage input, UnmanagedImage output, ushort minDepth, ushort maxDepth, Rectangle bbox)
        {
            ushort* inRowPtr = (ushort*) input.ImageData;
            byte* outRowPtr = (byte*) output.ImageData;

            for (int y = 0; y < input.Height; y++)
            {
                ushort* inPtr = inRowPtr;
                byte* outPtr = outRowPtr;
                
                for (int x = 0; x < input.Width; x++)
                {
                    if (x >= bbox.Left && x <= bbox.Right && y >= bbox.Top && y <= bbox.Bottom)
                    {
                        var value = *inPtr;
                        if (value > minDepth && value < maxDepth)
                            *outPtr = 0xff;
                        else
                            *outPtr = 0;
                    }
                    else
                        *outPtr = 0;

                    inPtr++;
                    outPtr++;
                }
                inRowPtr += input.Stride / 2;
                outRowPtr += output.Stride;
            }
        }

        public void Paint(WriteableBitmap b)
        {
            //var size = _blobImage.Width*_blobImage.Height*4;
            //handImageSource.WritePixels(_sourceRect, _blobImage.ImageData, size, _blobImage.Stride);

            //var bitmap = _blobInput;
            //var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            //byte* pGrayRow = (byte*) data.Scan0;

            byte* pGrayRow = (byte*) _thresholdOutput.ImageData;

            b.Lock();
            int nTexMapX = b.BackBufferStride;
            byte* pTexRow = (byte*)b.BackBuffer + _sourceRect.Y * nTexMapX;

            for (int y = 0; y < _sourceRect.Height; y++)
            {
                byte* pGray = pGrayRow;
                byte* pTex = pTexRow + _sourceRect.X;

                for (int x = 0; x < _sourceRect.Width; x++)
                {
                    byte val = *pGray;
                    //var val = bitmap.GetPixel(x, y);
                    pTex[0] = val; // B
                    pTex[1] = val; // G
                    pTex[2] = val; // R
                    pTex[3] = (byte) (val != 0 ? 255 : 0); // A

                    pGray++;
                    pTex += 4;
                }

                pGrayRow += _thresholdOutput.Stride;
                pTexRow += nTexMapX;
            }
            //bitmap.UnlockBits(data);

            //todo: paint hands

            if (_blobs != null)
            {
                foreach (var blob in _blobs)
                {

                    var r = blob.Rectangle;
                    b.DrawRectangle(r.Left, r.Top, r.Right, r.Bottom, Colors.Red);
                }
            }

            if (_hull != null)
            {
                var p1 = _hull[0];
                for (int i = 1; i < _hull.Count; i++)
                {
                    var p2 = _hull[i];
                    b.DrawLine(p1.X, p1.Y, p2.X, p2.Y, Colors.LimeGreen);
                    p1 = p2;
                }
            }

            b.AddDirtyRect(_sourceRect);
            b.Unlock();
        }

        public void UpadeHand(int id, Point3D position, Point3D projectedPosition)
        {
            HandInfo hand;
            if (_hands.Contains(id) == false)
            {
                hand = new HandInfo(id);
                _hands.Add(hand);
            }
            else
            {
                hand = _hands[id];
            }
            hand.RealWorldPosition = position;
            hand.ProjectedPosition = projectedPosition;
        }

        public void RemoveHand(int id)
        {
            _hands.Remove(id);
        }

        class HandInfo
        {
            public HandInfo(int id)
            {
                Id = id;
            }

            public int Id { get; private set; }

            public Point3D RealWorldPosition { get; set; }

            public Point3D ProjectedPosition { get; set; }
        }

        class HandsCollection : KeyedCollection<int, HandInfo>
        {
            protected override int GetKeyForItem(HandInfo item)
            {
                return item.Id;
            }
        }
    }


}