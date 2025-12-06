using Funda.Models.Data;

namespace Funda.Services
{
    public interface IFundaPollingService
    {
        /// <summary>
        /// Polls the funda API for a given feed, retrieving <see cref="MakelaarData"/> about makelaars within the feed.
        /// </summary>
        /// <param name="type">The feed to poll, e.g. "koop".</param>
        /// <param name="queries">The list of queries to filter the feed by, e.g. ["amsterdam", "tuin"].</param>
        /// <param name="pageSize">The size of each pagination page retrieved from the API.</param>
        /// <returns>A list of <see cref="MakelaarData"/> with ID, name, and listing count from the given feed.</returns>
        Task<List<MakelaarData>> PollMakelaars(string type, string[] queries, int pageSize = 25);
    }
}
