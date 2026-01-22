namespace jcHernande2.ServiceClients.Http.Tests.Integrations
{
    using System;
    using System.Net.Http;
    
    using jcHernande2.ServiceClients.Http.Integrations;

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
    }
}


