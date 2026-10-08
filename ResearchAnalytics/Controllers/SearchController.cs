using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchAnalytics.Data;
using ResearchAnalytics.Models;
using ResearchAnalytics.Services;
using System.Security.Claims;
using ClosedXML.Excel;

namespace ResearchAnalytics.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SemanticScholarTestService _semanticScholarService;
        private readonly OpenAlexService _openAlexService;
        private readonly ScopusService _scopusService;

        public SearchController(
            ApplicationDbContext context,
            SemanticScholarTestService semanticScholarService, OpenAlexService openAlexService, ScopusService scopusService)
        {
            _context = context;
            _semanticScholarService = semanticScholarService;
            _openAlexService = openAlexService;
            _scopusService = scopusService;
        }

        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> New(string queryText, string? orcidId)
        {
            if (string.IsNullOrWhiteSpace(queryText))
            {
                ModelState.AddModelError("", "Please enter a search query.");
                return View();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var search = new SearchQuery
            {
                QueryText = queryText,
                CreatedAt = DateTime.Now,
                UserId = userId,
                OrcidId = orcidId
            };

            _context.SearchQueries.Add(search);
            await _context.SaveChangesAsync();

            var semanticResults = new List<SearchResult>();

            try
            {
                semanticResults = await _semanticScholarService
                    .GetAuthorsAsync(queryText, search.SearchQueryId);
            }
            catch
            {
                semanticResults = new List<SearchResult>();
            }

            var openAlexResults = new List<SearchResult>();

            try
            {
                openAlexResults = await _openAlexService
                    .SearchAuthorsAsync(queryText, search.SearchQueryId);
            }
            catch
            {
                openAlexResults = new List<SearchResult>();
            }

            var results = semanticResults
                .Concat(openAlexResults)
                .ToList();

            _context.SearchResults.AddRange(results);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = search.SearchQueryId });
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var searches = _context.SearchQueries
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            return View(searches);
        }

        public IActionResult Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var search = _context.SearchQueries
                .Include(s => s.Results.OrderByDescending(r => r.Citations))
                .FirstOrDefault(s => s.SearchQueryId == id && s.UserId == userId);

            if (search == null)
            {
                return NotFound();
            }

            return View(search);
        }

        public async Task<IActionResult> AuthorDetails(int id)
        {
            var result = await _context.SearchResults
                .Include(r => r.Papers)
                .FirstOrDefaultAsync(r => r.SearchResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            // Dacă nu avem papers salvate deja
            if (result.Papers == null || !result.Papers.Any())
            {
                List<ResearchPaper> papers;

                if (result.Source != null && result.Source.StartsWith("OpenAlex"))
                {
                    papers = await _openAlexService
                        .GetAuthorWorksAsync(result.ExternalAuthorId, result.SearchResultId);
                }
                else
                {
                    papers = await _semanticScholarService
                        .GetAuthorPapersAsync(result.ExternalAuthorId, result.SearchResultId);
                }

                _context.ResearchPapers.AddRange(papers);
                await _context.SaveChangesAsync();

                result.Papers = papers;
            }

            var loadedPapers = result.Papers ?? new List<ResearchPaper>();

            var yearlyStats = loadedPapers
                .Where(p => p.Year.HasValue)
                .GroupBy(p => p.Year.Value)
                .Select(g => new
                {
                    Year = g.Key,
                    PaperCount = g.Count(),
                    CitationCount = g.Sum(p => p.CitationCount)
                })
                .OrderBy(x => x.Year)
                .ToList();

            double CalculateTrendScore(List<double> actualVector)
            {
                if (actualVector.Count < 2)
                {
                    return 0;
                }

                var idealVector = Enumerable
                    .Range(1, actualVector.Count)
                    .Select(x => (double)x)
                    .ToList();

                double dot = actualVector.Zip(idealVector, (a, b) => a * b).Sum();
                double actualNorm = Math.Sqrt(actualVector.Sum(x => x * x));
                double idealNorm = Math.Sqrt(idealVector.Sum(x => x * x));

                if (actualNorm == 0 || idealNorm == 0)
                {
                    return 0;
                }

                return dot / (actualNorm * idealNorm);
            }

            var productivityVector = yearlyStats
                .Select(x => (double)x.PaperCount)
                .ToList();

            var citationImpactVector = yearlyStats
                .Select(x => (double)x.CitationCount)
                .ToList();

            var productivityScore = CalculateTrendScore(productivityVector);
            var citationImpactScore = CalculateTrendScore(citationImpactVector);

            ViewBag.ProductivityTrendScore = Math.Round(productivityScore, 2);
            ViewBag.CitationImpactTrendScore = Math.Round(citationImpactScore, 2);

            ViewBag.ProductivityTrendLabel = productivityScore switch
            {
                >= 0.80 => "Strong upward productivity",
                >= 0.50 => "Moderate productivity trend",
                >= 0.30 => "Stable productivity",
                _ => "Weak or declining productivity"
            };

            ViewBag.CitationImpactTrendLabel = citationImpactScore switch
            {
                >= 0.80 => "Strong upward citation impact",
                >= 0.50 => "Moderate citation impact",
                >= 0.30 => "Stable citation impact",
                _ => "Weak or delayed citation impact"
            };

            var citationValues = loadedPapers
                .Select(p => p.CitationCount)
                .ToList();

            double averageCitations = citationValues.Any()
                ? citationValues.Average()
                : 0;

            double standardDeviation = citationValues.Any()
                ? Math.Sqrt(citationValues.Average(c => Math.Pow(c - averageCitations, 2)))
                : 0;

            ViewBag.ChartYears = yearlyStats.Select(x => x.Year).ToList();
            ViewBag.ChartPaperCounts = yearlyStats.Select(x => x.PaperCount).ToList();
            ViewBag.ChartCitationCounts = yearlyStats.Select(x => x.CitationCount).ToList();

            ViewBag.AverageCitations = Math.Round(averageCitations, 2);
            ViewBag.StandardDeviation = Math.Round(standardDeviation, 2);
            ViewBag.ActiveYears = yearlyStats.Count;
            ViewBag.MostProductiveYear = yearlyStats
                .OrderByDescending(x => x.PaperCount)
                .FirstOrDefault()?.Year;

            return View(result);
        }

        [HttpPost]
        public IActionResult CombinedReport(int searchQueryId, List<int> selectedAuthorIds)
        {
            if (selectedAuthorIds == null || !selectedAuthorIds.Any())
            {
                return RedirectToAction("Details", new { id = searchQueryId });
            }

            var ids = string.Join(",", selectedAuthorIds);

            return RedirectToAction("CombinedReportView", new { ids });
        }

        [HttpGet]
        public async Task<IActionResult> CombinedReportView(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
            {
                return RedirectToAction("Index");
            }

            var selectedAuthorIds = ids
                .Split(',')
                .Select(int.Parse)
                .ToList();

            var selectedAuthors = await _context.SearchResults
                .Where(r => selectedAuthorIds.Contains(r.SearchResultId))
                .Include(r => r.Papers)
                .ToListAsync();

            foreach (var author in selectedAuthors)
            {
                if (author.Papers == null || !author.Papers.Any())
                {
                    List<ResearchPaper> papers;

                    if (author.Source != null && author.Source.StartsWith("OpenAlex"))
                    {
                        papers = await _openAlexService
                            .GetAuthorWorksAsync(author.ExternalAuthorId, author.SearchResultId);
                    }
                    else
                    {
                        papers = await _semanticScholarService
                            .GetAuthorPapersAsync(author.ExternalAuthorId, author.SearchResultId);
                    }

                    _context.ResearchPapers.AddRange(papers);
                    await _context.SaveChangesAsync();

                    author.Papers = papers;
                }
            }

            return View("CombinedReport", selectedAuthors);
        }

        [HttpPost]
        public async Task<IActionResult> ExportCombinedReportToExcel(List<int> selectedAuthorIds)
        {
            if (selectedAuthorIds == null || !selectedAuthorIds.Any())
            {
                return RedirectToAction("Index");
            }

            var selectedAuthors = await _context.SearchResults
                .Where(r => selectedAuthorIds.Contains(r.SearchResultId))
                .Include(r => r.Papers)
                .ToListAsync();

            foreach (var author in selectedAuthors)
            {
                if (author.Papers == null || !author.Papers.Any())
                {
                    List<ResearchPaper> papers;

                    if (author.Source != null && author.Source.StartsWith("OpenAlex"))
                    {
                        papers = await _openAlexService.GetAuthorWorksAsync(author.ExternalAuthorId, author.SearchResultId);
                    }
                    else
                    {
                        papers = await _semanticScholarService.GetAuthorPapersAsync(author.ExternalAuthorId, author.SearchResultId);
                    }

                    _context.ResearchPapers.AddRange(papers);
                    await _context.SaveChangesAsync();

                    author.Papers = papers;
                }
            }

            string NormalizeSource(string source)
            {
                if (!string.IsNullOrWhiteSpace(source) && source.StartsWith("OpenAlex"))
                    return "OpenAlex";

                if (!string.IsNullOrWhiteSpace(source) && source.StartsWith("Semantic Scholar"))
                    return "Semantic Scholar";

                return "Unknown";
            }

            var publicationRows = selectedAuthors
                .SelectMany(author => (author.Papers ?? new List<ResearchPaper>())
                    .Select(paper => new
                    {
                        Source = NormalizeSource(author.Source),
                        Author = author.AuthorName,
                        Paper = paper
                    }))
                .Where(x => !string.IsNullOrWhiteSpace(x.Paper.Title))
                .ToList();

            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Summary by Source");

            summary.Cell("A1").Value = "Research Analytics Report";
            summary.Range("A1:E1").Merge();
            summary.Cell("A1").Style.Font.Bold = true;
            summary.Cell("A1").Style.Font.FontSize = 18;

            summary.Cell("A3").Value = "Generated At";
            summary.Cell("B3").Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            summary.Cell("A5").Value = "Source";
            summary.Cell("B5").Value = "Selected Profiles";
            summary.Cell("C5").Value = "Publications";
            summary.Cell("D5").Value = "Total Citations";
            summary.Cell("E5").Value = "Average Citations / Publication";
            summary.Range("A5:E5").Style.Font.Bold = true;

            var sources = selectedAuthors
                .Select(a => NormalizeSource(a.Source))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            int summaryRow = 6;

            foreach (var source in sources)
            {
                var sourceAuthors = selectedAuthors
                    .Where(a => NormalizeSource(a.Source) == source)
                    .ToList();

                var sourcePapers = publicationRows
                    .Where(p => p.Source == source)
                    .ToList();

                summary.Cell(summaryRow, 1).Value = source;
                summary.Cell(summaryRow, 2).Value = sourceAuthors.Count;
                summary.Cell(summaryRow, 3).Value = sourcePapers.Count;
                summary.Cell(summaryRow, 4).Value = sourcePapers.Sum(p => p.Paper.CitationCount);
                summary.Cell(summaryRow, 5).Value = sourcePapers.Any()
                    ? Math.Round(sourcePapers.Average(p => p.Paper.CitationCount), 2)
                    : 0;

                summaryRow++;
            }

            summary.Columns().AdjustToContents();

            var authorsWs = workbook.Worksheets.Add("Author Profiles");

            authorsWs.Cell("A1").Value = "Author Profiles";
            authorsWs.Range("A1:F1").Merge();
            authorsWs.Cell("A1").Style.Font.Bold = true;
            authorsWs.Cell("A1").Style.Font.FontSize = 16;

            authorsWs.Cell("A3").Value = "Source";
            authorsWs.Cell("B3").Value = "Author";
            authorsWs.Cell("C3").Value = "Papers";
            authorsWs.Cell("D3").Value = "Citations";
            authorsWs.Cell("E3").Value = "Average Citations / Paper";
            authorsWs.Cell("F3").Value = "External Author ID";
            authorsWs.Range("A3:F3").Style.Font.Bold = true;

            int authorRow = 4;

            foreach (var author in selectedAuthors
                .OrderBy(a => NormalizeSource(a.Source))
                .ThenByDescending(a => a.Citations))
            {
                authorsWs.Cell(authorRow, 1).Value = NormalizeSource(author.Source);
                authorsWs.Cell(authorRow, 2).Value = author.AuthorName;
                authorsWs.Cell(authorRow, 3).Value = author.PaperCount;
                authorsWs.Cell(authorRow, 4).Value = author.Citations;
                authorsWs.Cell(authorRow, 5).Value = author.PaperCount > 0
                    ? Math.Round((double)author.Citations / author.PaperCount, 2)
                    : 0;
                authorsWs.Cell(authorRow, 6).Value = author.ExternalAuthorId;

                authorRow++;
            }

            authorsWs.Columns().AdjustToContents();

            void AddSourcePublicationSheet(string sourceName, string sheetName)
            {
                var ws = workbook.Worksheets.Add(sheetName);

                ws.Cell("A1").Value = $"{sourceName} Publication List";
                ws.Range("A1:F1").Merge();
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A1").Style.Font.FontSize = 16;

                ws.Cell("A3").Value = "Author";
                ws.Cell("B3").Value = "Title";
                ws.Cell("C3").Value = "Year";
                ws.Cell("D3").Value = "Citations";
                ws.Cell("E3").Value = "External Paper ID";
                ws.Cell("F3").Value = "URL";

                ws.Range("A3:F3").Style.Font.Bold = true;

                int row = 4;

                foreach (var item in publicationRows
                    .Where(p => p.Source == sourceName)
                    .OrderByDescending(p => p.Paper.Year)
                    .ThenByDescending(p => p.Paper.CitationCount))
                {
                    ws.Cell(row, 1).Value = item.Author;
                    ws.Cell(row, 2).Value = item.Paper.Title;
                    ws.Cell(row, 3).Value = item.Paper.Year;
                    ws.Cell(row, 4).Value = item.Paper.CitationCount;
                    ws.Cell(row, 5).Value = item.Paper.ExternalPaperId;
                    ws.Cell(row, 6).Value = item.Paper.Url;

                    row++;
                }

                ws.Columns().AdjustToContents();
            }

            AddSourcePublicationSheet("Semantic Scholar", "Semantic Scholar");
            AddSourcePublicationSheet("OpenAlex", "OpenAlex");

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"ResearchAnalyticsReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var search = _context.SearchQueries
                .Include(s => s.Results)
                    .ThenInclude(r => r.Papers)
                .FirstOrDefault(s => s.SearchQueryId == id && s.UserId == userId);

            if (search == null)
            {
                return NotFound();
            }

            _context.SearchQueries.Remove(search);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EnrichWithScopus(int id, int limit = 25)
        {
            var result = await _context.SearchResults
                .Include(r => r.Papers)
                .FirstOrDefaultAsync(r => r.SearchResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (result.Papers == null || !result.Papers.Any())
            {
                List<ResearchPaper> papers;

                if (result.Source != null && result.Source.StartsWith("OpenAlex"))
                {
                    papers = await _openAlexService
                        .GetAuthorWorksAsync(result.ExternalAuthorId, result.SearchResultId);
                }
                else
                {
                    papers = await _semanticScholarService
                        .GetAuthorPapersAsync(result.ExternalAuthorId, result.SearchResultId);
                }

                _context.ResearchPapers.AddRange(papers);
                await _context.SaveChangesAsync();

                result.Papers = papers;
            }

            string originalSource =
                result.Source != null && result.Source.StartsWith("OpenAlex")
                    ? "OpenAlex"
                    : "Semantic Scholar";

            var allPapers = result.Papers
                .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                .OrderByDescending(p => p.CitationCount)
                .ToList();

            var papersToCheck = allPapers
                .Take(limit)
                .ToList();

            var enrichedPapers = new List<ScopusEnrichedPaper>();

            foreach (var paper in papersToCheck)
            {
                var enriched = await _scopusService.EnrichPaperByTitleAsync(paper, originalSource);
                enrichedPapers.Add(enriched);

                await Task.Delay(150);
            }

            ViewBag.SearchResultId = id;
            ViewBag.AuthorName = result.AuthorName;
            ViewBag.Source = originalSource;
            ViewBag.TotalPapers = allPapers.Count;
            ViewBag.CheckedPapers = papersToCheck.Count;
            ViewBag.CurrentLimit = limit;
            ViewBag.NextLimit = limit + 25;
            ViewBag.HasMore = limit < allPapers.Count;
            ViewBag.FoundInScopus = enrichedPapers.Count(p => p.FoundInScopus);

            return View("ScopusEnrichment", enrichedPapers);
        }

        public async Task<IActionResult> ExportScopusEnrichmentToExcel(int id, int limit = 25)
        {
            var result = await _context.SearchResults
                .Include(r => r.Papers)
                .FirstOrDefaultAsync(r => r.SearchResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            string originalSource =
                result.Source != null && result.Source.StartsWith("OpenAlex")
                    ? "OpenAlex"
                    : "Semantic Scholar";

            var papersToCheck = result.Papers
                .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                .OrderByDescending(p => p.CitationCount)
                .Take(limit)
                .ToList();

            var enrichedPapers = new List<ScopusEnrichedPaper>();

            foreach (var paper in papersToCheck)
            {
                var enriched = await _scopusService.EnrichPaperByTitleAsync(paper, originalSource);
                enrichedPapers.Add(enriched);

                await Task.Delay(150);
            }

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Scopus Enrichment");

            ws.Cell("A1").Value = "Scopus Enrichment Report";
            ws.Range("A1:I1").Merge();
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            ws.Cell("A4").Value = "Original Source";
            ws.Cell("B4").Value = originalSource;
            ws.Cell("A5").Value = "Checked Publications";
            ws.Cell("B5").Value = papersToCheck.Count;
            ws.Cell("A6").Value = "Found in Scopus";
            ws.Cell("B6").Value = enrichedPapers.Count(p => p.FoundInScopus);

            ws.Cell("A8").Value = "Original Title";
            ws.Cell("B8").Value = "Found in Scopus";
            ws.Cell("C8").Value = "Scopus Title";
            ws.Cell("D8").Value = "Journal";
            ws.Cell("E8").Value = "Year";
            ws.Cell("F8").Value = "Scopus Citations";
            ws.Cell("G8").Value = "DOI";
            ws.Cell("H8").Value = "Link";

            ws.Range("A8:H8").Style.Font.Bold = true;

            ws.Range("A8:I8").Style.Font.Bold = true;

            int row = 9;

            foreach (var item in enrichedPapers)
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

            var fileName = $"ScopusEnrichment_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpPost]
        public IActionResult CustomizeReport(List<int> selectedAuthorIds)
        {
            if (selectedAuthorIds == null || !selectedAuthorIds.Any())
            {
                return RedirectToAction("Index");
            }

            var model = new ReportCustomizationOptions
            {
                SelectedAuthorIds = selectedAuthorIds
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ExportCustomCombinedReportToExcel(ReportCustomizationOptions options)
        {
            if (options.SelectedAuthorIds == null ||
                !options.SelectedAuthorIds.Any())
            {
                return RedirectToAction("Index");
            }

            var selectedAuthors = await _context.SearchResults
                .Where(r => options.SelectedAuthorIds.Contains(r.SearchResultId))
                .Include(r => r.Papers)
                .ToListAsync();

            foreach (var author in selectedAuthors)
            {
                if (author.Papers == null || !author.Papers.Any())
                {
                    List<ResearchPaper> papers;

                    if (!string.IsNullOrWhiteSpace(author.Source) &&
                        author.Source.StartsWith("OpenAlex"))
                    {
                        papers = await _openAlexService
                            .GetAuthorWorksAsync(
                                author.ExternalAuthorId,
                                author.SearchResultId);
                    }
                    else
                    {
                        papers = await _semanticScholarService
                            .GetAuthorPapersAsync(
                                author.ExternalAuthorId,
                                author.SearchResultId);
                    }

                    _context.ResearchPapers.AddRange(papers);
                    await _context.SaveChangesAsync();

                    author.Papers = papers;
                }
            }

            string NormalizeSource(string source)
            {
                if (!string.IsNullOrWhiteSpace(source) &&
                    source.StartsWith("OpenAlex"))
                {
                    return "OpenAlex";
                }

                if (!string.IsNullOrWhiteSpace(source) &&
                    source.StartsWith("Semantic Scholar"))
                {
                    return "Semantic Scholar";
                }

                return "Unknown";
            }

            var workbook = new XLWorkbook();

            if (options.SeparateBySource)
            {
                var groupedSources = selectedAuthors
                    .GroupBy(a => NormalizeSource(a.Source));

                foreach (var sourceGroup in groupedSources)
                {
                    var worksheet = workbook.Worksheets
                        .Add(sourceGroup.Key);

                    int row = 1;
                    int col = 1;

                    if (options.IncludeAuthorName)
                    {
                        worksheet.Cell(row, col).Value = "Author";
                        col++;
                    }

                    if (options.IncludeSource)
                    {
                        worksheet.Cell(row, col).Value = "Source";
                        col++;
                    }

                    if (options.IncludePaperCount)
                    {
                        worksheet.Cell(row, col).Value = "Paper Count";
                        col++;
                    }

                    if (options.IncludeAuthorCitations)
                    {
                        worksheet.Cell(row, col).Value = "Author Citations";
                        col++;
                    }

                    if (options.IncludeTitle)
                    {
                        worksheet.Cell(row, col).Value = "Title";
                        col++;
                    }

                    if (options.IncludeYear)
                    {
                        worksheet.Cell(row, col).Value = "Year";
                        col++;
                    }

                    if (options.IncludeCitations)
                    {
                        worksheet.Cell(row, col).Value = "Paper Citations";
                        col++;
                    }

                    if (options.IncludeExternalPaperId)
                    {
                        worksheet.Cell(row, col).Value = "External Paper ID";
                        col++;
                    }

                    if (options.IncludeUrl)
                    {
                        worksheet.Cell(row, col).Value = "URL";
                        col++;
                    }

                    worksheet.Range(1, 1, 1, col - 1)
                        .Style.Font.Bold = true;

                    row = 2;

                    foreach (var author in sourceGroup)
                    {
                        var papers = author.Papers ??
                                     new List<ResearchPaper>();

                        foreach (var paper in papers)
                        {
                            col = 1;

                            if (options.IncludeAuthorName)
                            {
                                worksheet.Cell(row, col).Value =
                                    author.AuthorName;
                                col++;
                            }

                            if (options.IncludeSource)
                            {
                                worksheet.Cell(row, col).Value =
                                    NormalizeSource(author.Source);
                                col++;
                            }

                            if (options.IncludePaperCount)
                            {
                                worksheet.Cell(row, col).Value =
                                    author.PaperCount;
                                col++;
                            }

                            if (options.IncludeAuthorCitations)
                            {
                                worksheet.Cell(row, col).Value =
                                    author.Citations;
                                col++;
                            }

                            if (options.IncludeTitle)
                            {
                                worksheet.Cell(row, col).Value =
                                    paper.Title;
                                col++;
                            }

                            if (options.IncludeYear)
                            {
                                worksheet.Cell(row, col).Value =
                                    paper.Year;
                                col++;
                            }

                            if (options.IncludeCitations)
                            {
                                worksheet.Cell(row, col).Value =
                                    paper.CitationCount;
                                col++;
                            }

                            if (options.IncludeExternalPaperId)
                            {
                                worksheet.Cell(row, col).Value =
                                    paper.ExternalPaperId;
                                col++;
                            }

                            if (options.IncludeUrl)
                            {
                                worksheet.Cell(row, col).Value =
                                    paper.Url;
                                col++;
                            }

                            row++;
                        }
                    }

                    worksheet.Columns().AdjustToContents();
                }
            }
            else
            {
                var worksheet = workbook.Worksheets
                    .Add("Combined Report");

                int row = 1;
                int col = 1;

                if (options.IncludeAuthorName)
                {
                    worksheet.Cell(row, col).Value = "Author";
                    col++;
                }

                if (options.IncludeSource)
                {
                    worksheet.Cell(row, col).Value = "Source";
                    col++;
                }

                if (options.IncludePaperCount)
                {
                    worksheet.Cell(row, col).Value = "Paper Count";
                    col++;
                }

                if (options.IncludeAuthorCitations)
                {
                    worksheet.Cell(row, col).Value = "Author Citations";
                    col++;
                }

                if (options.IncludeTitle)
                {
                    worksheet.Cell(row, col).Value = "Title";
                    col++;
                }

                if (options.IncludeYear)
                {
                    worksheet.Cell(row, col).Value = "Year";
                    col++;
                }

                if (options.IncludeCitations)
                {
                    worksheet.Cell(row, col).Value = "Paper Citations";
                    col++;
                }

                if (options.IncludeExternalPaperId)
                {
                    worksheet.Cell(row, col).Value = "External Paper ID";
                    col++;
                }

                if (options.IncludeUrl)
                {
                    worksheet.Cell(row, col).Value = "URL";
                    col++;
                }

                worksheet.Range(1, 1, 1, col - 1)
                    .Style.Font.Bold = true;

                row = 2;

                foreach (var author in selectedAuthors)
                {
                    var papers = author.Papers ??
                                 new List<ResearchPaper>();

                    foreach (var paper in papers)
                    {
                        col = 1;

                        if (options.IncludeAuthorName)
                        {
                            worksheet.Cell(row, col).Value =
                                author.AuthorName;
                            col++;
                        }

                        if (options.IncludeSource)
                        {
                            worksheet.Cell(row, col).Value =
                                NormalizeSource(author.Source);
                            col++;
                        }

                        if (options.IncludePaperCount)
                        {
                            worksheet.Cell(row, col).Value =
                                author.PaperCount;
                            col++;
                        }

                        if (options.IncludeAuthorCitations)
                        {
                            worksheet.Cell(row, col).Value =
                                author.Citations;
                            col++;
                        }

                        if (options.IncludeTitle)
                        {
                            worksheet.Cell(row, col).Value =
                                paper.Title;
                            col++;
                        }

                        if (options.IncludeYear)
                        {
                            worksheet.Cell(row, col).Value =
                                paper.Year;
                            col++;
                        }

                        if (options.IncludeCitations)
                        {
                            worksheet.Cell(row, col).Value =
                                paper.CitationCount;
                            col++;
                        }

                        if (options.IncludeExternalPaperId)
                        {
                            worksheet.Cell(row, col).Value =
                                paper.ExternalPaperId;
                            col++;
                        }

                        if (options.IncludeUrl)
                        {
                            worksheet.Cell(row, col).Value =
                                paper.Url;
                            col++;
                        }

                        row++;
                    }
                }

                worksheet.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            var fileName =
                $"CustomResearchReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}