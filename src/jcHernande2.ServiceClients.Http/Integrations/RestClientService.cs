namespace jcHernande2.ServiceClients.Http.Integrations
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using jcHernande2.ServiceClients.Http.Models;
    using jcHernande2.ServiceClients.Http.Models.Exception;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using RestSharp;

    public class RestClientService : IHttpClient
    {
        private readonly RestClient restClient;
        private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new StringEnumConverter() },
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };
        public RestClientService(RestClient HttpClient)
        {
            restClient = HttpClient;
        }
        private async Task<TO> ExecuteAsync<TO, TI>(string url, TI data, HttpRequestOptions options, Method method)
        {
            var request = CreateRequest(url, data, options, method);
            var response = await restClient.ExecuteAsync(request).ConfigureAwait(false) as RestResponse;
            return HandleResponse<TO>(response);
        }

        private TO Execute<TO, TI>(string url, TI data, HttpRequestOptions options, Method method)
        {

            return ExecuteAsync<TO, TI>(url, data, options, method)
                .ConfigureAwait(false)
                .GetAwaiter().GetResult();
        }
        private RestRequest CreateRequest<TI>(string url, TI data,HttpRequestOptions options, Method method)
        {
            var request = new RestRequest(url, ToMethod(method));

            // Headers
            if (options?.Headers != null)
            {
                foreach (var header in options.Headers)
                {
                    request.AddHeader(header.Key, header.Value);
                }
            }

            // Query parameters
            if (options?.QueryParams != null)
            {
                foreach (var param in options.QueryParams)
                {
                    request.AddQueryParameter(param.Key, param.Value);
                }
            }

            if (data != null && method != Method.Get && method != Method.Delete)
            {
                var json = JsonConvert.SerializeObject(data, JsonSettings);
                request.AddJsonBody(json);
            }
            return request;
        }

        private TO HandleResponse<TO>(RestResponse response)
        {
            if (response == null)
            {
                throw new Exception("No response received from server.");
            }

            if (!response.IsSuccessful)
            {
                HandleErrorResponse(response);
            }

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new TimeoutException($"Request timeout: {response.ErrorMessage}");
            }

            // Treat 204 No Content or empty body as default(TO)
            if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(response.Content))
            {
                return default!;
            }

            try
            {
                return JsonConvert.DeserializeObject<TO>(response.Content);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to deserialize response: {response.Content}", ex);
            }
        }

        private void HandleErrorResponse(RestResponse response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new ServiceClientException("Unauthorized");

                case HttpStatusCode.BadRequest:
                    HandleBadRequest(response);
                    break;

                case HttpStatusCode.InternalServerError:
                    throw new Exception($"Internal Server Error: {response.Content}");

                case HttpStatusCode.MethodNotAllowed:
                    throw new Exception($"Method Not Allowed: {response.Content}");

                default:
                    throw new Exception($"HTTP Error {(int)response.StatusCode}: {response.Content}");
            }
        }

        private void HandleBadRequest(RestResponse response)
        {
            try
            {
                var exception = JsonConvert.DeserializeObject<ModelException>(response.Content);
                throw new ServiceClientException(exception?.Message ?? "Bad request", exception);
            }
            catch (JsonException)
            {
                throw new Exception($"Bad Request: {response.Content}");
            }
        }

        private Method ToMethod(Method method)
        {
            switch(method)
            {
                case Method.Get: return Method.Get;
                case Method.Post: return Method.Post;
                case Method.Put: return Method.Put;
                case Method.Delete: return Method.Delete;
                default: throw new NotImplementedException();
            };
        }
        private async Task<TO> SendAsync<TO, TI>(TI obj, AuthenticationHeaderValue? auth, HttpMethod httpMethod, string urlParams = "", CancellationToken cancellationToken = default)
        {
            var method = httpMethod.Method.ToUpper() switch
            {
                "GET" => Method.Get,
                "POST" => Method.Post,
                "PUT" => Method.Put,
                "DELETE" => Method.Delete,
                _ => throw new NotImplementedException(),
            };

            var request = new RestRequest(urlParams, method);

            if (auth != null)
            {
                request.AddHeader("Authorization", auth.ToString());
            }

            if (obj != null && method != Method.Get && method != Method.Delete)
            {
                var json = JsonConvert.SerializeObject(obj, JsonSettings);
                request.AddJsonBody(json);
            }

            var response = await restClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false) as RestResponse;
            return HandleResponse<TO>(response);
        }
        public string GetBaseUrl()
        {
            return this.restClient.Options.BaseUrl?.OriginalString;
        }
        public TO Post<TO, TI>(string path,TI data,HttpRequestOptions options = null)
        {
            return Execute<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.Post);
        }
        public async Task<TO> PostAsync<TO, TI>(string path, TI data, HttpRequestOptions options = null) 
        {
            return await ExecuteAsync<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.Post);
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
        public TO Get<TO>(string path, HttpRequestOptions options = null)
        {
            return Execute<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.Get);
        }
        public async Task<TO> GetAsync<TO>(string path, HttpRequestOptions options = null)
        {
            return await ExecuteAsync<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.Get);
        }
        public TO Put<TO, TI>(string path, TI data, HttpRequestOptions options = null)
        {
            return Execute<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.Put);
        }
        public async Task<TO> PutAsync<TO, TI>(string path, TI data, HttpRequestOptions options = null)
        {
            return await ExecuteAsync<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.Put);
        }
        public TO Delete<TO>(string path, HttpRequestOptions options = null)
        {
            return Execute<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.Delete);
        }
        public async Task<TO> DeleteAsync<TO>(string path, HttpRequestOptions options = null)
        {
            return await ExecuteAsync<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.Delete);
        }
        
    }
}
