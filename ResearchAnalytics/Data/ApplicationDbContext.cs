using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ResearchAnalytics.Models;

namespace ResearchAnalytics.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SearchQuery> SearchQueries { get; set; }
        public DbSet<SearchResult> SearchResults { get; set; }
        public DbSet<ResearchPaper> ResearchPapers { get; set; }
    }
}
