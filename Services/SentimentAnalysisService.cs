using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SesliKitapWeb.Services
{
    public class SentimentAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://api-inference.huggingface.co/models/nlptown/bert-base-multilingual-uncased-sentiment";
        private readonly string _apiToken;

        public SentimentAnalysisService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiToken = configuration["HuggingFace:ApiToken"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        }

        public async Task<string> AnalyzeSentimentAsync(string text)
        {
            var requestBody = new { inputs = text };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            // En yüksek skorlu etiketi bul
            var predictions = doc.RootElement[0].EnumerateArray();
            string? bestLabel = null;
            float bestScore = float.MinValue;
            foreach (var prediction in predictions)
            {
                var label = prediction.GetProperty("label").GetString();
                var score = prediction.GetProperty("score").GetSingle();
                if (score > bestScore)
                {
                    bestScore = score;
                    bestLabel = label ?? "Bilinmiyor";
                }
            }
            return bestLabel ?? "Bilinmiyor";
        }
    }
}
