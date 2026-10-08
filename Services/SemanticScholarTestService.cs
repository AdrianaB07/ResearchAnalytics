using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ResearchAnalytics.Models;
using ResearchAnalytics.Models.DTOs;

namespace ResearchAnalytics.Services
{
    public class SemanticScholarTestService
    {
        private readonly HttpClient _httpClient;

        public SemanticScholarTestService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();

            var apiKey = configuration["SemanticScholar:ApiKey"];

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
        }

        public async Task<List<SearchResult>> GetAuthorsAsync(string query, int searchQueryId)
        {
            var url =
                $"https://api.semanticscholar.org/graph/v1/author/search?query={Uri.EscapeDataString(query)}&limit=5&fields=name,paperCount,citationCount";

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return new List<SearchResult>();
            }

            if (!response.IsSuccessStatusCode)
            {
                return new List<SearchResult>();
            }

            var apiResponse =
                await response.Content.ReadFromJsonAsync<SemanticScholarAuthorResponse>();

            if (apiResponse?.Data == null)
            {
                return new List<SearchResult>();
            }

            return apiResponse.Data.Select(author => new SearchResult
            {
                SearchQueryId = searchQueryId,
                AuthorName = author.Name,
                ExternalAuthorId = author.AuthorId,
                PaperCount = author.PaperCount,
                Citations = author.CitationCount,
                Source = "Semantic Scholar"
            }).ToList();
        }

        public async Task<List<ResearchPaper>> GetAuthorPapersAsync(
    string authorId,
    int searchResultId)
        {
            var url =
                $"https://api.semanticscholar.org/graph/v1/author/{authorId}/papers?limit=100&fields=title,year,citationCount,url";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ResearchPaper>();
            }

            var apiResponse =
                await response.Content.ReadFromJsonAsync<SemanticScholarPaperResponse>();

            if (apiResponse?.Data == null)
            {
                return new List<ResearchPaper>();
            }

            return apiResponse.Data.Select(paper => new ResearchPaper
            {
                SearchResultId = searchResultId,
                ExternalPaperId = paper.PaperId,
                Title = paper.Title,
                Year = paper.Year,
                CitationCount = paper.CitationCount,
                Url = paper.Url
            }).ToList();
        }
    }
}