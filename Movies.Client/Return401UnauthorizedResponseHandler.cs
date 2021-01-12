using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Client
{
    /// <summary>
    /// Implementing a custom Handler to Allow Unit Testing with HttpClient
    /// No need to derive from DelegatingHandler Class since request doesn't need to be passed to inner handler
    /// No need to communicate with an actual API, but return a response to mock it
    /// </summary>
    public class Return401UnauthorizedResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            return Task.FromResult(response);
        }
    }
}
