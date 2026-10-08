using ResearchAnalytics.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ResearchAnalytics.Services
{
    public class OrcidService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OrcidService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            var clientId = _configuration["Orcid:ClientId"];
            var clientSecret = _configuration["Orcid:ClientSecret"];

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["grant_type"] = "client_credentials",
                ["scope"] = "/read-public"
            });

            var response = await _httpClient.PostAsync("https://orcid.org/oauth/token", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("access_token", out var token)
                ? token.GetString()
                : null;
        }

        public async Task<OrcidProfile?> GetProfileByIdAsync(string orcidId)
        {
            var token = await GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
                return null;

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://pub.orcid.org/v3.0/{orcidId}/person");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string givenNames = "";
            string familyName = "";
            string creditName = "";
            string biography = "";

            if (root.TryGetProperty("name", out var name))
            {
                if (name.TryGetProperty("given-names", out var given) &&
                    given.TryGetProperty("value", out var givenValue))
                {
                    givenNames = givenValue.GetString() ?? "";
                }

                if (name.TryGetProperty("family-name", out var family) &&
                    family.TryGetProperty("value", out var familyValue))
                {
                    familyName = familyValue.GetString() ?? "";
                }

                if (name.TryGetProperty("credit-name", out var credit) &&
                    credit.ValueKind != JsonValueKind.Null &&
                    credit.TryGetProperty("value", out var creditValue))
                {
                    creditName = creditValue.GetString() ?? "";
                }
            }

            if (root.TryGetProperty("biography", out var bio) &&
                bio.ValueKind != JsonValueKind.Null &&
                bio.TryGetProperty("content", out var bioContent))
            {
                biography = bioContent.GetString() ?? "";
            }

            var fullName = $"{givenNames} {familyName}".Trim();

            return new OrcidProfile
            {
                OrcidId = orcidId,
                FullName = string.IsNullOrWhiteSpace(fullName) ? creditName : fullName,
                CreditName = creditName,
                Biography = biography,
                ProfileUrl = $"https://orcid.org/{orcidId}"
            };
        }

        public async Task<List<OrcidWork>> GetWorksAsync(string orcidId)
        {
            var token = await GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<OrcidWork>();
            }

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://pub.orcid.org/v3.0/{orcidId}/works");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            request.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new List<OrcidWork>();
            }

            var json = await response.Content.ReadAsStringAsync();

            var works = new List<OrcidWork>();

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("group", out var groups))
            {
                return works;
            }

            foreach (var group in groups.EnumerateArray())
            {
                if (!group.TryGetProperty("work-summary", out var summaries) ||
                    summaries.ValueKind != JsonValueKind.Array ||
                    summaries.GetArrayLength() == 0)
                {
                    continue;
                }

                var summary = summaries[0];

                string title = "";
                string year = "";
                string type = "";
                string doi = "";
                string url = "";

                if (summary.TryGetProperty("title", out var titleObj) &&
                    titleObj.TryGetProperty("title", out var titleValue) &&
                    titleValue.TryGetProperty("value", out var titleText))
                {
                    title = titleText.GetString() ?? "";
                }

                if (summary.TryGetProperty("publication-date", out var dateObj) &&
                    dateObj.ValueKind != JsonValueKind.Null &&
                    dateObj.TryGetProperty("year", out var yearObj) &&
                    yearObj.ValueKind != JsonValueKind.Null &&
                    yearObj.TryGetProperty("value", out var yearValue))
                {
                    year = yearValue.GetString() ?? "";
                }

                if (summary.TryGetProperty("type", out var typeObj))
                {
                    type = typeObj.GetString() ?? "";
                }

                if (summary.TryGetProperty("external-ids", out var externalIds) &&
                    externalIds.TryGetProperty("external-id", out var ids) &&
                    ids.ValueKind == JsonValueKind.Array)
                {
                    foreach (var id in ids.EnumerateArray())
                    {
                        var idType = id.TryGetProperty("external-id-type", out var idTypeObj)
                            ? idTypeObj.GetString()
                            : "";

                        if (idType == "doi")
                        {
                            if (id.TryGetProperty("external-id-value", out var idValue))
                            {
                                doi = idValue.GetString() ?? "";
                            }

                            if (id.TryGetProperty("external-id-url", out var idUrl) &&
                                idUrl.ValueKind != JsonValueKind.Null &&
                                idUrl.TryGetProperty("value", out var urlValue))
                            {
                                url = urlValue.GetString() ?? "";
                            }

                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    works.Add(new OrcidWork
                    {
                        Title = title,
                        PublicationYear = year,
                        WorkType = type,
                        DOI = doi,
                        Url = url
                    });
                }
            }

            return works;
        }
    }
}