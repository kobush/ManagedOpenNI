using System;

namespace NiSimpleViewerWPF
{
    public class FrameCounter
    {
        private int _frameCount;
        private long _lastFrameTime;

        public FrameCounter()
        {
            Reset();
        }

        public double FramesPerSecond { get; private set; }

        public void Reset()
        {
            _frameCount = 0;
            _lastFrameTime = DateTime.Now.Ticks;
            FramesPerSecond = 0.0;
        }

        public void AddFrame()
        {
            // update frame counter
            _frameCount++;
            var nowTime = DateTime.Now.Ticks;
            double milliseconds = (nowTime - _lastFrameTime) / (double)TimeSpan.TicksPerMillisecond;
            if (milliseconds >= 1000)
            {
                FramesPerSecond = _frameCount * 1000.0 / milliseconds;
                _frameCount = 0;
                _lastFrameTime = nowTime;
            }
        }
    }
}