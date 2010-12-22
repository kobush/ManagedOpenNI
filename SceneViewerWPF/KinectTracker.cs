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
        private KinectFrame _currentFrame;

        public KinectFrame CurrentFrame
        {
            //TODO: guard that this data is only accessed on the dispather thread
            get { return _currentFrame; }
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

            if (InitOpenNi(asyncData))
            {
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
            }
            asyncData.Running = false;
            asyncData.AsyncOperation.PostOperationCompleted(evt => InvokeTrackinkgCompleted(EventArgs.Empty), null);

        }

        private bool InitOpenNi(AsyncStateData asyncData)
        {
            try
            {
                _niContext = new XnMOpenNIContextEx();
                _niContext.InitFromXmlFile("openni.xml");

                _imageNode = (XnMImageGenerator) _niContext.FindExistingNode(XnMProductionNodeType.Image);
                _imageMeta = new XnMImageMetaData();
                _imageNode.GetMetaData(_imageMeta);

                if (_imageMeta.PixelFormat != XnMPixelFormat.Rgb24)
                    throw new InvalidOperationException("Only RGB24 pixel format is supported");

                // add depth node
                _depthNode = (XnMDepthGenerator) _niContext.FindExistingNode(XnMProductionNodeType.Depth);
                _depthMeta = new XnMDepthMetaData();
                _depthNode.GetMetaData(_depthMeta);

                if (_depthMeta.PixelFormat != XnMPixelFormat.Grayscale16Bit)
                    throw new InvalidOperationException("Only 16-bit depth precission is supported");

                if (_depthMeta.XRes != _imageMeta.XRes || _depthMeta.YRes != _imageMeta.YRes)
                    throw new InvalidOperationException("Image and depth map must have the same resolution");

                // add scene node
                _sceneNode = (XnMSceneAnalyzer) _niContext.FindExistingNode(XnMProductionNodeType.Scene);
                _sceneMeta = new XnMSceneMetaData();
                _sceneNode.GetMetaData(_sceneMeta);

                asyncData.AsyncOperation.SynchronizationContext.Send(
                    delegate
                        {
                            UpdateDataRecord();
                            InvokeTrackinkgStarted(EventArgs.Empty);
                        }, null);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void UpdateDataRecord()
        {
            if (_currentFrame == null)
                _currentFrame = new KinectFrame();

            _currentFrame.FrameId = (int) _imageMeta.FrameID;
            _currentFrame.XRes = (int) _imageMeta.XRes;
            _currentFrame.YRes = (int) _imageMeta.YRes;
            _currentFrame.ZRes = (int) _depthMeta.ZRes;

            // get the focal length in mm (ZPS = zero plane distance)/ focal length
            _currentFrame.ZeroPlaneDistance = _depthNode.GetIntProperty("ZPD");

            // get the pixel size in mm ("ZPPS" = pixel size at zero plane) 
            _currentFrame.ZeroPlanePixelSize = _depthNode.GetRealProperty("ZPPS");

            int imageSize = (int) (_imageMeta.XRes*_imageMeta.YRes*3);
            Debug.Assert(imageSize == _imageMeta.DataSize);

            if (_currentFrame.ImageMap == null || _currentFrame.ImageMap.Length != imageSize)
                _currentFrame.ImageMap = new byte[imageSize];

            // copy image data
            Marshal.Copy(_imageMeta.Data, _currentFrame.ImageMap, 0, imageSize);

            int depthSize = (int) (_depthMeta.XRes*_depthMeta.YRes);
            Debug.Assert(depthSize * sizeof(ushort) == _depthMeta.DataSize);

            if (_currentFrame.DepthMap == null || _currentFrame.DepthMap.Length != depthSize)
                _currentFrame.DepthMap = new short[depthSize];

            Marshal.Copy(_depthMeta.Data, _currentFrame.DepthMap, 0, depthSize);
        }
    }

    public class KinectFrame
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
