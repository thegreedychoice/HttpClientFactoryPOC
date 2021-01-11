using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Client
{
    public class TimeOutDelegatingHandler : DelegatingHandler
    {
        private readonly TimeSpan _timeOut = TimeSpan.FromSeconds(100);

        public TimeOutDelegatingHandler(TimeSpan timeOut)
            : base()
        {
            _timeOut = timeOut;
        }

        public TimeOutDelegatingHandler(HttpMessageHandler innerHandler, TimeSpan timeOut) 
            : base(innerHandler)
        {
            _timeOut = timeOut;
        }

        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //create a new linked cancelled state tokensource, ensures when new one reaches a cancelled state,
            //the original one is cancelled as well
            using (var linkedCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                //cancel the new token when the timeout has been reached
                linkedCancellationTokenSource.CancelAfter(_timeOut);
                try
                {
                    return await base.SendAsync(request, linkedCancellationTokenSource.Token);
                }
                catch (OperationCanceledException ex) //if cancellation of original token happens
                {
                    //check if the original token was cancelled (not because linked token was cancelled)
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        //enters only if linkedtokensource was cancelled after timeout as stated above
                        throw new TimeoutException("The request timed out.", ex);
                    }
                    throw; //operation cancelled not via timeout but something else
                }
            }
        }
    }
}
