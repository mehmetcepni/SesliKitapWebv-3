using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Net.Http;

namespace SesliKitapWeb.Services
{
    public class HuggingFaceService
    {
        private readonly string _modelPath;
        private readonly HttpClient _httpClient;

        public HuggingFaceService()
        {
            _httpClient = new HttpClient();
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "bert-base-turkish-cased");
            
            // Model klasörünü oluştur
            if (!Directory.Exists(_modelPath))
            {
                Directory.CreateDirectory(_modelPath);
            }

            // Model dosyasını indir (eğer yoksa)
            var modelFile = Path.Combine(_modelPath, "model.onnx");
            if (!File.Exists(modelFile))
            {
                DownloadModelAsync().Wait();
            }
        }

        private async Task DownloadModelAsync()
        {
            try
            {
                // Model dosyasını indir
                var modelUrl = "https://huggingface.co/dbmdz/bert-base-turkish-cased/resolve/main/model.onnx";
                var modelFile = Path.Combine(_modelPath, "model.onnx");
                
                using (var response = await _httpClient.GetAsync(modelUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(modelFile))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                // Vocab dosyasını indir
                var vocabUrl = "https://huggingface.co/dbmdz/bert-base-turkish-cased/resolve/main/vocab.txt";
                var vocabFile = Path.Combine(_modelPath, "vocab.txt");
                
                using (var response = await _httpClient.GetAsync(vocabUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(vocabFile))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Model indirme hatası: " + ex.Message);
            }
        }
    }

    public class BertTokenizer
    {
        private readonly Dictionary<string, int> _vocab;

        public BertTokenizer(string vocabPath)
        {
            _vocab = File.ReadAllLines(vocabPath)
                .Select((line, index) => new { line, index })
                .ToDictionary(x => x.line, x => x.index);
        }

        public List<int> Tokenize(string text)
        {
            var tokens = new List<int>();
            var words = text.Split(' ');

            foreach (var word in words)
            {
                if (_vocab.TryGetValue(word, out int tokenId))
                {
                    tokens.Add(tokenId);
                }
                else
                {
                    // Bilinmeyen kelimeler için [UNK] token'ı
                    tokens.Add(_vocab["[UNK]"]);
                }
            }

            return tokens;
        }
    }
}