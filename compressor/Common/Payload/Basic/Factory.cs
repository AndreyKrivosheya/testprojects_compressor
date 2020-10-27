using System;
using System.Collections.Generic;
using System.Linq;
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

        public Common.Payload.Payload Succeed()
        {
            return new PayloadSucceed(CancellationTokenSource);
        }
        
        public Common.Payload.Payload ReturnValue(Func<object, object> valueProvider)
        {
            return new PayloadReturnValue(CancellationTokenSource, valueProvider);
        }
        public Common.Payload.Payload ReturnValue(Func<object> valueProvider)
        {
            return new PayloadReturnValue(CancellationTokenSource, valueProvider);
        }
        public Common.Payload.Payload ReturnValue(object value)
        {
            return new PayloadReturnValue(CancellationTokenSource, value);
        }

        public Common.Payload.Payload Conditional(Func<object, bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }

        public Common.Payload.Payload Conditional(Func<int, bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditional(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue, payloadIfFalse);
        }
        public Common.Payload.Payload Conditional<T>(Func<T, bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditional(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue, payloadIfFalse);
        }

        public Common.Payload.Payload Conditional(Func<bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }

        public Common.Payload.Payload ConditionalOnceAndForever(Func<object, bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            return new PayloadConditionalOnceAndForever(CancellationTokenSource, condition, payloadIfTrue);
        }
        public Common.Payload.Payload ConditionalOnceAndForever<T>(Func<T, bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditionalOnceAndForever(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue);
        }
        public Common.Payload.Payload ConditionalOnceAndForever(Func<int, bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditionalOnceAndForever(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue);
        }


        public Common.Payload.Payload ConditionalOnceAndForever(Func<bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            return new PayloadConditionalOnceAndForever(CancellationTokenSource, condition, payloadIfTrue);
        }

        public Common.Payload.Payload Sequence(IEnumerable<(Common.Payload.Payload Payload, bool Mandatory)> payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }
        public Common.Payload.Payload Sequence(IEnumerable<Common.Payload.Payload> payloads)
        {
            return Sequence(payloads.Select(x => (x, true)));
        }
        public Common.Payload.Payload Sequence(params (Common.Payload.Payload Payload, bool Mandatory)[] payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }
        public Common.Payload.Payload Sequence(params Common.Payload.Payload[] payloads)
        {
            return Sequence(payloads.Select(x => (x, true)));
        }

        public Common.Payload.Payload Chain(IEnumerable<Common.Payload.Payload> payloads)
        {
            return new PayloadChain(CancellationTokenSource, payloads);
        }
        public Common.Payload.Payload Chain(params Common.Payload.Payload[] payloads)
        {
            return new PayloadChain(CancellationTokenSource, payloads);
        }

        public Common.Payload.Payload Repeat(Common.Payload.Payload payload)
        {
            return new PayloadRepeat(CancellationTokenSource, payload);
        }
   
        public Common.Payload.Payload WhenSucceeded(Common.Payload.Payload payload, Common.Payload.Payload payloadAfterPayloadFinished)
        {
            return new PayloadWhenSucceeded(CancellationTokenSource, payload, payloadAfterPayloadFinished);
        }
   }
}