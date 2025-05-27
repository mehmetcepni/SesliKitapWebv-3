using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SesliKitapWeb.Services
{
    public class TextClassificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://api-inference.huggingface.co/models/facebook/bart-large-mnli";
        private readonly string _apiToken; // HuggingFace API anahtarı

        public TextClassificationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiToken = configuration["HuggingFace:ApiToken"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        }

        public async Task<string> PredictCategoryAsync(string text, List<string> candidateLabels)
        {
            var requestBody = new
            {
                inputs = text,
                parameters = new { candidate_labels = candidateLabels }
            };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var label = doc.RootElement.GetProperty("labels")[0].GetString();
            return label;
        }
    }
}
