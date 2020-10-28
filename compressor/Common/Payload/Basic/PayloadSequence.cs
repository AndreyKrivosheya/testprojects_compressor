using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace compressor.Common.Payload.Basic
{
    class PayloadSequence: Payload, AwaitablesHolder
    {
        public PayloadSequence(CancellationTokenSource cancellationTokenSource, IEnumerable<(Common.Payload.Payload Payload, bool Mandatory)> payloads)
            : base(cancellationTokenSource)
        {
            this.Payloads = new List<PayloadWithState>(payloads.Select(x => new PayloadWithState(x.Payload, x.Mandatory)));
        }
        public PayloadSequence(CancellationTokenSource cancellationTokenSource, params (Common.Payload.Payload Payload, bool Mandatory)[] payloads)
            : this(cancellationTokenSource, payloads.AsEnumerable())
        {
        }

        class PayloadWithState
        {
            public PayloadWithState(Common.Payload.Payload payload, bool mandatory)
            {
                this.Payload = payload;
                this.Mandatory = mandatory;
            }

            public readonly Common.Payload.Payload Payload;
            
            public readonly bool Mandatory;

            public bool RanAtLeastOnce = false;
            public bool Succeeded = false;
        }

        readonly List<PayloadWithState> Payloads;

        T TransformCurrentPayloadsLeftToRunTo<T>(Func<T> whenNothingLeftToRun, Func<IEnumerable<PayloadWithState>, T> whenAnyLeftToRun)
        {
            var payloadsNotYetSucceed = Payloads.Where(x => !x.Succeeded);
            // if no unfinished paylaods
            if(!payloadsNotYetSucceed.Any())
            {
                return whenNothingLeftToRun();
            }
            else
            {
                // if all unfinished payloads are either not mandatory or were never run
                if(!(payloadsNotYetSucceed.Where(x => x.Mandatory || (!x.Mandatory && x.RanAtLeastOnce)).Any()))
                {
                    return whenNothingLeftToRun();
                }
                else
                {
                    return whenAnyLeftToRun(payloadsNotYetSucceed);
                }
            }
        }

        protected override PayloadResult RunUnsafe(object parameter)
        {
            return TransformCurrentPayloadsLeftToRunTo<PayloadResult>(
                whenNothingLeftToRun: () => new PayloadResultSucceeded(),
                whenAnyLeftToRun:(payloadsNotYetSucceed) => 
                {
                    var allSucceeded = true;
                    var allDoneNothing = true;
                    foreach(var payload in payloadsNotYetSucceed)
                    {
                        if(CancellationTokenSource.IsCancellationRequested)
                        {
                            return new PayloadResultCanceled();
                        }
                        else
                        {
                            var payloadResult = payload.Payload.Run(parameter);
                            switch(payloadResult.Status)
                            {
                                case PayloadResultStatus.Succeeded:
                                case PayloadResultStatus.ContinuationPendingDoneNothing:
                                case PayloadResultStatus.ContinuationPending:
                                    payload.RanAtLeastOnce = true;
                                    switch(payloadResult.Status)
                                    {
                                        case PayloadResultStatus.Succeeded:
                                            // will not run succeedeed payload in future
                                            payload.Succeeded = true;
                                            // ...
                                            allDoneNothing = false;
                                            break;
                                        case PayloadResultStatus.ContinuationPendingDoneNothing:
                                            allSucceeded = false;
                                            break;
                                        case PayloadResultStatus.ContinuationPending:
                                        default:
                                            allSucceeded = false;
                                            allDoneNothing = false;
                                            break;
                                    }
                                    break;
                                case PayloadResultStatus.ContinuationPendingEvaluatedToEmptyPayload:
                                    allSucceeded = false;
                                    break;
                                case PayloadResultStatus.Canceled:
                                case PayloadResultStatus.Failed:
                                default:
                                    return payloadResult;
                            }
                        }
                    }

                    if(allSucceeded && allDoneNothing)
                    {
                        // were nothing to do actually
                        return new PayloadResultSucceeded();
                    }
                    else
                    {
                        if(!allSucceeded && !allDoneNothing)
                        {
                            // some work was done but not all is finished
                            return new PayloadResultContinuationPending();
                        }
                        else
                        {
                            if(allSucceeded)
                            {
                                // all work is done
                                return new PayloadResultSucceeded();
                            }
                            else /*if(allDoneNothing)*/
                            {
                                // no work was done
                                return new PayloadResultContinuationPendingDoneNothing();
                            }
                        }
                    }
                }
            );
        }

        #region AwaitablesHolder implementation

        IEnumerable<WaitHandle> AwaitablesHolder.GetAwaitables()
        {
            return TransformCurrentPayloadsLeftToRunTo<IEnumerable<WaitHandle>>(
                whenNothingLeftToRun: () => Enumerable.Empty<WaitHandle>(),
                whenAnyLeftToRun: (payloadsNotYetSucceed) => payloadsNotYetSucceed.SelectMany(x => x.Payload.GetAwaitables())
            );
        }

        #endregion
    }
}