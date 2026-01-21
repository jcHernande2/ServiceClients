namespace jcHernande2.ServiceClients.Http.Models.Exception
{
    using Newtonsoft.Json;

    public class ModelException
    {
        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("ModelState")]
        public Dictionary<string, string[]> Details { get; set; }
        
    }
}
