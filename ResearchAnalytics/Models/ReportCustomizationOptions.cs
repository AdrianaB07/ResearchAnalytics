namespace ResearchAnalytics.Models
{
    public class ReportCustomizationOptions
    {
        public List<int> SelectedAuthorIds { get; set; } = new();

        public bool IncludeAuthorName { get; set; } = true;
        public bool IncludeSource { get; set; } = true;
        public bool IncludePaperCount { get; set; } = true;
        public bool IncludeAuthorCitations { get; set; } = true;

        public bool IncludeTitle { get; set; } = true;
        public bool IncludeYear { get; set; } = true;
        public bool IncludeCitations { get; set; } = true;
        public bool IncludeExternalPaperId { get; set; } = false;
        public bool IncludeUrl { get; set; } = true;

        public bool SeparateBySource { get; set; } = true;
    }
}