﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RioSharp
{
    internal sealed class SegmentAwaiter : INotifyCompletion, IDisposable
    {
        RioBufferSegment _currentValue;
        Action _continuation = null;
        WaitCallback _continuationWrapperDelegate;
        SpinLock _spinLock = new SpinLock();

        public SegmentAwaiter()
        {
            _continuationWrapperDelegate = continuationWrapper;
        }

        private void continuationWrapper(object o)
        {
            var res = _continuation;
            _continuation = null;
            res();
        }

        public bool IsCompleted
        {
            get
            {
                bool taken = false;
                _spinLock.Enter(ref taken);
                var res = _currentValue != null;
                if (res)
                    _spinLock.Exit();
                return res;
            }
        }

        public void OnCompleted(Action continuation)
        {
           _continuation = continuation;
            _spinLock.Exit();
        }

        public void Set(RioBufferSegment item)
        {
            bool taken = false;
            _spinLock.Enter(ref taken);
            //if (!taken)
            //    throw new ArgumentException("fuu");

            //if (_currentValue != null)
            //    throw new ArgumentException("fuu");
            
            _currentValue = item;
            _spinLock.Exit();

            if (_continuation != null)
                ThreadPool.QueueUserWorkItem(_continuationWrapperDelegate, null);
        }

        public RioBufferSegment GetResult()
        {
            var res = _currentValue;
            _currentValue = null;
            return res;
        }

        public SegmentAwaiter GetAwaiter() => this;

        public void Dispose()
        {
            _currentValue?.Dispose();
            _currentValue = null;
            if (_continuation != null)
                _continuation();
        }
    }
}