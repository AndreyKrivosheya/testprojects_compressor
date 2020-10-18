// Refactored from https://docs.microsoft.com/en-us/archive/msdn-magazine/2007/march/implementing-the-clr-asynchronous-programming-model by Jeffrey Richter

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace compressor.Common
{
    class AsyncResultNoResult : IAsyncResult
    {
        readonly AsyncCallback m_AsyncCallback;
        readonly object m_AsyncState;

        const int c_StatePending = 0;
        const int c_StateCompletedSynchronously = 1;
        const int c_StateCompletedAsynchronously = 2;
        int m_CompletedState = c_StatePending;

        ManualResetEvent m_AsyncWaitHandle;

        private ExceptionDispatchInfo m_exception;

        public AsyncResultNoResult(AsyncCallback asyncCallback, Object state)
        {
            m_AsyncCallback = asyncCallback;
            m_AsyncState = state;
        }

        public void SetAsCompleted(Exception exception, Boolean completedSynchronously)
        {
            // Passing null for exception means no error occurred. 
            // This is the common case
            if(exception != null)
            {
                m_exception = ExceptionDispatchInfo.Capture(exception);
            }

            // The m_CompletedState field MUST be set prior calling the callback
            var prevState = Interlocked.Exchange(ref m_CompletedState, completedSynchronously ? c_StateCompletedSynchronously : c_StateCompletedAsynchronously);
            if (prevState != c_StatePending)
            {
                throw new InvalidOperationException("You can set a result only once");
            }

            // If the event exists, set it
            if (m_AsyncWaitHandle != null)
            {
                m_AsyncWaitHandle.Set();
            }

            // If a callback method was set, call it
            if (m_AsyncCallback != null)
            {
                m_AsyncCallback(this);
            }
        }

        public void EndInvoke()
        {
            // This method assumes that only 1 thread calls EndInvoke for this object
            if (!IsCompleted)
            {
                // If the operation isn't done, wait for it
                AsyncWaitHandle.WaitOne();
                AsyncWaitHandle.Close();
                m_AsyncWaitHandle = null;  // Allow early GC
            }

            // Operation is done: if an exception occured, throw it
            if (m_exception != null)
            {
                m_exception.Throw();
            }
        }

        public Object AsyncState
        {
            get
            {
                return m_AsyncState;
            }
        }

        public Boolean CompletedSynchronously
        {
            get
            {
                return Thread.VolatileRead(ref m_CompletedState) == c_StateCompletedSynchronously;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (m_AsyncWaitHandle == null)
                {
                    Boolean done = IsCompleted;
                    ManualResetEvent mre = new ManualResetEvent(done);
                    if (Interlocked.CompareExchange(ref m_AsyncWaitHandle, mre, null) != null)
                    {
                        // Another thread created this object's event; dispose the event we just created
                        mre.Close();
                    }
                    else
                    {
                        if (!done && IsCompleted)
                        {
                            // If the operation wasn't done when we created the event but now it is done, set the event
                            m_AsyncWaitHandle.Set();
                        }
                    }
                }
                return m_AsyncWaitHandle;
            }
        }

        public Boolean IsCompleted
        {
            get
            {
                return Thread.VolatileRead(ref m_CompletedState) !=  c_StatePending;
            }
        }
    }
}