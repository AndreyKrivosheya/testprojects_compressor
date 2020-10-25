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

        public Common.Payload.Payload ReturnConstant(object constant)
        {
            return new PayloadReturnConstant(CancellationTokenSource, constant);
        }

        public Common.Payload.Payload Conditional(Func<object, bool> condition, Common.Payload.Payload payloadIfTrue, Common.Payload.Payload payloadIfFalse)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }
        public Common.Payload.Payload Conditional(Func<object, bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue);
        }
        public Common.Payload.Payload Conditional(Func<bool> condition, Common.Payload.Payload payloadIfTrue, Common.Payload.Payload payloadIfFalse)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }
        public Common.Payload.Payload Conditional(Func<bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue);
        }

        public Common.Payload.Payload Sequence(IEnumerable<Common.Payload.Payload> payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }
        public Common.Payload.Payload Sequence(params Common.Payload.Payload[] payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }

        public Common.Payload.Payload Chain(IEnumerable<Common.Payload.Payload> payloads)
        {
            return new PayloadChain(CancellationTokenSource, false, payloads);
        }
        public Common.Payload.Payload Chain(params Common.Payload.Payload[] payloads)
        {
            return new PayloadChain(CancellationTokenSource, false, payloads);
        }

        public Common.Payload.Payload ChainConvertingSuceededToPending(IEnumerable<Common.Payload.Payload> payloads)
        {
            return new PayloadChain(CancellationTokenSource, true, payloads);
        }
        public Common.Payload.Payload ChainConvertingSuceededToPending(params Common.Payload.Payload[] payloads)
        {
            return new PayloadChain(CancellationTokenSource, true, payloads);
        }

        public Common.Payload.Payload Repeat(Payload payload)
        {
            return new PayloadRepeat(CancellationTokenSource, payload);
        }
   }
}