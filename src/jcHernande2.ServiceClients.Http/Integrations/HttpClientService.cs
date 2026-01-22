namespace jcHernande2.ServiceClients.Http.Integrations
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
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
                throw new jcHernande2Exception(exception?.Message ?? "Bad request", exception);
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
                    throw new jcHernande2Exception("Unauthorized");

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
            if (response.StatusCode == HttpStatusCode.RequestTimeout)
            {
                throw new TimeoutException($"Request timeout: {response.ReasonPhrase}");
            }
            try
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    HandleErrorResponse(response, responseContent);
                }
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
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            foreach (var header in options?.Headers ?? new Dictionary<string, string>())
            {
                content.Headers.Add(header.Key, header.Value);
            }
            return content;
        }
        public string GetBaseUrl()
        {
            return "";
        }
        public TO Post<TO, TI>(string url, TI data, HttpRequestOptions options = null)
        {
           
            return PostAsync<TO, TI>(url, data, options)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
           
        }
        public async Task<TO> PostAsync<TO, TI>(string url, TI data, HttpRequestOptions options = null)
        {
            var content = CreateRequest(data, options);
            try
            {
                var response = await httpClient.PostAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}", content).ConfigureAwait(false);
                return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public TO Get<TO>(string url, HttpRequestOptions options = null)
        {
            return GetAsync<TO>(url, options)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            
        }
        public async Task<TO> GetAsync<TO>(string url, HttpRequestOptions options = null)
        {
            try
            {
                var response = await httpClient.GetAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}").ConfigureAwait(false);
                return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public TO Put<TO, TI>(string url, TI data, HttpRequestOptions options = null)
        {
            return PutAsync<TO, TI>(url, data, options)
                 .ConfigureAwait(false)
                 .GetAwaiter()
                 .GetResult();
        }
        public async Task<TO> PutAsync<TO, TI>(string url, TI data, HttpRequestOptions options = null)
        {
            var content = CreateRequest(data, options);
            try
            {
                var response = await httpClient.PutAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}", content).ConfigureAwait(false);
                return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public TO Delete<TO>(string url, HttpRequestOptions options = null)
        {
            return DeleteAsync<TO>(url, options)
                 .ConfigureAwait(false)
                 .GetAwaiter()
                 .GetResult();
        }
        public async Task<TO> DeleteAsync<TO>(string url, HttpRequestOptions options = null)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"{httpClient.BaseAddress.AbsoluteUri}{url}").ConfigureAwait(false);
                return await HandleResponseAsync<TO>(response).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<TO> SendAsync<TO, TI>(TI obj, string token, HttpMethod httpMethod, string urlParams = "")
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            });
            try 
            {
                var request = new HttpRequestMessage(httpMethod, $"{httpClient.BaseAddress.AbsoluteUri}{urlParams}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", token);

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.RequestTimeout || response.StatusCode == HttpStatusCode.GatewayTimeout)
                {
                    throw new TimeoutException($"La petición HTTP excedió el tiempo de espera configurado {response.StatusCode}.");
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var exception = JsonConvert.DeserializeObject<ModelException>(responseContent);
                        if (exception != null && !string.IsNullOrEmpty(exception.Message))
                        {
                            throw new jcHernande2Exception(exception.Message, exception);
                        }
                        else
                        {
                            throw new jcHernande2Exception("Bad request: Error al intentar deserializar response.Content con ModelException", exception);
                        }
                    }
                    catch (JsonException jsonEx)
                    {

                        throw new JsonException(
                            $"Bad request: No se pudo deserializar la respuesta del servidor. Content: {responseContent},{jsonEx.Message}");
                    }
                }
                response.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<TO>(responseContent);
            }
            catch (TaskCanceledException tex) when (!tex.CancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("La petición HTTP excedió el tiempo de espera configurado.", tex);
            }
            catch
            {
                throw;
            }
        }
    }
}
