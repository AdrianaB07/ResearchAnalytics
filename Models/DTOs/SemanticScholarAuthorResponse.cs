namespace ResearchAnalytics.Models.DTOs
{
    public class SemanticScholarAuthorResponse
    {
        public List<SemanticScholarAuthor> Data { get; set; }
    }

    public class SemanticScholarAuthor
    {
        public string AuthorId { get; set; }

        public string Name { get; set; }

        public int PaperCount { get; set; }

        public int CitationCount { get; set; }
    }
}