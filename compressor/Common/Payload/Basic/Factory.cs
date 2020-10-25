using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class Factory
    {
        public Factory(CancellationTokenSource cancellationTokenSource)
        {
            this.CancellationTokenSource = cancellationTokenSource;
        }

        readonly CancellationTokenSource CancellationTokenSource;

        public Payload CreateReturnConstant(object constant)
        {
            return new PayloadReturnConstant(CancellationTokenSource, constant);
        }

        public Payload CreateConditional(Func<object, bool> condition, Payload payloadIfTrue, Payload payloadIfFalse)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }
        public Payload CreateConditional(Func<object, bool> condition, Payload payloadIfTrue)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue);
        }
        public Payload CreateConditional(Func<bool> condition, Payload payloadIfTrue, Payload payloadIfFalse)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }
        public Payload CreateConditional(Func<bool> condition, Payload payloadIfTrue)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue);
        }

        public Payload CreateSequence(IEnumerable<Payload> payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }
        public Payload CreateSequence(params Payload[] payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }

        public Payload CreateChain(IEnumerable<Payload> payloads)
        {
            return new PayloadChain(CancellationTokenSource, payloads);
        }
        public Payload CreateChain(params Payload[] payloads)
        {
            return new PayloadChain(CancellationTokenSource, payloads);
        }

        public Payload CreateRepeat(Payload payload)
        {
            return new PayloadRepeat(CancellationTokenSource, payload);
        }
   }
}