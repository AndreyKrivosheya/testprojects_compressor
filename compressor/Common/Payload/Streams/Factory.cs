using System;
using System.IO;
using System.Threading;

namespace compressor.Common.Payload.Streams
{
    class Factory
    {
        public Factory(CancellationTokenSource cancellationTokenSource)
        {
            this.CancellationTokenSource = cancellationTokenSource;
            this.FactoryBasicLazy = new Lazy<Basic.Factory>(() => new Basic.Factory(CancellationTokenSource));
        }

        readonly CancellationTokenSource CancellationTokenSource;

        readonly Lazy<Basic.Factory> FactoryBasicLazy;
        Basic.Factory FactoryBasic { get { return FactoryBasicLazy.Value; } }

        public Common.Payload.Payload CreateWriteBytes(Stream stream, Func<Exception, Exception> exceptionProducer)
        {
            return new PayloadWriteBytes(CancellationTokenSource, stream, exceptionProducer);
        }
        public Common.Payload.Payload CreateWriteBytesFinsh(Stream stream, Func<Exception, Exception> exceptionProducer)
        {
            return new PayloadWriteBytesFinish(CancellationTokenSource, stream, exceptionProducer);
        }

        public Common.Payload.Payload CreateWriteBytesChain(Stream stream, Func<Exception, Exception> exceptionProducer)
        {
            return FactoryBasic.CreateChain(
                CreateWriteBytes(stream, exceptionProducer),
                CreateWriteBytesFinsh(stream, exceptionProducer)
            );
        }
        public Common.Payload.Payload CreateWriteBytesChain(Stream stream)
        {
            return CreateWriteBytesChain(stream, (e) => new ApplicationException("Failed to write block", e));
        }
    }
}