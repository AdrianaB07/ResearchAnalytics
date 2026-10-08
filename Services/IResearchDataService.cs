using ResearchAnalytics.Models;

namespace ResearchAnalytics.Services
{
    public interface IResearchDataService
    {
        List<SearchResult> GetResults(string queryText, int searchQueryId);
    }
}