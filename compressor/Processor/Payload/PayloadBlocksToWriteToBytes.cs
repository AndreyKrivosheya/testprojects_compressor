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
        public PayloadBlocksToWriteToBytes(CancellationTokenSource cancellationTokenSource, Func<List<BlockToWrite>, byte[]> converter)
            : base(cancellationTokenSource)
        {
            this.Converter = converter;
        }

        readonly Func<List<BlockToWrite>, byte[]> Converter;

        protected sealed override PayloadResult RunUnsafe(object parameter)
        {
            return parameter.VerifyNotNullAnd(
                (parameter) => {
                    var parameterAsListOfBlocks = parameter as List<BlockToWrite>;
                    if(parameterAsListOfBlocks != null)
                    {
                        return new PayloadResultContinuationPending(Converter(parameterAsListOfBlocks));
                    }
                    
                    var parameterAsBlock = parameter as BlockToWrite;
                    if(parameterAsBlock != null)
                    {
                        return new PayloadResultContinuationPending(Converter(new List<BlockToWrite>(new [] { parameterAsBlock })));
                    }

                    throw new ArgumentException(string.Format("Value of parameter '{0}' type '{1}' is not expected", parameter, parameter.GetType().AssemblyQualifiedName));
                });
        }
    }
}