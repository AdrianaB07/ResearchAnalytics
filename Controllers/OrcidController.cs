using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchAnalytics.Models;
using ResearchAnalytics.Services;

namespace ResearchAnalytics.Controllers
{
    [Authorize]
    public class OrcidController : Controller
    {
        private readonly OrcidService _orcidService;
        private readonly ScopusService _scopusService;

        public OrcidController(OrcidService orcidService, ScopusService scopusService)
        {
            _orcidService = orcidService;
            _scopusService = scopusService;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> Profile(string orcidId)
        {
            if (string.IsNullOrWhiteSpace(orcidId))
            {
                return RedirectToAction("Index", "Search");
            }

            var profile = await _orcidService.GetProfileByIdAsync(orcidId);

            if (profile == null)
            {
                TempData["Error"] = "No public ORCID profile found for this ID.";
                return RedirectToAction("Index", "Search");
            }

            return View(profile);
        }

        public async Task<IActionResult> Works(string orcidId)
        {
            if (string.IsNullOrWhiteSpace(orcidId))
            {
                return RedirectToAction("Index", "Search");
            }

            var profile = await _orcidService.GetProfileByIdAsync(orcidId);
            var works = await _orcidService.GetWorksAsync(orcidId);

            ViewBag.OrcidId = orcidId;
            ViewBag.AuthorName = profile?.FullName ?? "Unknown researcher";
            ViewBag.ProfileUrl = profile?.ProfileUrl;

            return View(works);
        }
        public async Task<IActionResult> EnrichWorksWithScopus(string orcidId, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(orcidId))
            {
                return RedirectToAction(nameof(Index));
            }

            var profile = await _orcidService.GetProfileByIdAsync(orcidId);
            var works = await _orcidService.GetWorksAsync(orcidId);

            var worksToCheck = works
                .Where(w => !string.IsNullOrWhiteSpace(w.Title))
                .Take(limit)
                .ToList();

            var semaphore = new SemaphoreSlim(3);

            var enrichmentTasks = worksToCheck.Select(async work =>
            {
                await semaphore.WaitAsync();

                try
                {
                    return await _scopusService
                        .EnrichTitleWithScopusAsync(work.Title, "ORCID");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var enrichedWorks = (await Task.WhenAll(enrichmentTasks)).ToList();
            HttpContext.Session.SetString(
                "OrcidScopusEnrichment",
                System.Text.Json.JsonSerializer.Serialize(enrichedWorks));


            ViewBag.OrcidId = orcidId;
            ViewBag.AuthorName = profile?.FullName ?? "Unknown researcher";
            ViewBag.TotalWorks = works.Count;
            ViewBag.CheckedWorks = worksToCheck.Count;
            ViewBag.CurrentLimit = limit;
            ViewBag.NextLimit = limit + 5;
            ViewBag.HasMore = limit < works.Count;
            ViewBag.FoundInScopus = enrichedWorks.Count(w => w.FoundInScopus);

            return View("ScopusEnrichment", enrichedWorks);
        }

        public async Task<IActionResult> ExportOrcidScopusEnrichmentToExcel(string orcidId, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(orcidId))
            {
                return RedirectToAction(nameof(Index));
            }

            var profile = await _orcidService.GetProfileByIdAsync(orcidId);
            var works = await _orcidService.GetWorksAsync(orcidId);

            var worksToCheck = works
                .Where(w => !string.IsNullOrWhiteSpace(w.Title))
                .Take(limit)
                .ToList();

            var semaphore = new SemaphoreSlim(3);

            var enrichmentTasks = worksToCheck.Select(async work =>
            {
                await semaphore.WaitAsync();

                try
                {
                    return await _scopusService
                        .EnrichTitleWithScopusAsync(work.Title, "ORCID");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var enrichedWorks = (await Task.WhenAll(enrichmentTasks)).ToList();

            using var workbook = new ClosedXML.Excel.XLWorkbook();

            var ws = workbook.Worksheets.Add("ORCID Scopus Enrichment");

            ws.Cell("A1").Value = "ORCID Scopus Enrichment Report";
            ws.Range("A1:H1").Merge();
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            ws.Cell("A3").Value = "Researcher";
            ws.Cell("B3").Value = profile?.FullName ?? "Unknown researcher";

            ws.Cell("A4").Value = "ORCID iD";
            ws.Cell("B4").Value = orcidId;

            ws.Cell("A5").Value = "Checked Works";
            ws.Cell("B5").Value = worksToCheck.Count;

            ws.Cell("A6").Value = "Found in Scopus";
            ws.Cell("B6").Value = enrichedWorks.Count(w => w.FoundInScopus);

            ws.Cell("A8").Value = "Original ORCID Title";
            ws.Cell("B8").Value = "Found in Scopus";
            ws.Cell("C8").Value = "Scopus Title";
            ws.Cell("D8").Value = "Journal";
            ws.Cell("E8").Value = "Year";
            ws.Cell("F8").Value = "Scopus Citations";
            ws.Cell("G8").Value = "DOI";
            ws.Cell("H8").Value = "Link";

            ws.Range("A8:H8").Style.Font.Bold = true;

            int row = 9;

            foreach (var item in enrichedWorks)
            {
                ws.Cell(row, 1).Value = item.OriginalTitle;
                ws.Cell(row, 2).Value = item.FoundInScopus ? "Yes" : "No";
                ws.Cell(row, 3).Value = item.ScopusTitle;
                ws.Cell(row, 4).Value = item.PublicationName;
                ws.Cell(row, 5).Value = item.Year;
                ws.Cell(row, 6).Value = item.CitedByCount;
                ws.Cell(row, 7).Value = item.DOI;
                ws.Cell(row, 8).Value = item.Link;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"ORCID_Scopus_Enrichment_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}