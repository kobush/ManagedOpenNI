using System;
using System.Diagnostics;

namespace NiSimpleViewerWPF
{
    public class FrameCounter
    {
        private long _frameCount;
        private long _lastFrameTime;
        private Stopwatch _timer;

        public FrameCounter()
        {
            Reset();
        }

        public double FramesPerSecond { get; private set; }

        public long FrameCount { get { return _frameCount; } }

        public void Reset()
        {
            _timer = Stopwatch.StartNew();

            _frameCount = 0;
            _lastFrameTime = _timer.ElapsedMilliseconds;
            FramesPerSecond = 0.0;
        }

        public void AddFrame()
        {
            // update frame counter
            _frameCount++;

            long nowTime = _timer.ElapsedMilliseconds;
            long milliseconds = (nowTime - _lastFrameTime);
            if (milliseconds >= 1000)
            {
                FramesPerSecond = _frameCount * 1000.0 / milliseconds;
                _frameCount = 0;
                _lastFrameTime = nowTime;
            }
        }
    }
}