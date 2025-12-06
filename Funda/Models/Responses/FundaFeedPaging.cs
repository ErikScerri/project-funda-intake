using Newtonsoft.Json;

namespace Funda.Models.Responses
{
    public class FundaFeedPaging
    {
        [JsonProperty("AantalPaginas")]
        public required int TotalPages { get; set; }

        [JsonProperty("HuidigePagina")]
        public required int CurrentPage { get; set; }
    }
}
