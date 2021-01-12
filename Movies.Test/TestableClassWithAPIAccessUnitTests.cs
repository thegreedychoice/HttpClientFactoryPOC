using Moq;
using Moq.Protected;
using Movies.Client;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Movies.Test
{
    /// <summary>
    /// Unit Testing APIs
    /// -Calls to the API can be avoided with custom handler
    ///     -Handler can return preset response
    ///     -Allows Unit Testing part of our code that would otherwise call the API
    /// -Or use a mocking framework like Moq, to mock handlers on the fly
    /// </summary>
    public class TestableClassWithAPIAccessUnitTests
    {
        [Fact]
        public void GetMovie_On401Response_MustThrowUnauthorizedApiAccessException_WithCustomHandler()
        {
            var httpClient = new HttpClient(new Return401UnauthorizedResponseHandler());
            var testableClass = new TestableClassWithAPIAccess(httpClient);

            var cancellationTokenSource = new CancellationTokenSource();
            Assert.ThrowsAsync<UnauthorizedApiAccessException>(
                () => testableClass.GetMovie(cancellationTokenSource.Token));
        }
        [Fact]
        public void GetMovie_On401Response_MustThrowUnauthorizedApiAccessException_WithMoq()
        {
            //we want to mock HttpHandler that returns 401 response

            //mock HttpHandler
            var unauthorizedResponseHttpMessageHandlerMock = new Mock<HttpMessageHandler>();

            //issue- we want to mock the result of sendAsync Method of HttpMessageHandler Class
            //but the method is protected and Moq can't automatically implement that
            //Can be manually implemented with Moq .Protected() extension methods


            //setup the protected method to mock (MOCKED HANDLER)
            unauthorizedResponseHttpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(  // return type of method - HttpResponseMessage
                "SendAsync",                        // Method name to mock
                ItExpr.IsAny<HttpRequestMessage>(), // any object of Type HttpRequestMessage for 1st parameter
                ItExpr.IsAny<CancellationToken>()   // any object of Type CancellationToken for 2nd parameter
                ).ReturnsAsync(new HttpResponseMessage()  // object that is returned from the method
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            var httpClient = new HttpClient(unauthorizedResponseHttpMessageHandlerMock.Object);

            var testableClass = new TestableClassWithAPIAccess(httpClient);

            var cancellationTokenSource = new CancellationTokenSource();

            Assert.ThrowsAsync<UnauthorizedApiAccessException>(
                () => testableClass.GetMovie(cancellationTokenSource.Token));

        }
    }
}
