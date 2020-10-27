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

        public PayloadSucceed Succeed()
        {
            return new PayloadSucceed(CancellationTokenSource);
        }
        
        public PayloadReturnValue ReturnValue(Func<object, object> valueProvider)
        {
            return new PayloadReturnValue(CancellationTokenSource, valueProvider);
        }
        public PayloadReturnValue ReturnValue(Func<object> valueProvider)
        {
            return new PayloadReturnValue(CancellationTokenSource, valueProvider);
        }
        public PayloadReturnValue ReturnValue(object value)
        {
            return new PayloadReturnValue(CancellationTokenSource, value);
        }

        public PayloadConditional Conditional(Func<object, bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }

        public PayloadConditional Conditional(Func<int, bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditional(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue, payloadIfFalse);
        }
        public PayloadConditional Conditional<T>(Func<T, bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditional(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue, payloadIfFalse);
        }

        public PayloadConditional Conditional(Func<bool> condition, Common.Payload.Payload payloadIfTrue = null, Common.Payload.Payload payloadIfFalse = null)
        {
            return new PayloadConditional(CancellationTokenSource, condition, payloadIfTrue, payloadIfFalse);
        }

        public PayloadConditionalOnceAndForever ConditionalOnceAndForever(Func<object, bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            return new PayloadConditionalOnceAndForever(CancellationTokenSource, condition, payloadIfTrue);
        }
        public PayloadConditionalOnceAndForever ConditionalOnceAndForever<T>(Func<T, bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditionalOnceAndForever(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue);
        }
        public PayloadConditionalOnceAndForever ConditionalOnceAndForever(Func<int, bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            if(condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            return new PayloadConditionalOnceAndForever(CancellationTokenSource, (parameter) => parameter.VerifyNotNullConvertAndTransform(condition), payloadIfTrue);
        }

        public PayloadConditionalOnceAndForever ConditionalOnceAndForever(Func<bool> condition, Common.Payload.Payload payloadIfTrue)
        {
            return new PayloadConditionalOnceAndForever(CancellationTokenSource, condition, payloadIfTrue);
        }

        public PayloadSequence Sequence(IEnumerable<(Common.Payload.Payload Payload, bool Mandatory)> payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }
        public PayloadSequence Sequence(IEnumerable<Common.Payload.Payload> payloads)
        {
            return Sequence(payloads.Select(x => (x, true)));
        }
        public PayloadSequence Sequence(params (Common.Payload.Payload Payload, bool Mandatory)[] payloads)
        {
            return new PayloadSequence(CancellationTokenSource, payloads);
        }
        public PayloadSequence Sequence(params Common.Payload.Payload[] payloads)
        {
            return Sequence(payloads.Select(x => (x, true)));
        }

        public PayloadChain Chain(IEnumerable<Common.Payload.Payload> payloads)
        {
            return new PayloadChain(CancellationTokenSource, payloads);
        }
        public PayloadChain Chain(params Common.Payload.Payload[] payloads)
        {
            return new PayloadChain(CancellationTokenSource, payloads);
        }

        public PayloadRepeat Repeat(Common.Payload.Payload payload)
        {
            return new PayloadRepeat(CancellationTokenSource, payload);
        }
   
        public PayloadWhenSucceeded WhenSucceeded(Common.Payload.Payload payload, Common.Payload.Payload payloadAfterPayloadFinished)
        {
            return new PayloadWhenSucceeded(CancellationTokenSource, payload, payloadAfterPayloadFinished);
        }

        public PayloadTrackLast TrackLast()
        {
            return new PayloadTrackLast(CancellationTokenSource);
        }

        public PayloadTrackLast TrackLastIfNotNull()
        {
            return new PayloadTrackLastIfNotNull(CancellationTokenSource);
        }
   }
}