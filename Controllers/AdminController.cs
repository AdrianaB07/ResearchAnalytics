using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchAnalytics.Data;

namespace ResearchAnalytics.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalSearches = await _context.SearchQueries.CountAsync();
            ViewBag.TotalAuthors = await _context.SearchResults.CountAsync();
            ViewBag.TotalPapers = await _context.ResearchPapers.CountAsync();
            ViewBag.TotalCitations = await _context.ResearchPapers.SumAsync(p => p.CitationCount);

            ViewBag.RecentSearches = await _context.SearchQueries
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TopQueries = await _context.SearchQueries
                .GroupBy(s => s.QueryText)
                .Select(g => new
                {
                    QueryText = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}