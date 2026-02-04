namespace jcHernande2.ServiceClients.Http.Tests.Integrations
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using jcHernande2.ServiceClients.Http.Integrations;
    using jcHernande2.ServiceClients.Http.Models.Exception;
    using jcHernande2.ServiceClients.Http.Tests.Models;
    using Xunit;

    public class HttpClientServiceTests
    {
        private readonly HttpClientService _httpClientService;

        public HttpClientServiceTests()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
            };
            _httpClientService = new HttpClientService(httpClient);
        }

        [Fact]
        public void Get_ShouldReturnData_Synchronously()
        {
            // Arrange
            var url = "posts/1";
            
            // Act
            var result = _httpClientService.Get<JsonResponse>(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task PostWithTokenAsync_ShouldReturnCreatedResource()
        {
            var req = new JsonRequest { Title = "foo", Body = "bar", UserId = 1 };
            var result = await _httpClientService.PostWithTokenAsync<JsonResponse, JsonRequest>("posts", req, "");

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task PostWithTokenAsync_SendsAuthorizationHeader()
        {
            // arrange
            HttpRequestMessage captured = null!;
            var handler = new DelegatingHandlerStub(req =>
            {
                captured = req;
                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.Created)
                {
                    Content = new StringContent("{ \"Id\": 123, \"recordsTotal\": 0, \"recordsFiltered\": 0, \"data\": [] }")
                };
                return resp;
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
            var svc = new HttpClientService(httpClient);

            // act
            var result = await svc.PostWithTokenAsync<JsonResponse, JsonRequest>("posts", new JsonRequest { Title = "t" , Body = "b", UserId = 1 }, "the-token", "Bearer");

            // assert
            Assert.NotNull(captured.Headers.Authorization);
            Assert.Equal("Bearer", captured.Headers.Authorization.Scheme);
            Assert.Equal("the-token", captured.Headers.Authorization.Parameter);
            Assert.Equal(123, result.Id);
        }

        [Fact]
        public async Task GetAsync_NoContent_ReturnsDefault()
        {
            var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
            var svc = new HttpClientService(httpClient);

            var result = await svc.GetAsync<JsonResponse>("empty");
            Assert.Null(result);
        }

        [Fact]
        public async Task PostWithTokenAsync_BadRequest_MapsModelException()
        {
            var modelJson = "{ \"Message\": \"Invalid\", \"ModelState\": { \"field\": [\"err\"] } }";
            var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent(modelJson)
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
            var svc = new HttpClientService(httpClient);

            var ex = await Assert.ThrowsAsync<ServiceClientException>(async () =>
                await svc.PostWithTokenAsync<JsonResponse, JsonRequest>("posts", new JsonRequest { Title = "t" }, "")
            );

            Assert.NotNull(ex.Model);
            var model = ex.Model as jcHernande2.ServiceClients.Http.Models.Exception.ModelException;
            Assert.Equal("Invalid", model?.Message);
        }

        // simple delegating handler to stub responses and capture requests
        private class DelegatingHandlerStub : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;
            public DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                this.responder = responder;
            }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(responder(request));
            }
        }
    }
}


