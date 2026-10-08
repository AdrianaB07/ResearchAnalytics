namespace ResearchAnalytics.Models
{
    public class OpenAlexAuthor
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public int WorksCount { get; set; }

        public int CitedByCount { get; set; }

        public string Institution { get; set; }
    }
}