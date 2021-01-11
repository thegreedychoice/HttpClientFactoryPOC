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
    /// Problem with HttpClient
    /// WHen we dispose HttpClient the underlying HttpClientHandler is disposed, which closes the 
    /// underlying connection
    /// 
    /// When we resuse HttpClient/HttpCLientHandler instances( and thus the connection), DNS changes aren't honoured
    /// -Can lead to requests not arrivng at the correct server 
    /// -leads to issues when using Azure PaaS services
    /// HttpClient factory can be used to solve these problem
    /// Introduced with .Net Core 2.1 and used to create and manage instances of HttpClient and underlying handler(s)
    /// 
    /// How does it do it?
    /// HttpCLientFactory creates HttpCLient instance and then HttpClient instead of creating HttpClientHandler
    /// which is the primary Http message handler, it takes one from the pool of HttpMessageHandlers
    /// And then the picked HttpMessageHandler is used to call the API. The life of handler is 2 mins
    /// If a new instances of HttpCLient is created from the factory, it can still reuse the HttpMessageHandler
    /// from the pool previously created with an active underlying connection.
    /// 
    /// Reusing handlers like this from the pool allows resusing the underlying connection which solves the 
    /// socket exhaustion issue(opening multiple socket connections for multiple requests)
    /// 
    /// And since the handlers are disposed after 2 minutes(=default), it solves the DNS issues by taking into account
    /// new changes 
    /// 
    /// HttpCLientFactory provides a central location for naming and configuring logical HttpClients. This is very 
    /// helpful when dealing with multiple api clients (microservices). For these, we can configure delegating handlers
    /// and policies(using Polly)
    /// 
    /// 
    /// HttpClientFactory can have direct, named, typed HttpCLient instances
    /// 
    /// </summary>
    public class HttpClientFactoryInstanceManagementService : IIntegrationService
    {
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MoviesClient _moviesClient;

        public HttpClientFactoryInstanceManagementService(IHttpClientFactory httpClientFactory,
            MoviesClient moviesClient)
        {
            _httpClientFactory = httpClientFactory;
            _moviesClient = moviesClient;
        }

        public async Task Run()
        {
            //await TestDisposeHttpClient(_cancellationTokenSource.Token);
            //await TestReuseHttpClient(_cancellationTokenSource.Token);
            //await GetMoviesWithHttpClientFromFactory(_cancellationTokenSource.Token);
            //await GetMoviesWithNamedHttpClientFromFactory(_cancellationTokenSource.Token);
            //await GetMoviesWithTypedHttpClientFromFactory(_cancellationTokenSource.Token);
            await GetMoviesViaMovieClient(_cancellationTokenSource.Token);
        }

        private async Task TestDisposeHttpClient(CancellationToken cancellationToken)
        {
            for (var i = 0; i < 10; i++)
            {
                using(var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        "https://www.google.com");

                    using (var response = await httpClient.SendAsync(request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        response.EnsureSuccessStatusCode();

                        Console.WriteLine($"Request completed with Status Code" + 
                            $" {response.StatusCode}");
                    }
                }
            }
        }

        private async Task TestReuseHttpClient(CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient();

            for (var i = 0; i < 10; i++)
            {
                var request = new HttpRequestMessage(
                                        HttpMethod.Get,
                                        "https://www.google.com");

                using (var response = await httpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    response.EnsureSuccessStatusCode();

                    Console.WriteLine($"Request completed with Status Code" +
                        $" {response.StatusCode}");
                }
            }
        }

        private async Task GetMoviesWithHttpClientFromFactory(
            CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost:57863/api/movies");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using (var response = await httpClient.SendAsync(request, 
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                var stream = await response.Content.ReadAsStreamAsync();
                response.EnsureSuccessStatusCode();
                var movies = stream.ReadAndDeserializeFromJson<List<Movie>>();
            }
        }

        private async Task GetMoviesWithNamedHttpClientFromFactory(
             CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("MoviesClient");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/movies");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            using (var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                var stream = await response.Content.ReadAsStreamAsync();
                response.EnsureSuccessStatusCode();
                var movies = stream.ReadAndDeserializeFromJson<List<Movie>>();
            }
        }

        //private async Task GetMoviesWithTypedHttpClientFromFactory(
        //    CancellationToken cancellationToken)
        //{
        //    var request = new HttpRequestMessage(
        //        HttpMethod.Get,
        //        "api/movies");
        //    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        //    using (var response = await _moviesClient.Client.SendAsync(request,
        //        HttpCompletionOption.ResponseHeadersRead,
        //        cancellationToken))
        //    {
        //        var stream = await response.Content.ReadAsStreamAsync();
        //        response.EnsureSuccessStatusCode();
        //        var movies = stream.ReadAndDeserializeFromJson<List<Movie>>();
        //    }
        //}

        private async Task GetMoviesViaMovieClient(CancellationToken cancellationToken)
        {
            var movies = await _moviesClient.GetMovies(cancellationToken);
        }
    }
}
