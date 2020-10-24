using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadProcess : Payload
    {
        public PayloadProcess(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Func<BlockToProcess, BlockToWrite> processor)
            : base(cancellationTokenSource, settings)
        {
            this.Processor = processor;
        }

        readonly Func<BlockToProcess, BlockToWrite> Processor;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            var blockToProcess = parameter as BlockToProcess;
            if(blockToProcess == null)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not BlockToProcess", parameter), "parameter");
            }

            return new PayloadResultContinuationPending(Processor(blockToProcess));
        }
    }
}