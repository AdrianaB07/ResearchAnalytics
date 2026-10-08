
using Microsoft.AspNetCore.Identity;

namespace ResearchAnalytics.Models
{

    public class SearchQuery
    {
        public int SearchQueryId { get; set; }

        public string QueryText { get; set; }

        public DateTime CreatedAt { get; set; }

        // Foreign key către Identity User
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public ICollection<SearchResult> Results { get; set; }
        public string? OrcidId { get; set; }
    }
}
