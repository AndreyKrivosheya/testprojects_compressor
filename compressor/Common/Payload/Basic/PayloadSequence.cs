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
            public bool Finished = false;
        }

        readonly List<PayloadWithState> Payloads;

        T TransformCurrentPayloadsLeftToRunTo<T>(Func<T> whenNothingLeftToRun, Func<IEnumerable<PayloadWithState>, T> whenAnyLeftToRun)
        {
            var payloadsUnfinished = Payloads.Where(x => !x.Finished);
            // if no unfinished paylaods
            if(!payloadsUnfinished.Any())
            {
                return whenNothingLeftToRun();
            }
            else
            {
                // if all unfinished payloads are either not mandatory or were never run
                if(!(payloadsUnfinished.Where(x => x.Mandatory || (!x.Mandatory && x.RanAtLeastOnce)).Any()))
                {
                    return whenNothingLeftToRun();
                }
                else
                {
                    return whenAnyLeftToRun(payloadsUnfinished);
                }
            }
        }

        protected override PayloadResult RunUnsafe(object parameter)
        {
            return TransformCurrentPayloadsLeftToRunTo<PayloadResult>(
                whenNothingLeftToRun: () => new PayloadResultSucceeded(),
                whenAnyLeftToRun:(payloadsUnfinished) => 
                {
                    var allSucceeded = true;
                    var allDoneNothing = true;
                    foreach(var payload in payloadsUnfinished)
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
                                            payload.Finished = true;
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
                whenAnyLeftToRun: (payloadsUnfinished) => payloadsUnfinished.SelectMany(x => x.Payload.GetAwaitables())
            );
        }

        #endregion
    }
}