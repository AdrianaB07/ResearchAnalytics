using ResearchAnalytics.Models;
using System.Text.Json;

namespace ResearchAnalytics.Services
{
    public class ScopusService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ScopusService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<SearchResult>> SearchAuthorsAsync(string query, int searchQueryId)
        {
            var apiKey = _configuration["ScopusApi:ApiKey"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-ELS-APIKey", apiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var url =
                $"https://api.elsevier.com/content/search/author?query={Uri.EscapeDataString(query)}&apiKey={apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<SearchResult>();
            }

            var json = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine("===== SCOPUS API RESULT =====");
            System.Diagnostics.Debug.WriteLine(json);
            System.Diagnostics.Debug.WriteLine("===== END SCOPUS API RESULT =====");

            var results = new List<SearchResult>();

            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("search-results", out var searchResults))
            {
                return results;
            }

            if (!searchResults.TryGetProperty("entry", out var entries))
            {
                return results;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                string authorName = "Unknown";

                if (entry.TryGetProperty("preferred-name", out var preferredName))
                {
                    var givenName = preferredName.TryGetProperty("given-name", out var given)
                        ? given.GetString()
                        : "";

                    var surname = preferredName.TryGetProperty("surname", out var sur)
                        ? sur.GetString()
                        : "";

                    authorName = $"{givenName} {surname}".Trim();

                    if (string.IsNullOrWhiteSpace(authorName))
                    {
                        authorName = "Unknown";
                    }
                }

                string authorId = "";

                if (entry.TryGetProperty("dc:identifier", out var idProp))
                {
                    authorId = idProp.GetString() ?? "";
                }

                int paperCount = 0;

                if (entry.TryGetProperty("document-count", out var docCount))
                {
                    int.TryParse(docCount.GetString(), out paperCount);
                }

                results.Add(new SearchResult
                {
                    AuthorName = authorName,
                    ExternalAuthorId = authorId,
                    PaperCount = paperCount,
                    Citations = 0,
                    Source = "Scopus",
                    SearchQueryId = searchQueryId
                });
            }

            return results;
        }

        public async Task<List<ScopusPublication>> SearchPublicationsAsync(string query, string searchType)
        {
            var apiKey = _configuration["ScopusApi:ApiKey"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-ELS-APIKey", apiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            string scopusQuery;

            scopusQuery = $"TITLE(\"{query}\")";

            var url =
                $"https://api.elsevier.com/content/search/scopus?query={Uri.EscapeDataString(scopusQuery)}&apiKey={apiKey}";

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine("===== SCOPUS PUBLICATION SEARCH RESULT =====");
            System.Diagnostics.Debug.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine(json);
            System.Diagnostics.Debug.WriteLine("===== END SCOPUS PUBLICATION SEARCH RESULT =====");

            if (!response.IsSuccessStatusCode)
            {
                return new List<ScopusPublication>();
            }

            var results = new List<ScopusPublication>();

            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("search-results", out var searchResults))
            {
                return results;
            }

            if (!searchResults.TryGetProperty("entry", out var entries))
            {
                return results;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                string GetString(string propertyName)
                {
                    return entry.TryGetProperty(propertyName, out var prop)
                        ? prop.GetString() ?? ""
                        : "";
                }

                var link = "";

                if (entry.TryGetProperty("link", out var links))
                {
                    foreach (var l in links.EnumerateArray())
                    {
                        if (l.TryGetProperty("@ref", out var refProp) &&
                            refProp.GetString() == "scopus" &&
                            l.TryGetProperty("@href", out var hrefProp))
                        {
                            link = hrefProp.GetString() ?? "";
                            break;
                        }
                    }
                }

                results.Add(new ScopusPublication
                {
                    Title = GetString("dc:title"),
                    Authors = GetString("dc:creator"),
                    PublicationName = GetString("prism:publicationName"),
                    Year = GetString("prism:coverDate"),
                    CitedByCount = GetString("citedby-count"),
                    DOI = GetString("prism:doi"),
                    Link = link
                });
            }

            return results;
        }

        public async Task<ScopusEnrichedPaper> EnrichPaperByTitleAsync(
    ResearchPaper paper,
    string originalSource)
        {
            var apiKey = _configuration["ScopusApi:ApiKey"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-ELS-APIKey", apiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var result = new ScopusEnrichedPaper
            {
                OriginalTitle = paper.Title,
                OriginalSource = originalSource,
                FoundInScopus = false
            };

            if (string.IsNullOrWhiteSpace(paper.Title))
            {
                return result;
            }

            var safeTitle = paper.Title.Replace("\"", "");

            var scopusQuery = $"TITLE(\"{safeTitle}\")";

            var url =
                $"https://api.elsevier.com/content/search/scopus?query={Uri.EscapeDataString(scopusQuery)}&apiKey={apiKey}&count=1";

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("search-results", out var searchResults))
            {
                return result;
            }

            if (!searchResults.TryGetProperty("entry", out var entries) ||
                entries.ValueKind != JsonValueKind.Array ||
                entries.GetArrayLength() == 0)
            {
                return result;
            }

            var entry = entries[0];

            string GetString(string propertyName)
            {
                return entry.TryGetProperty(propertyName, out var prop)
                    ? prop.GetString() ?? ""
                    : "";
            }

            var scopusTitle = GetString("dc:title");

            if (string.IsNullOrWhiteSpace(scopusTitle))
            {
                return result;
            }

            var link = "";

            if (entry.TryGetProperty("link", out var links))
            {
                foreach (var l in links.EnumerateArray())
                {
                    if (l.TryGetProperty("@ref", out var refProp) &&
                        refProp.GetString() == "scopus" &&
                        l.TryGetProperty("@href", out var hrefProp))
                    {
                        link = hrefProp.GetString() ?? "";
                        break;
                    }
                }
            }

            result.FoundInScopus = true;
            result.ScopusTitle = scopusTitle;
            result.Authors = GetString("dc:creator");
            result.PublicationName = GetString("prism:publicationName");
            result.Year = GetString("prism:coverDate");
            result.CitedByCount = GetString("citedby-count");
            result.DOI = GetString("prism:doi");
            result.Link = link;

            return result;
        }

        public async Task<ScopusEnrichedPaper> EnrichTitleWithScopusAsync(string title, string originalSource)
        {
            var paper = new ResearchPaper
            {
                Title = title
            };

            return await EnrichPaperByTitleAsync(paper, originalSource);
        }
    }
}