using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace compressor.Common
{
    class Processors
    {
        public Processors()
        {
            this.IAsyncResultsToProcessors = new ConcurrentDictionary<IAsyncResult, Processor>();
        }

        readonly IDictionary<IAsyncResult, Processor> IAsyncResultsToProcessors;

        public IAsyncResult BeginRunToBeCompleted(AsyncCallback asyncCallback = null, object state = null)
        {
            var asyncResultCompleted = new AsyncResult(asyncCallback, state);
            
            IAsyncResultsToProcessors.Add(asyncResultCompleted, null);

            return asyncResultCompleted;
        }

        public void SetAllToBeCompletedWithoutProcessorAsCompleted(bool completedSynchroniously)
        {
            foreach(var asyncResult in IAsyncResultsToProcessors.Keys)
            {
                Processor processor = null;
                if(IAsyncResultsToProcessors.TryGetValue(asyncResult, out processor))
                {
                    if(processor == null && !asyncResult.IsCompleted)
                    {
                        ((AsyncResult)asyncResult).SetAsCompleted(completedSynchroniously);
                    }
                }
                else
                {
                    // key already removed (corresponding begin ended)
                    continue;
                }
            }
        }

        public IAsyncResult BeginRunCompleted(AsyncCallback asyncCallback = null, object state = null)
        {
            var asyncResultCompleted = BeginRunToBeCompleted(asyncCallback, state);
            ((AsyncResult)asyncResultCompleted).SetAsCompleted(true);

            return asyncResultCompleted;
        }

        public IAsyncResult BeginRun(Action actionToRun, AsyncCallback asyncCallback = null, object state = null)
        {
            var processor = new Processor(actionToRun);
            var processorAsyncResult = processor.BeginRun(asyncCallback, state);

            IAsyncResultsToProcessors.Add(processorAsyncResult, processor);

            return processorAsyncResult;
        }

        public void EndRun(IAsyncResult asyncResult, Action onAsyncResultNotFromThisProcessors = null)
        {
            if(asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            Processor processor;
            if(!IAsyncResultsToProcessors.TryGetValue(asyncResult, out processor))
            {
                if(null != onAsyncResultNotFromThisProcessors)
                {
                    onAsyncResultNotFromThisProcessors();
                }
                
                throw new InvalidOperationException("Unrecognized async result");
            }

            try
            {
                if(processor == null)
                {
                    var asyncResultAsAsyncResultNoResult = asyncResult as Common.AsyncResult;
                    if(asyncResultAsAsyncResultNoResult != null)
                    {
                        asyncResultAsAsyncResultNoResult.EndInvoke();
                    }
                    else
                    {
                        throw new NotSupportedException("Could not end unexpected but recognized async result");
                    }
                }
                else
                {
                    processor.EndRun(asyncResult);
                }
            }
            finally
            {
                IAsyncResultsToProcessors.Remove(asyncResult);
            }
        }
    }

    class Processors<T>
    {
        public Processors()
        {
            this.IAsyncResultsToProcessors = new ConcurrentDictionary<IAsyncResult, Processor<T>>();
        }

        readonly IDictionary<IAsyncResult, Processor<T>> IAsyncResultsToProcessors;

        public IAsyncResult BeginRunCompleted(T value, AsyncCallback asyncCallback = null, object state = null)
        {
            var asyncResultCompleted = new Common.AsyncResult<T>(asyncCallback, state);
            
            IAsyncResultsToProcessors.Add(asyncResultCompleted, null);

            asyncResultCompleted.SetAsCompleted(value, true);

            return asyncResultCompleted;
        }

        public IAsyncResult BeginRun(Func<T> funcToRun, AsyncCallback asyncCallback = null, object state = null)
        {
            var processor = new Processor<T>(funcToRun);
            var processorAsyncResult = processor.BeginRun(asyncCallback, state);

            IAsyncResultsToProcessors.Add(processorAsyncResult, processor);

            return processorAsyncResult;
        }

        public T EndRun(IAsyncResult asyncResult, Action onAsyncResultNotFromThisProcessors = null)
        {
            if(asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            Processor<T> processor;
            if(!IAsyncResultsToProcessors.TryGetValue(asyncResult, out processor))
            {
                if(null != onAsyncResultNotFromThisProcessors)
                {
                    onAsyncResultNotFromThisProcessors();
                }
                
                throw new InvalidOperationException("Unrecognized async result");
            }

            try
            {
                if(processor == null)
                {
                    var asyncResultAsAsyncResultNoResult = asyncResult as Common.AsyncResult<T>;
                    if(asyncResultAsAsyncResultNoResult != null)
                    {
                        return asyncResultAsAsyncResultNoResult.EndInvoke();
                    }
                    else
                    {
                        throw new NotSupportedException("Could not end unexpected but recognized async result");
                    }
                }
                else
                {
                    return processor.EndRun(asyncResult);
                }
            }
            finally
            {
                IAsyncResultsToProcessors.Remove(asyncResult);
            }
        }
    }
}