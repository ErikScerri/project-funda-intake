using Newtonsoft.Json;

namespace Funda.Models.Responses
{
    public class FundaFeedObject
    {
        [JsonProperty("MakelaarId")]
        public required int MakelaarId { get; set; }

        [JsonProperty("MakelaarNaam")]
        public required string MakelaarName { get; set; }
    }
}
