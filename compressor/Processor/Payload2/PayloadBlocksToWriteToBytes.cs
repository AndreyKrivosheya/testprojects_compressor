using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload2
{
    abstract class PayloadBlocksToWriteToBytes : Payload
    {
        public PayloadBlocksToWriteToBytes(CancellationTokenSource cancellationTokenSource, SettingsProvider settings)
            : base(cancellationTokenSource, settings)
        {
        }

        protected abstract byte[] ConvertBlocksToBytes(List<BlockToWrite> blocks);
        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            if(parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            var blocks = parameter as List<BlockToWrite>;
            if(blocks == null)
            {
                throw new ArgumentException(string.Format("Value of 'parameter' ({0}) is not BlockToProcess", parameter), "parameter");
            }

            return new PayloadResultContinuationPending(ConvertBlocksToBytes(blocks));
        }
    }
}