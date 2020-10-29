using System;
using System.Collections.Generic;

namespace compressor.Common
{
    class Processors
    {
        public Processors()
        {
            this.IAsyncResultsToProcessors = new Dictionary<IAsyncResult, Processor>();
        }
        public Processors(Processors another)
        {
            this.IAsyncResultsToProcessors = new Dictionary<IAsyncResult, Processor>(another.IAsyncResultsToProcessors);
        }

        readonly Dictionary<IAsyncResult, Processor> IAsyncResultsToProcessors;

        public IAsyncResult BeginRunToBeCompleted(AsyncCallback asyncCallback = null, object state = null)
        {
            var asyncResultCompleted = new AsyncResult(asyncCallback, state);
            
            lock(IAsyncResultsToProcessors)
            {
                IAsyncResultsToProcessors.Add(asyncResultCompleted, null);
            }

            return asyncResultCompleted;
        }

        public void SetAllToBeCompletedWithoutProcessorAsCompleted(bool completedSynchroniously)
        {
            foreach(var pair in IAsyncResultsToProcessors)
            {
                if(pair.Value == null)
                {
                    if(!((AsyncResult)pair.Key).IsCompleted)
                    {
                        ((AsyncResult)pair.Key).SetAsCompleted(completedSynchroniously);
                    }
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
            lock(IAsyncResultsToProcessors)
            {
                var processor = new Processor(actionToRun);
                var processorAsyncResult = processor.BeginRun(asyncCallback, state);

                IAsyncResultsToProcessors.Add(processorAsyncResult, processor);

                return processorAsyncResult;
            }
        }

        public void EndRun(IAsyncResult asyncResult, Action onAsyncResultNotFromThisProcessors = null)
        {
            if(asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            Processor processor;
            lock(IAsyncResultsToProcessors)
            {
                if(!IAsyncResultsToProcessors.TryGetValue(asyncResult, out processor))
                {
                    if(null != onAsyncResultNotFromThisProcessors)
                    {
                        onAsyncResultNotFromThisProcessors();
                    }
                    
                    throw new InvalidOperationException("Unrecognized async result");
                }
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
                lock(IAsyncResultsToProcessors)
                {
                    IAsyncResultsToProcessors.Remove(asyncResult);
                }
            }
        }
    }

    class Processors<T>
    {
        public Processors()
        {
            this.IAsyncResultsToProcessors = new Dictionary<IAsyncResult, Processor<T>>();
        }
        public Processors(Processors<T> another)
        {
            this.IAsyncResultsToProcessors = new Dictionary<IAsyncResult, Processor<T>>(another.IAsyncResultsToProcessors);
        }

        readonly Dictionary<IAsyncResult, Processor<T>> IAsyncResultsToProcessors;

        public IAsyncResult BeginRunCompleted(T value, AsyncCallback asyncCallback = null, object state = null)
        {
            var asyncResultCompleted = new Common.AsyncResult<T>(asyncCallback, state);
            
            lock(IAsyncResultsToProcessors)
            {
                IAsyncResultsToProcessors.Add(asyncResultCompleted, null);
            }

            asyncResultCompleted.SetAsCompleted(value, true);

            return asyncResultCompleted;
        }

        public IAsyncResult BeginRun(Func<T> funcToRun, AsyncCallback asyncCallback = null, object state = null)
        {
            lock(IAsyncResultsToProcessors)
            {
                var processor = new Processor<T>(funcToRun);
                var processorAsyncResult = processor.BeginRun(asyncCallback, state);

                IAsyncResultsToProcessors.Add(processorAsyncResult, processor);

                return processorAsyncResult;
            }
        }

        public T EndRun(IAsyncResult asyncResult, Action onAsyncResultNotFromThisProcessors = null)
        {
            if(asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            Processor<T> processor;
            lock(IAsyncResultsToProcessors)
            {
                if(!IAsyncResultsToProcessors.TryGetValue(asyncResult, out processor))
                {
                    if(null != onAsyncResultNotFromThisProcessors)
                    {
                        onAsyncResultNotFromThisProcessors();
                    }
                    
                    throw new InvalidOperationException("Unrecognized async result");
                }
            }

            try
            {
                if(processor == null)
                {
                    var asyncResultAsAsyncResultNoResult = asyncResult as Common.AsyncResult<T>;
                    if(asyncResultAsAsyncResultNoResult != null && asyncResultAsAsyncResultNoResult.IsCompleted)
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
                lock(IAsyncResultsToProcessors)
                {
                    IAsyncResultsToProcessors.Remove(asyncResult);
                }
            }
        }
    }
}