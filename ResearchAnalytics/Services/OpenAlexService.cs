using ResearchAnalytics.Models;
using System.Text.Json;

namespace ResearchAnalytics.Services
{
    public class OpenAlexService
    {
        private readonly HttpClient _httpClient;

        public OpenAlexService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<SearchResult>> SearchAuthorsAsync(
            string query,
            int searchQueryId)
        {
            var url =
                $"https://api.openalex.org/authors?search={Uri.EscapeDataString(query)}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<SearchResult>();
            }

            var json = await response.Content.ReadAsStringAsync();

            var results = new List<SearchResult>();

            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var authors))
            {
                return results;
            }

            foreach (var author in authors.EnumerateArray())
            {
                string authorId = "";
                string authorName = "Unknown";
                int worksCount = 0;
                int citations = 0;
                string institution = "";

                if (author.TryGetProperty("id", out var idProp))
                {
                    authorId = idProp.GetString() ?? "";
                }

                if (author.TryGetProperty("display_name", out var nameProp))
                {
                    authorName = nameProp.GetString() ?? "Unknown";
                }

                if (author.TryGetProperty("works_count", out var worksProp))
                {
                    worksCount = worksProp.GetInt32();
                }

                if (author.TryGetProperty("cited_by_count", out var citeProp))
                {
                    citations = citeProp.GetInt32();
                }

                if (author.TryGetProperty("last_known_institutions", out var insts)
                    && insts.ValueKind == JsonValueKind.Array
                    && insts.GetArrayLength() > 0)
                {
                    var firstInst = insts[0];

                    if (firstInst.TryGetProperty("display_name", out var instName))
                    {
                        institution = instName.GetString() ?? "";
                    }
                }

                results.Add(new SearchResult
                {
                    AuthorName = authorName,
                    ExternalAuthorId = authorId,
                    PaperCount = worksCount,
                    Citations = citations,
                    Source = $"OpenAlex ({institution})",
                    SearchQueryId = searchQueryId
                });
            }

            return results;
        }

        public async Task<List<ResearchPaper>> GetAuthorWorksAsync(string authorId, int searchResultId)
        {
            var url =
                $"https://api.openalex.org/works?filter=author.id:{Uri.EscapeDataString(authorId)}&per-page=100";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ResearchPaper>();
            }

            var json = await response.Content.ReadAsStringAsync();

            var papers = new List<ResearchPaper>();

            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var works))
            {
                return papers;
            }

            foreach (var work in works.EnumerateArray())
            {
                string paperId = "";
                string title = "Untitled";
                int? year = null;
                int citationCount = 0;
                string urlLink = "";

                if (work.TryGetProperty("id", out var idProp))
                {
                    paperId = idProp.GetString() ?? "";
                }

                if (work.TryGetProperty("title", out var titleProp))
                {
                    title = titleProp.GetString() ?? "Untitled";
                }

                if (work.TryGetProperty("publication_year", out var yearProp)
                    && yearProp.ValueKind == JsonValueKind.Number)
                {
                    year = yearProp.GetInt32();
                }

                if (work.TryGetProperty("cited_by_count", out var citedProp)
                    && citedProp.ValueKind == JsonValueKind.Number)
                {
                    citationCount = citedProp.GetInt32();
                }

                if (work.TryGetProperty("doi", out var doiProp)
                    && doiProp.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(doiProp.GetString()))
                {
                    urlLink = doiProp.GetString();
                }
                else if (work.TryGetProperty("id", out var openAlexIdProp))
                {
                    urlLink = openAlexIdProp.GetString() ?? "";
                }

                papers.Add(new ResearchPaper
                {
                    SearchResultId = searchResultId,
                    ExternalPaperId = paperId,
                    Title = title,
                    Year = year,
                    CitationCount = citationCount,
                    Url = urlLink
                });
            }

            return papers;
        }
    }
}