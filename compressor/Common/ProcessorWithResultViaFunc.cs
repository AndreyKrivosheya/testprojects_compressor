using System;
using System.Threading;

namespace compressor.Common
{
    class ProcessorWithResultViaFunc<T> : ProcessorWithResult<T>
    {
        public ProcessorWithResultViaFunc(Func<T> funcToRun)
        {
            if(funcToRun == null)
            {
                throw new ArgumentNullException("funcToRun");
            }

            this.FuncToRun = funcToRun;
        }

        readonly Func<T> FuncToRun;

        protected sealed override T RunOnThread()
        {
            return FuncToRun();
        }
    }
}