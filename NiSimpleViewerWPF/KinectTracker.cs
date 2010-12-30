using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xn;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace NiSimpleViewerWPF
{
    public class KinectTracker
    {
/*
        private InteropBitmap _rgbImageSource;
        private Bitmap _rgbBitmap;
*/

        private WriteableBitmap _rgbImageSource;
        private WriteableBitmap _depthImageSource;
        private WriteableBitmap _sceneImageSource;

        private AsyncStateData _currentState;
        private Context _niContext;
        private ImageGenerator _imageNode;
        private ImageMetaData _imageMeta;
        private DepthGenerator _depthNode;
        private DepthMetaData _depthMeta;
        private SceneAnalyzer _sceneNode;
        //private SceneMetaData _sceneMeta;
        //private UserGenerator _userNode;

        private readonly FrameCounter _frameCounter = new FrameCounter();
        private readonly DepthHistogram _depthHist = new DepthHistogram();
        private readonly SceneMap _sceneMap = new SceneMap();

        private class AsyncStateData
        {
            public readonly AsyncOperation AsyncOperation;
            public volatile bool Canceled = false;
            public volatile bool Running = true;

            public AsyncStateData(object stateData)
            {
                AsyncOperation = AsyncOperationManager.CreateOperation(stateData);
            }
        }

        public ImageSource RgbImageSource
        {
            get { return _rgbImageSource; }
        }

        public ImageSource DepthImageSource
        {
            get { return _depthImageSource; }
        }

        public ImageSource SceneImageSource
        {
            get { return _sceneImageSource; }
        }

        public double FramesPerSecond { get { return _frameCounter.FramesPerSecond; } }

        public event EventHandler TrackinkgCompleted;

        public event EventHandler UpdateViewPort;

        public void InvokeUpdateViewPort(EventArgs e)
        {
            EventHandler handler = UpdateViewPort;
            if (handler != null) handler(this, e);
        }

        public void InvokeTrackinkgCompleted(EventArgs e)
        {
            EventHandler handler = TrackinkgCompleted;
            if (handler != null) handler(this, e);
        }

        public void StartTracking()
        {
            StopTracking();

            AsyncStateData asyncData = new AsyncStateData(new object());

            TrackDelegate trackDelegate = Track;
            trackDelegate.BeginInvoke(asyncData, trackDelegate.EndInvoke, null);

            _currentState = asyncData;
        }

        public void StopTracking()
        {
            if (_currentState != null && _currentState.Running)
                _currentState.Canceled = true;
        }

        private delegate void TrackDelegate(AsyncStateData asyncData);

        private void Track(AsyncStateData asyncData)
        {
            asyncData.Running = true;

            InitOpenNi(asyncData);
            
            _frameCounter.Reset();

            while (!asyncData.Canceled)
            {
                _niContext.WaitAndUpdateAll();

                // update image metadata
                _imageNode.GetMetaData(_imageMeta);
                _depthNode.GetMetaData(_depthMeta);
                //_sceneNode.GetLabelMapPtr();
                //_userNode.GetUserPixels()

                _depthHist.Update(_depthMeta);
                //_sceneMap.Update(_sceneMeta);

                _frameCounter.AddFrame();

                // continue update on UI thread
                asyncData.AsyncOperation.SynchronizationContext.Send(
                    delegate
                    {
                        // Must be called on the synchronization thread.
                        CopyWritableBitmap(_imageMeta, _rgbImageSource);

                       // CopyWritableBitmap(_depthMeta, _depthImageSource);
                        _depthHist.Paint(_depthMeta, _depthImageSource);

                        //CopyWritableBitmap(_sceneMeta, _sceneImageSource);
                        _sceneMap.Paint(_sceneNode, _depthMeta, _sceneImageSource);

                        InvokeUpdateViewPort(EventArgs.Empty);
                     }, null);
            }
            asyncData.Running = false;
            asyncData.AsyncOperation.PostOperationCompleted(evt => InvokeTrackinkgCompleted(EventArgs.Empty), null);
        }


        private void InitOpenNi(AsyncStateData asyncData)
        {
            _niContext = new Context("openni.xml");

            _imageNode = (ImageGenerator)_niContext.FindExistingNode(NodeType.Image);

            _imageMeta = new ImageMetaData();
            _imageNode.GetMetaData(_imageMeta);

            // create the image bitmap source on 
            asyncData.AsyncOperation.SynchronizationContext.Send(
                md => CreateImageBitmap(_imageMeta, out _rgbImageSource), 
                null);

            // add depth node
            _depthNode = (DepthGenerator) _niContext.FindExistingNode(NodeType.Depth);

            _depthMeta = new DepthMetaData();
            _depthNode.GetMetaData(_depthMeta);

            asyncData.AsyncOperation.SynchronizationContext.Send(
                state => CreateImageBitmap(_depthMeta, out _depthImageSource, PixelFormats.Pbgra32), 
                null);

            // add scene node
            _sceneNode = (SceneAnalyzer) _niContext.FindExistingNode(NodeType.Scene);

            //_sceneMeta = new SceneMetaData();
            //_sceneNode.GetMetaData(_sceneMeta);

            asyncData.AsyncOperation.SynchronizationContext.Send(
                state => CreateImageBitmap(_depthMeta, out _sceneImageSource, PixelFormats.Pbgra32),
                null);
        }

        private static void CreateImageBitmap(MapMetaData imageMd, out WriteableBitmap writeableBitmap, PixelFormat format)
        {
            var bmpWidth = imageMd.FullXRes;
            var bmpHeight = imageMd.FullYRes;

            writeableBitmap = new WriteableBitmap(bmpWidth, bmpHeight, 96.0, 96.0, format, null);
        }

        private static void CreateImageBitmap(MapMetaData imageMd, out WriteableBitmap writeableBitmap)
        {
            var format = MapPixelFormat(imageMd.PixelFormat);
            CreateImageBitmap(imageMd, out writeableBitmap, format);
        }

        private static void CopyWritableBitmap(ImageMetaData imageMd, WriteableBitmap b)
        {
            int dataSize = (int) imageMd.DataSize;
            IntPtr data = imageMd.ImageMapPtr;
            
            var rect = new Int32Rect(imageMd.XOffset, imageMd.YOffset, imageMd.XRes, imageMd.YRes);
            b.WritePixels(rect, data, dataSize, b.BackBufferStride);
/*
            b.Lock();
            NativeMethods.RtlMoveMemory(b.BackBuffer, data, dataSize);
            b.Unlock();
*/
        }

        private static void CopyBitmap(IntPtr data, uint dataSize, Bitmap b)
        {
            var pf = b.PixelFormat;
            var rect = new Rectangle(0, 0, b.Width, b.Height);

            System.Drawing.Imaging.BitmapData bmpData = b.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, pf);
            //Marshal.Copy(data, bmpData.Scan0, 0, (int)dataSize);
            NativeMethods.RtlMoveMemory(bmpData.Scan0, data, dataSize);
            b.UnlockBits(bmpData);

            // workaround for InteropBitmap memory leak
            //https://connect.microsoft.com/VisualStudio/feedback/details/603004/massive-gpu-memory-leak-with-interopbitmap
            //GC.Collect(1);
        }

/*
        private Bitmap CreateBitmap(byte[] data)
        {
            // Do the magic to create a bitmap
            int stride = _camWidth * _bytesPerPixel;
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            int scan0 = (int)handle.AddrOfPinnedObject();
            scan0 += (_camHeight - 1) * stride;
            Bitmap b = new Bitmap(_camWidth, _camHeight, -stride, PixelFormat.Format24bppRgb, (IntPtr)scan0);

            // Now you can free the handle
            handle.Free();

            return b;
        }
*/
/*
        private void CreateImageBitmap(XnMImageMetaData imageMd, out InteropBitmap interopBitmap, out Bitmap bitmap)
        {
            var format = MapPixelFormat(imageMd.PixelFormat);
            var bmpWidth = (int)imageMd.FullXRes;
            var bmpHeight = (int)imageMd.FullYRes;

            var numBytes = (uint)(bmpWidth * bmpHeight * format.BitsPerPixel / 8);
            var stride = (bmpWidth * format.BitsPerPixel / 8);

            var section = NativeMethods.CreateFileMapping(NativeMethods.INVALID_HANDLE_VALUE, IntPtr.Zero, NativeMethods.PAGE_READWRITE, 0, numBytes, null);
            var map = NativeMethods.MapViewOfFile(section, NativeMethods.FILE_MAP_ALL_ACCESS, 0, 0, numBytes);

            interopBitmap = (InteropBitmap)Imaging.CreateBitmapSourceFromMemorySection(section,
                bmpWidth, bmpHeight, format, stride, 0);

            bitmap = new Bitmap(bmpWidth, bmpHeight, stride, PixelFormat.Format24bppRgb, map);

            NativeMethods.CloseHandle(section);
        }
*/

        private static PixelFormat MapPixelFormat(xn.PixelFormat xnMPixelFormat)
        {
            switch (xnMPixelFormat)
            {
                case xn.PixelFormat.Grayscale8Bit:
                    return PixelFormats.Gray8;
                case xn.PixelFormat.Grayscale16Bit:
                    return PixelFormats.Gray16;
                case xn.PixelFormat.RGB24:
                    return PixelFormats.Rgb24;
                case xn.PixelFormat.YUV422:
                default:
                    throw new NotSupportedException();
            }
        }

        private static class NativeMethods
        {
            [DllImport("kernel32", EntryPoint = "CreateFileMapping", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

            [DllImport("kernel32", EntryPoint = "CloseHandle", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr handle);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern void RtlMoveMemory(IntPtr dest, IntPtr src, uint len);

            public static readonly uint FILE_MAP_ALL_ACCESS = 0xF001F;

            public const uint PAGE_READWRITE = 0x04;

            public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        }
    }
}
