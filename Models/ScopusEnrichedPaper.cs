namespace ResearchAnalytics.Models
{
    public class ScopusEnrichedPaper
    {
        public string OriginalTitle { get; set; }
        public string OriginalSource { get; set; }

        public bool FoundInScopus { get; set; }

        public string ScopusTitle { get; set; }
        public string Authors { get; set; }
        public string PublicationName { get; set; }
        public string Year { get; set; }
        public string CitedByCount { get; set; }
        public string DOI { get; set; }
        public string Link { get; set; }
    }
}