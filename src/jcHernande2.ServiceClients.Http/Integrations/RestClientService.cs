namespace jcHernande2.ServiceClients.Http.Integrations
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using jcHernande2.ServiceClients.Http.Models;
    using jcHernande2.ServiceClients.Http.Models.Exception;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using RestSharp;
    using DataMethod=jcHernande2.ServiceClients.Http.Data.Method;
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
        private async Task<TO> ExecuteAsync<TO, TI>(string url, TI data,HttpRequestOptions options,Method method )
        {
            var request = CreateRequest(url, data, options,method);
            try
            {
                var response = await restClient.ExecuteAsync(request).ConfigureAwait(false) as RestResponse;
                return HandleResponse<TO>(response);
            }
            catch (Exception) {
                throw;
            }
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
            if (!response.IsSuccessful)
            {
                HandleErrorResponse(response);
            }

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new TimeoutException($"Request timeout: {response.ErrorMessage}");
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
                    throw new jcHernande2Exception("Unauthorized");

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
                throw new jcHernande2Exception(exception?.Message ?? "Bad request", exception);
            }
            catch (JsonException)
            {
                throw new Exception($"Bad Request: {response.Content}");
            }
        }

        private Method ToMethod(jcHernande2.ServiceClients.Http.Data.Method method)
        {
            switch(method)
            {
                case jcHernande2.ServiceClients.Http.Data.Method.GET: return Method.Get;
                case jcHernande2.ServiceClients.Http.Data.Method.POST: return Method.Post;
                case jcHernande2.ServiceClients.Http.Data.Method.PUT: return Method.Put;
                case jcHernande2.ServiceClients.Http.Data.Method.DELETE: return Method.Delete;
                default: throw new NotImplementedException();
            };
        }
        public string GetBaseUrl()
        {
            return this.restClient.Options.BaseUrl?.OriginalString;
        }
        public TO Post<TO, TI>(string path,TI data,HttpRequestOptions options = null)
        {
            return Execute<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.POST);
        }
        public async Task<TO> PostAsync<TO, TI>(string path, TI data, HttpRequestOptions options = null) 
        {
            return await ExecuteAsync<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.POST);
        }
        public TO Get<TO>(string path, HttpRequestOptions options = null)
        {
            return Execute<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.GET);
        }
        public async Task<TO> GetAsync<TO>(string path, HttpRequestOptions options = null)
        {
            return await ExecuteAsync<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.GET);
        }
        public TO Put<TO, TI>(string path, TI data, HttpRequestOptions options = null)
        {
            return Execute<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.PUT);
        }
        public async Task<TO> PutAsync<TO, TI>(string path, TI data, HttpRequestOptions options = null)
        {
            return await ExecuteAsync<TO, TI>($"{restClient.Options.BaseUrl?.OriginalString}{path}", data, options, Method.PUT);
        }
        public TO Delete<TO>(string path, HttpRequestOptions options = null)
        {
            return Execute<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.DELETE);
        }
        public async Task<TO> DeleteAsync<TO>(string path, HttpRequestOptions options = null)
        {
            return await ExecuteAsync<TO, object>($"{restClient.Options.BaseUrl?.OriginalString}{path}", null, options, Method.Delete);
        }
    }
}
