using Marvin.StreamExtensions;
using Movies.Client.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Client.Services
{
    /// <summary>
    /// Good use cases for delegating handlers
    /// - Retry Logic
    /// - Timeout
    /// - Authentication for access tokens
    /// -Client-side cache implementation
    /// - Implementing HSTS support (Maarten Balliauw wrote a handler for this on github)
    /// </summary>
    public class HttpHandlersService : IIntegrationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        //If not using HttpCLientFactory, create pipeline like this
        private static HttpClient _notSoNicelyInstantiatedHttpClient =
            new HttpClient(
                new RetryPolicyDelegatingHandler(
                    new HttpClientHandler()
                    { AutomaticDecompression = System.Net.DecompressionMethods.GZip },
                    2));

        public HttpHandlersService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task Run()
        {
            await GetMoviesWithRetryPolicy(_cancellationTokenSource.Token);
        }

        private async Task GetMoviesWithRetryPolicy(CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("MoviesClient");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/movies/5b1c2b4d-48c7-402a-80c3-cc796ad49c6b");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            using (var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    //inspect the status code
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        //show this to the user
                        Console.WriteLine("The requested movie can't be found!");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    //trigger a login flow
                    return;
                }

                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();

                var movie = stream.ReadAndDeserializeFromJson<Movie>();
            }
        }
    }
}
