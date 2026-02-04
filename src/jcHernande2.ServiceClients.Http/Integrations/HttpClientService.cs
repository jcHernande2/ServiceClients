namespace jcHernande2.ServiceClients.Http.Integrations
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using jcHernande2.ServiceClients.Http.Models;
    using jcHernande2.ServiceClients.Http.Models.Exception;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class HttpClientService : IHttpClient
    {
        private readonly System.Net.Http.HttpClient httpClient;
        public HttpClientService(System.Net.Http.HttpClient HttpClient)
        {
            httpClient = HttpClient;
        }
        private void HandleBadRequest(string responseContent)
        {
            try
            {
                var exception = JsonConvert.DeserializeObject<ModelException>(responseContent);
                throw new ServiceClientException(exception?.Message ?? "Bad request", exception);
            }
            catch (JsonException)
            {
                throw new Exception($"Bad Request: {responseContent}");
            }
        }
        private void HandleErrorResponse(HttpResponseMessage response, string responseContent)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new ServiceClientException("Unauthorized");

                case HttpStatusCode.BadRequest:
                    HandleBadRequest(responseContent);
                    break;

                case HttpStatusCode.InternalServerError:
                    throw new Exception($"Internal Server Error: {responseContent}");

                case HttpStatusCode.MethodNotAllowed:
                    throw new Exception($"Method Not Allowed: {responseContent}");

                default:
                    throw new Exception($"HTTP Error {(int)response.StatusCode}: {responseContent}");
            }
        }
        private async Task<TO> HandleResponseAsync<TO>(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new Exception("No response received from server.");
            }

            if (response.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new TimeoutException($"Request timeout: {response.ReasonPhrase}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                HandleErrorResponse(response, responseContent);
            }

            if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(responseContent))
            {
                return default!;
            }

            try
            {
                return JsonConvert.DeserializeObject<TO>(responseContent);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to deserialize response: {response.ReasonPhrase}", ex);
            }
        }

        private StringContent CreateRequest<TI>(TI data, HttpRequestOptions options)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            foreach (var header in options?.Headers ?? new Dictionary<string, string>())
            {
                content.Headers.Add(header.Key, header.Value);
            }
            return content;
        }

        public async Task<TO> SendAsync<TO, TI>(TI obj, AuthenticationHeaderValue? auth, HttpMethod httpMethod, string urlParams = "", CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            });

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var request = new HttpRequestMessage(httpMethod, $"{httpClient.BaseAddress.AbsoluteUri}{urlParams}")
            {
                Content = content,
            })
            {
                if (auth != null)
                {
                    request.Headers.Authorization = auth;
                }

                var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
            }
        }

        public string GetBaseUrl()
        {
            return httpClient.BaseAddress?.AbsoluteUri ?? string.Empty;
        }

        public TO Post<TO, TI>(string url, TI data, HttpRequestOptions options = null)
        {
            return PostAsync<TO, TI>(url, data, options)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<TO> PostAsync<TO, TI>(string url, TI data, HttpRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            using var content = CreateRequest(data, options);
            var response = await httpClient.PostAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}", content, cancellationToken).ConfigureAwait(false);
            return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
        }

        public Task<TO> PostWithTokenAsync<TO, TI>(
            string urlOrRelativePath,
            TI body,
            string token,
            string scheme = "Bearer",
            HttpRequestOptions options = null,
            CancellationToken cancellationToken = default)
            => PostAuthenticatedAsync<TO, TI>(urlOrRelativePath, body, string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue(scheme, token), options, cancellationToken);

        public async Task<TO> PostAuthenticatedAsync<TO, TI>(
            string urlOrRelativePath,
            TI body,
            AuthenticationHeaderValue auth,
            HttpRequestOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync<TO, TI>(body, auth, HttpMethod.Post, urlOrRelativePath, cancellationToken).ConfigureAwait(false);
        }

        public TO Get<TO>(string url, HttpRequestOptions options = null)
        {
            return GetAsync<TO>(url, options)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<TO> GetAsync<TO>(string url, HttpRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}", cancellationToken).ConfigureAwait(false);
            return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
        }

        public TO Put<TO, TI>(string url, TI data, HttpRequestOptions options = null)
        {
            return PutAsync<TO, TI>(url, data, options)
                 .ConfigureAwait(false)
                 .GetAwaiter()
                 .GetResult();
        }

        public async Task<TO> PutAsync<TO, TI>(string url, TI data, HttpRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            using var content = CreateRequest(data, options);
            var response = await httpClient.PutAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}", content, cancellationToken).ConfigureAwait(false);
            return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
        }

        public TO Delete<TO>(string url, HttpRequestOptions options = null)
        {
            return DeleteAsync<TO>(url, options)
                 .ConfigureAwait(false)
                 .GetAwaiter()
                 .GetResult();
        }

        public async Task<TO> DeleteAsync<TO>(string url, HttpRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.DeleteAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}", cancellationToken).ConfigureAwait(false);
            return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
        }
        
    }
}
