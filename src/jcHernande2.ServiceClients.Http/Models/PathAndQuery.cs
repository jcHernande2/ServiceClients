namespace jcHernande2.ServiceClients.Http.Models
{
    using System.Collections.Generic;

    public class PathAndQuery
    {
        public string Path {  get; set; }
        public Dictionary<string, string> QueryParams { get; set; }
    }
}
