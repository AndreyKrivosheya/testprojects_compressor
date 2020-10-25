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
            return VerifyParameterNotNullConvertAndRunUnsafe(parameter,
            (BlockToProcess blockToProcess) => new PayloadResultContinuationPending(Processor(blockToProcess)));
        }
    }
}