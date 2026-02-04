namespace jcHernande2.ServiceClients.Http.Integrations
{
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using jcHernande2.ServiceClients.Http.Models;

    public interface IHttpClient
    {
        string GetBaseUrl();

        TO Post<TO, TI>(string url, TI data, HttpRequestOptions options = null);
        Task<TO> PostAsync<TO, TI>(string url, TI data, HttpRequestOptions options = null, CancellationToken cancellationToken = default);

        Task<TO> PostAuthenticatedAsync<TO, TI>(
            string urlOrRelativePath,
            TI body,
            AuthenticationHeaderValue auth,
            HttpRequestOptions options = null,
            CancellationToken cancellationToken = default);

        Task<TO> PostWithTokenAsync<TO, TI>(
            string urlOrRelativePath,
            TI body,
            string token,
            string scheme = "Bearer",
            HttpRequestOptions options = null,
            CancellationToken cancellationToken = default);

        TO Get<TO>(string url, HttpRequestOptions options = null);
        Task<TO> GetAsync<TO>(string url, HttpRequestOptions options = null, CancellationToken cancellationToken = default);

        TO Put<TO, TI>(string url, TI data, HttpRequestOptions options = null);
        Task<TO> PutAsync<TO, TI>(string url, TI data, HttpRequestOptions options = null, CancellationToken cancellationToken = default);

        TO Delete<TO>(string url, HttpRequestOptions options = null);
        Task<TO> DeleteAsync<TO>(string url, HttpRequestOptions options = null, CancellationToken cancellationToken = default);

        Task<TO> SendAsync<TO, TI>(TI obj, AuthenticationHeaderValue? auth, System.Net.Http.HttpMethod httpMethod, string urlParams = "", CancellationToken cancellationToken = default);
    }
}
