using System;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadProcess : Common.Payload.Payload
    {
        public PayloadProcess(CancellationTokenSource cancellationTokenSource, Func<BlockToProcess, BlockToWrite> processor)
            : base(cancellationTokenSource)
        {
            this.Processor = processor;
        }

        readonly Func<BlockToProcess, BlockToWrite> Processor;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAndRunUnsafe((BlockToProcess blockToProcess) => new PayloadResultContinuationPending(Processor(blockToProcess)));
        }
    }
}