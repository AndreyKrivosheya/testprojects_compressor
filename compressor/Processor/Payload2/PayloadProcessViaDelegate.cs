using System;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    class PayloadProcessViaDelegate : PayloadProcess
    {
        public PayloadProcessViaDelegate(CancellationTokenSource cancellationTokenSource, SettingsProvider settings, Func<BlockToProcess, BlockToWrite> processor)
            : base(cancellationTokenSource, settings)
        {
            this.Processor = processor;
        }

        readonly Func<BlockToProcess, BlockToWrite> Processor;

        protected sealed override BlockToWrite ProcessBlock(BlockToProcess blockToProcess)
        {
            return Processor(blockToProcess);
        }
    }
}