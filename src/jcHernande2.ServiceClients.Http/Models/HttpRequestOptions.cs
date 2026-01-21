namespace jcHernande2.ServiceClients.Http.Models
{
    using System.Collections.Generic;

    public class HttpRequestOptions
    {
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> QueryParams { get; set; }
    }
}
