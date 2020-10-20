using System;
using System.Linq;
using System.Text;

namespace compressor.Common
{
    static class ExceptionExtensions
    {
        public static string GetMessagesChain(this AggregateException e)
        {
            if(e.InnerExceptions.Count > 0)
            {
                return string.Format("{0} => [{1}]", e.Message, string.Join(", ", e.InnerExceptions.Select(x => x.GetMessagesChain())));
            }
            else
            {
                return e.Message;
            }
        }
        public static string GetMessagesChain(this Exception e)
        {
            var eAsAggregate = e as AggregateException;
            if(eAsAggregate != null)
            {
                return eAsAggregate.GetMessagesChain();
            }
            else
            {
                if(e.InnerException != null)
                {
                    return string.Format("{0} => {1}", e.Message, e.InnerException.GetMessagesChain());
                }
                else
                {
                    return e.Message;
                }
            }
        }
    }
}