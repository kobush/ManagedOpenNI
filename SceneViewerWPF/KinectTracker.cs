using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ManagedNiteEx;

namespace SceneViewerWPF
{
    class KinectTracker
    {
        private XnMOpenNIContextEx _niContext;
        private XnMImageGenerator _imageNode;
        private XnMImageMetaData _imageMeta;
        private XnMDepthGenerator _depthNode;
        private XnMDepthMetaData _depthMeta;
        private XnMSceneAnalyzer _sceneNode;
        private XnMSceneMetaData _sceneMeta;

        private AsyncStateData _currentState;
        private KinectData _currentData;

        public KinectData CurrentData
        {
            //TODO: guard that this data is only accessed on the dispather thread
            get { return _currentData; }
        }

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

        public event EventHandler TrackinkgStarted;

        public event EventHandler TrackinkgCompleted;

        public event EventHandler TrackingUpdated;

        protected void InvokeTrackinkgStarted(EventArgs e)
        {
            EventHandler handler = TrackinkgStarted;
            if (handler != null) handler(this, e);
        }

        protected void InvokeTrackingUpdated(EventArgs e)
        {
            EventHandler handler = TrackingUpdated;
            if (handler != null) handler(this, e);
        }

        protected void InvokeTrackinkgCompleted(EventArgs e)
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
            
            while (!asyncData.Canceled)
            {
                _niContext.WaitAndUpdateAll();

                _imageNode.GetMetaData(_imageMeta);
                _depthNode.GetMetaData(_depthMeta);
                _sceneNode.GetMetaData(_sceneMeta);

                asyncData.AsyncOperation.SynchronizationContext.Send(
                    delegate
                    {
                        UpdateDataRecord();
                        InvokeTrackingUpdated(EventArgs.Empty);
                    }, null);

            }
            asyncData.Running = false;
            asyncData.AsyncOperation.PostOperationCompleted(evt => InvokeTrackinkgCompleted(EventArgs.Empty), null);

        }

        private void InitOpenNi(AsyncStateData asyncData)
        {
            _niContext = new XnMOpenNIContextEx();
            _niContext.InitFromXmlFile("openni.xml");

            _imageNode = (XnMImageGenerator)_niContext.FindExistingNode(XnMProductionNodeType.Image);
            _imageMeta = new XnMImageMetaData();
            _imageNode.GetMetaData(_imageMeta);

            if (_imageMeta.PixelFormat != XnMPixelFormat.Rgb24)
                throw new InvalidOperationException("Only RGB24 pixel format is supported");

            // add depth node
            _depthNode = (XnMDepthGenerator)_niContext.FindExistingNode(XnMProductionNodeType.Depth);
            _depthMeta = new XnMDepthMetaData();
            _depthNode.GetMetaData(_depthMeta);

            if (_depthMeta.PixelFormat != XnMPixelFormat.Grayscale16Bit)
                throw new InvalidOperationException("Only 16-bit depth precission is supported");

            if (_depthMeta.XRes != _imageMeta.XRes || _depthMeta.YRes != _imageMeta.YRes)
                throw new InvalidOperationException("Image and depth map must have the same resolution");

            // add scene node
            _sceneNode = (XnMSceneAnalyzer)_niContext.FindExistingNode(XnMProductionNodeType.Scene);
            _sceneMeta = new XnMSceneMetaData();
            _sceneNode.GetMetaData(_sceneMeta);

            asyncData.AsyncOperation.SynchronizationContext.Send(
                delegate
                    {
                        UpdateDataRecord();
                        InvokeTrackinkgStarted(EventArgs.Empty);
                    }, null);
        }

        private void UpdateDataRecord()
        {
            if (_currentData == null)
                _currentData = new KinectData();

            _currentData.FrameId = (int) _imageMeta.FrameID;
            _currentData.XRes = (int) _imageMeta.XRes;
            _currentData.YRes = (int) _imageMeta.YRes;
            _currentData.ZRes = (int) _depthMeta.ZRes;

            // get the focal length in mm (ZPS = zero plane distance)/ focal length
            _currentData.ZeroPlaneDistance = _depthNode.GetIntProperty("ZPD");

            // get the pixel size in mm ("ZPPS" = pixel size at zero plane) 
            _currentData.ZeroPlanePixelSize = _depthNode.GetRealProperty("ZPPS");

            int imageSize = (int) (_imageMeta.XRes*_imageMeta.YRes*3);
            Debug.Assert(imageSize == _imageMeta.DataSize);

            if (_currentData.ImageMap == null || _currentData.ImageMap.Length != imageSize)
                _currentData.ImageMap = new byte[imageSize];

            // copy image data
            Marshal.Copy(_imageMeta.Data, _currentData.ImageMap, 0, imageSize);

            int depthSize = (int) (_depthMeta.XRes*_depthMeta.YRes);
            Debug.Assert(depthSize * sizeof(ushort) == _depthMeta.DataSize);

            if (_currentData.DepthMap == null || _currentData.DepthMap.Length != depthSize)
                _currentData.DepthMap = new short[depthSize];

            Marshal.Copy(_depthMeta.Data, _currentData.DepthMap, 0, depthSize);
        }
    }

    public class KinectData
    {
        public int FrameId { get; set; }

        public byte[] ImageMap { get; set; }
        public short[] DepthMap { get; set; }

        public int XRes { get; set; }
        public int YRes { get; set; }
        public int ZRes { get; set; }

        public double ZeroPlaneDistance { get; set; } // or focal length
        public double ZeroPlanePixelSize { get; set; }
   }
}
