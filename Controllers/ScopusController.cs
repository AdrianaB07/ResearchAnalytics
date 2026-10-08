using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchAnalytics.Services;

namespace ResearchAnalytics.Controllers
{
    [Authorize]
    public class ScopusController : Controller
    {
        private readonly ScopusService _scopusService;

        public ScopusController(ScopusService scopusService)
        {
            _scopusService = scopusService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ModelState.AddModelError("", "Please enter a publication title.");
                return View();
            }

            var results = await _scopusService.SearchPublicationsAsync(query, "Title");

            ViewBag.Query = query;

            return View(results);
        }
    }
}