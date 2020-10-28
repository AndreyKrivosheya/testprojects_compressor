using System;
using System.Collections.Generic;
using System.Threading;

using compressor.Common.Payload;
using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor.Payload
{
    abstract class PayloadBlocksToWriteToBytes : Common.Payload.Payload
    {
        public PayloadBlocksToWriteToBytes(CancellationTokenSource cancellationTokenSource, Func<IEnumerable<BlockToWrite>, byte[]> converter)
            : base(cancellationTokenSource)
        {
            this.Converter = converter;
        }

        readonly Func<IEnumerable<BlockToWrite>, byte[]> Converter;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullConvertAnd(
                (IEnumerable<BlockToWrite> blocksToConvert) => new PayloadResultContinuationPending(Converter(blocksToConvert)),
                (BlockToWrite blockToConvert) => new PayloadResultContinuationPending(Converter(new [] { blockToConvert }))
            );
        }
    }
}