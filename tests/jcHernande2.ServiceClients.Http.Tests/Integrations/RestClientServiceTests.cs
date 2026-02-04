namespace jcHernande2.ServiceClients.Http.Tests.Integrations
{
    using System;
    using System.Threading.Tasks;
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
    }
}