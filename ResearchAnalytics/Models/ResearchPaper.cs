namespace ResearchAnalytics.Models
{
    public class ResearchPaper
    {
        public int ResearchPaperId { get; set; }

        public string ExternalPaperId { get; set; }
        public string Title { get; set; }

        public int? Year { get; set; }
        public int CitationCount { get; set; }

        public string Url { get; set; }

        public int SearchResultId { get; set; }
        public SearchResult SearchResult { get; set; }
    }
}