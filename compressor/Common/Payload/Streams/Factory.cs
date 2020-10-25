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

        public Common.Payload.Payload ReadBytesNoMoreThenStart(Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
        {
            return new PayloadReadBytesNoMoreThenStart(CancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd);
        }

        public Common.Payload.Payload ReadBytesNoMoreThenFinish(Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
        {
            return new PayloadReadBytesNoMoreThenFinish(CancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd);
        }

        public Common.Payload.Payload ReadBytesNoMoreThen(Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
        {
            return FactoryBasic.Chain(
                ReadBytesNoMoreThenStart(stream, exceptionProducer, onReadPastStreamEnd),
                ReadBytesNoMoreThenFinish(stream, exceptionProducer, onReadPastStreamEnd)
            );
        }

        public Common.Payload.Payload ReadBytesExactlyStart(Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
        {
            return new PayloadReadBytesExactlyStart(CancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd);
        }
        
        public Common.Payload.Payload ReadBytesExactlyFinish(Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
        {
            return new PayloadReadBytesExactlyFinish(CancellationTokenSource, stream, exceptionProducer, onReadPastStreamEnd);
        }

        public Common.Payload.Payload ReadBytesExactly(Stream stream, Func<Exception, Exception> exceptionProducer, Action onReadPastStreamEnd)
        {
            return FactoryBasic.Chain(
                ReadBytesExactlyStart(stream, exceptionProducer, onReadPastStreamEnd),
                ReadBytesExactlyFinish(stream, exceptionProducer, onReadPastStreamEnd)
            );
        }
        
        public Common.Payload.Payload WriteBytesStart(Stream stream, Func<Exception, Exception> exceptionProducer)
        {
            return new PayloadWriteBytesStart(CancellationTokenSource, stream, exceptionProducer);
        }
        public Common.Payload.Payload WriteBytesFinsh(Stream stream, Func<Exception, Exception> exceptionProducer)
        {
            return new PayloadWriteBytesFinish(CancellationTokenSource, stream, exceptionProducer);
        }

        public Common.Payload.Payload WriteBytes(Stream stream, Func<Exception, Exception> exceptionProducer)
        {
            return FactoryBasic.Chain(
                WriteBytesStart(stream, exceptionProducer),
                WriteBytesFinsh(stream, exceptionProducer)
            );
        }
    }
}