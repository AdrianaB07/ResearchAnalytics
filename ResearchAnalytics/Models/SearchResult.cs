namespace ResearchAnalytics.Models
{
    public class SearchResult
    {
        public int SearchResultId { get; set; }

        public string AuthorName { get; set; }
        public string ExternalAuthorId { get; set; }

        public int PaperCount { get; set; }
        public int Citations { get; set; }

        public string Source { get; set; }

        // Foreign key
        public int SearchQueryId { get; set; }
        public SearchQuery SearchQuery { get; set; }
        public ICollection<ResearchPaper> Papers { get; set; }
    }
}