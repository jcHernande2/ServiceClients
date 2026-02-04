namespace jcHernande2.ServiceClients.Http.Tests.Integrations
{
    using System;
    using System.Threading.Tasks;
    using System.Net;
    using System.Linq;
    using Newtonsoft.Json;
    using RestSharp;

    using jcHernande2.ServiceClients.Http.Integrations;
    using jcHernande2.ServiceClients.Http.Tests.Models;
    using Xunit;    

    public class RestClientServiceTests
    {
        private readonly RestClientService _restClientService;

        public RestClientServiceTests()
        {
            var restClient = new RestClient("https://jsonplaceholder.typicode.com/");
            _restClientService = new RestClientService(restClient);
        }

        [Fact]
        public async Task PostWithTokenAsync_ShouldReturnCreatedResource()
        {
            var req = new JsonRequest { Title = "foo", Body = "bar", UserId = 1 };
            var result = await _restClientService.PostWithTokenAsync<JsonResponse, JsonRequest>("posts", req, "");

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task PostWithTokenAsync_SendsAuthorizationHeader()
        {
            // arrange
            System.Net.Http.HttpRequestMessage captured = null!;
            var handler = new DelegatingHandlerStub(req =>
            {
                captured = req;
                var resp = new System.Net.Http.HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new System.Net.Http.StringContent("{ \"Id\": 123 }")
                };
                return resp;
            });

            var httpClient = new System.Net.Http.HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
            var restClient = new RestClient(httpClient);
            var svc = new RestClientService(restClient);

            // act
            var result = await svc.PostWithTokenAsync<JsonResponse, JsonRequest>("posts", new JsonRequest { Title = "t", Body = "b", UserId = 1 }, "the-token", "Bearer");

            // assert
            Assert.NotNull(captured.Headers.Authorization);
            Assert.Equal("Bearer", captured.Headers.Authorization.Scheme);
            Assert.Equal("the-token", captured.Headers.Authorization.Parameter);
            Assert.Equal(123, result.Id);
        }

        // simple delegating handler to stub responses and capture requests (keeps parity with HttpClientService tests)
        private class DelegatingHandlerStub : System.Net.Http.DelegatingHandler
        {
            private readonly Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> responder;
            public DelegatingHandlerStub(Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> responder)
            {
                this.responder = responder;
            }
            protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                return Task.FromResult(responder(request));
            }
        }
    }
}