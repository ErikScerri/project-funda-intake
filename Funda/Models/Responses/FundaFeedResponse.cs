

using Newtonsoft.Json;

namespace Funda.Models.Responses
{
    public class FundaFeedResponse
    {
        [JsonProperty("Objects")]
        public required FundaFeedObject[] Objects { get; set; } = [];

        [JsonProperty("Paging")]
        public required FundaFeedPaging Paging { get; set; }

        [JsonProperty("TotaalAantalObjecten")]
        public required int TotalObjects { get; set; }
    }
}
