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
            this.FactoryBasic = new Basic.Factory(CancellationTokenSource);
        }

        readonly CancellationTokenSource CancellationTokenSource;

        readonly Basic.Factory FactoryBasic;

        public PayloadFlush Flush(Stream stream)
        {
            return new PayloadFlush(CancellationTokenSource, stream);
        }

        public Common.Payload.Payload FlushAndSucceed(Stream stream)
        {
            return FactoryBasic.Chain(
                Flush(stream),
                FactoryBasic.Succeed()
            );
        }

        public PayloadReadBytesNoMoreThenStart ReadBytesNoMoreThenStart(Stream stream, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
        {
            return new PayloadReadBytesNoMoreThenStart(CancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd);
        }

        public PayloadReadBytesNoMoreThenFinish ReadBytesNoMoreThenFinish(Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
        {
            return new PayloadReadBytesNoMoreThenFinish(CancellationTokenSource, stream, streamOperationTimeoutMilliseconds, exceptionProducer, onReadPastStreamEnd);
        }

        public Common.Payload.Payload ReadBytesNoMoreThen(Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
        {
            return FactoryBasic.Chain(
                ReadBytesNoMoreThenStart(stream, exceptionProducer, onReadPastStreamEnd),
                ReadBytesNoMoreThenFinish(stream, streamOperationTimeoutMilliseconds, exceptionProducer, onReadPastStreamEnd)
            );
        }

        public PayloadReadBytesExactlyStart ReadBytesExactlyStart(Stream stream, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
        {
            return new PayloadReadBytesExactlyStart(CancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd);
        }
        
        public PayloadReadBytesExactlyFinish ReadBytesExactlyFinish(Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
        {
            return new PayloadReadBytesExactlyFinish(CancellationTokenSource, stream, streamOperationTimeoutMilliseconds, exceptionProducer, onReadPastStreamEnd);
        }

        public Common.Payload.Payload ReadBytesExactly(Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer = null, Action onReadPastStreamEnd = null)
        {
            return FactoryBasic.Chain(
                ReadBytesExactlyStart(stream, exceptionProducer, onReadPastStreamEnd),
                ReadBytesExactlyFinish(stream, streamOperationTimeoutMilliseconds, exceptionProducer, onReadPastStreamEnd)
            );
        }
        
        public Common.Payload.Payload WriteBytesStart(Stream stream, Func<Exception, Exception> exceptionProducer)
        {
            return new PayloadWriteBytesStart(CancellationTokenSource, stream, exceptionProducer);
        }
        public Common.Payload.Payload WriteBytesFinsh(Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer)
        {
            return new PayloadWriteBytesFinish(CancellationTokenSource, stream, streamOperationTimeoutMilliseconds, exceptionProducer);
        }

        public Common.Payload.Payload WriteBytes(Stream stream, int streamOperationTimeoutMilliseconds, Func<Exception, Exception> exceptionProducer)
        {
            return FactoryBasic.Chain(
                WriteBytesStart(stream, exceptionProducer),
                WriteBytesFinsh(stream, streamOperationTimeoutMilliseconds, exceptionProducer)
            );
        }
    }
}