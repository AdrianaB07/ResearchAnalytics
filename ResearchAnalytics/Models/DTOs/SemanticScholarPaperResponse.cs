namespace ResearchAnalytics.Models.DTOs
{
    public class SemanticScholarPaperResponse
    {
        public List<SemanticScholarPaperItem> Data { get; set; }
    }

    public class SemanticScholarPaperItem
    {
        public string PaperId { get; set; }

        public string Title { get; set; }

        public int? Year { get; set; }

        public int CitationCount { get; set; }

        public string Url { get; set; }
    }
}