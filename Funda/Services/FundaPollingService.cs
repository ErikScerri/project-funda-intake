using Funda.Models.Data;
using Funda.Models.Responses;
using Newtonsoft.Json;
using System.Threading.RateLimiting;

namespace Funda.Services
{
    public class FundaPollingService : IFundaPollingService
    {
        private const string API_KEY = "76666a29898f491480386d966b75f949";

        private const string BASE_URL = "http://partnerapi.funda.nl/feeds/Aanbod.svc/json";

        private readonly ILogger logger;

        private readonly HttpClient client;
        private readonly FixedWindowRateLimiter limiter;

        public FundaPollingService(ILogger<FundaPollingService> logger)
        {
            this.logger = logger;

            client = new HttpClient();

            // Defines a hardcoded limited of 100 calls per minute window
            // Allows a queue of requests to build up that is then processed correctly with each minute window
            FixedWindowRateLimiterOptions options = new()
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            };

            limiter = new FixedWindowRateLimiter(options);
        }

        /// <inheritdoc/>
        public async Task<List<MakelaarData>> PollMakelaars(string type, string[] queries, int pageSize = 25)
        {
            Dictionary<int, MakelaarData> makelaars = [];

            string query = string.Join("/", queries);
            string url = $"{BASE_URL}/{API_KEY}/?type={type}&zo=/{query}/&pagesize={pageSize}";

            logger.LogInformation("Polling makelaars for feed [{type}] with query [{query}] at {time}",
                type, query, DateTime.UtcNow.ToLongTimeString());

            // We need to at minimum call page 1 to determine total amount of pages.
            // If there is no data to retrieve, page 1 still returns pagination data we can use to close the loop.
            int totalPages = 1;

            for (int page = 1; page <= totalPages; page++)
            {
                FundaFeedResponse? responseData = await GetPaginatedResponse(url, page);

                if (responseData == null)
                {
                    logger.LogError("Page {page} of poll [{type}->{query}] failed to return data.",
                        page, type, query);
                    return [];
                }

                if (page == 1)
                {
                    // With the first call, we use the embedded pagination data to extend the loop to the total amount of pages.
                    totalPages = responseData.Paging.TotalPages;
                }

                ProcessMakelaarData(responseData, makelaars);
            }

            logger.LogInformation("Finished processing {sum} objects for feed [{type}] with query [{query}] at {time}",
                makelaars.Values.Sum(m => m.ListingCount), type, query, DateTime.UtcNow.ToLongTimeString());

            return [.. makelaars.Values];
        }

        /// <summary>
        /// Fetches and deserializes a single page from the feed into a <see cref="FundaFeedResponse"/>
        /// that can then be processed further in <see cref="ProcessMakelaarData"/>.
        /// </summary>
        /// <param name="url">The base URL of the request, including queries and page size.</param>
        /// <param name="page">The specific page requested for the given base URL.</param>
        /// <returns>A <see cref="FundaFeedResponse"/> that contains pagination and object data.</returns>
        private async Task<FundaFeedResponse?> GetPaginatedResponse(string url, int page)
        {
            await limiter.AcquireAsync(1);

            logger.LogInformation("Requested {url}&page={page} at {time}", url, page, DateTime.UtcNow.ToLongTimeString());
            HttpResponseMessage response = await client.GetAsync($"{url}&page={page}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Request failed with code {code} {status} ({phrase})",
                    (int)response.StatusCode,
                    response.StatusCode,
                    response.ReasonPhrase);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<FundaFeedResponse>(json);
        }

        /// <summary>
        /// Processes a given <see cref="FundaFeedResponse"/> into a dictionary of makelaar IDs to name and listing counts.
        /// The dictionary is passed in as a parameter, allowing a running tally from previous process data calls.
        /// </summary>
        /// <param name="data">The current batch of <see cref="FundaFeedResponse"/> data to process.</param>
        /// <param name="makelaars">The dictionary into which to process the data.</param>
        private static void ProcessMakelaarData(FundaFeedResponse data, Dictionary<int, MakelaarData> makelaars)
        {
            foreach (FundaFeedObject listing in data.Objects)
            {
                if (!makelaars.ContainsKey(listing.MakelaarId))
                {
                    makelaars[listing.MakelaarId] = new MakelaarData()
                    {
                        Id = listing.MakelaarId,
                        Name = listing.MakelaarName,
                        ListingCount = 0
                    };
                }

                makelaars[listing.MakelaarId].ListingCount += 1;
            }
        }
    }
}
