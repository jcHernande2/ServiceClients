namespace jcHernande2.ServiceClients.Http.Tests.Models
{
    using Newtonsoft.Json;
    public class JsonResponse
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("recordsTotal")]
        public long RecordsTotal { get; set; }

        [JsonProperty("recordsFiltered")]
        public long RecordsFiltered { get; set; }

        [JsonProperty("data")]
        public object[] Data { get; set; }
    }
}