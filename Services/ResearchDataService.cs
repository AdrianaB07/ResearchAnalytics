using ResearchAnalytics.Models;

namespace ResearchAnalytics.Services
{
    public class ResearchDataService : IResearchDataService
    {
        public List<SearchResult> GetResults(string queryText, int searchQueryId)
        {
            return new List<SearchResult>
            {
                new SearchResult
                {
                    SearchQueryId = searchQueryId,
                    AuthorName = $"Author result for {queryText}",
                    ExternalAuthorId = "simulated-1",
                    Source = "Semantic Scholar (simulated)",
                    PaperCount = 25,
                    Citations = 300
                },
                new SearchResult
                {
                    SearchQueryId = searchQueryId,
                    AuthorName = $"Second author result for {queryText}",
                    ExternalAuthorId = "simulated-2",
                    Source = "Semantic Scholar (simulated)",
                    PaperCount = 12,
                    Citations = 120
                },
                new SearchResult
                {
                    SearchQueryId = searchQueryId,
                    AuthorName = $"Third author result for {queryText}",
                    ExternalAuthorId = "simulated-3",
                    Source = "Semantic Scholar (simulated)",
                    PaperCount = 5,
                    Citations = 45
                }
            };
        }
    }
}