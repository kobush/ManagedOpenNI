using System;
using System.ComponentModel;
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
        public void StartTracking(IPointCloudViewer pointCloud)
        {
            StopTracking();

            AsyncStateData asyncData = new AsyncStateData(new object());

            TrackDelegate trackDelegate = Track;
            trackDelegate.BeginInvoke(asyncData, pointCloud, trackDelegate.EndInvoke, null);

            _currentState = asyncData;
        }

        public void StopTracking()
        {
            if (_currentState != null && _currentState.Running)
                _currentState.Canceled = true;
        }

        private delegate void TrackDelegate(AsyncStateData asyncData, IPointCloudViewer pointCloud);

        private void Track(AsyncStateData asyncData, IPointCloudViewer pointCloud)
        {
            asyncData.Running = true;

            InitOpenNi(asyncData);
            
            // init point cloud
            asyncData.AsyncOperation.SynchronizationContext.Send(
                delegate
                    {
                        pointCloud.Initialize(_depthMeta.XRes, _depthMeta.YRes);
                    }, null);

            while (!asyncData.Canceled)
            {
                _niContext.WaitAndUpdateAll();

                _imageNode.GetMetaData(_imageMeta);
                _depthNode.GetMetaData(_depthMeta);
                _sceneNode.GetMetaData(_sceneMeta);

                // update point cloud

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


            // add depth node
            _depthNode = (XnMDepthGenerator)_niContext.FindExistingNode(XnMProductionNodeType.Depth);
            _depthMeta = new XnMDepthMetaData();
            _depthNode.GetMetaData(_depthMeta);

            // add scene node
            _sceneNode = (XnMSceneAnalyzer)_niContext.FindExistingNode(XnMProductionNodeType.Scene);
            _sceneMeta = new XnMSceneMetaData();
            _sceneNode.GetMetaData(_sceneMeta);

        }
    }
}
