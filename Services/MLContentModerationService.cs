using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using SesliKitapWeb.Models.ML;

namespace SesliKitapWeb.Services
{
    public interface IContentModerationService
    {
        Task<bool> IsInappropriateContentAsync(string text);
        ValueTask<bool> QuickCheckAsync(string text);
    }

    public class MLContentModerationService : IContentModerationService
    {
        private readonly ILogger<MLContentModerationService> _logger;
        private readonly MLContext _mlContext;
        private readonly PredictionEngine<SpamInput, SpamPrediction> _predictionEngine;
        private readonly HashSet<string> _inappropriateWords;
        
        // ML.NET model dosya yolu
        private const string MODEL_PATH = "mlnet_spam_model.zip";
        
        public MLContentModerationService(ILogger<MLContentModerationService> logger)
        {
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
            
            // Türkçe ve İngilizce uygunsuz kelimeleri yükle - Hızlı kontrol için
            _inappropriateWords = LoadInappropriateWords();
            
            try
            {
                // Model dosyası varsa yükle
                if (File.Exists(MODEL_PATH))
                {
                    _logger.LogInformation("ML.NET modeli yükleniyor: {modelPath}", MODEL_PATH);
                    var loadedModel = _mlContext.Model.Load(MODEL_PATH, out var _);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<SpamInput, SpamPrediction>(loadedModel);
                    _logger.LogInformation("ML.NET modeli başarıyla yüklendi");
                }
                else
                {
                    _logger.LogWarning("ML.NET modeli bulunamadı. Model eğitilecek");
                    var model = TrainModel();
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<SpamInput, SpamPrediction>(model);
                    
                    // Modeli kaydet
                    _mlContext.Model.Save(model, null, MODEL_PATH);
                    _logger.LogInformation("Yeni ML.NET modeli oluşturuldu ve kaydedildi: {modelPath}", MODEL_PATH);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ML.NET modeli yüklenirken veya oluşturulurken hata oluştu");
                
                // Hata durumunda basit bir model oluştur
                var simpleModel = CreateSimpleModel();
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<SpamInput, SpamPrediction>(simpleModel);
                _logger.LogInformation("Basit bir ML.NET modeli oluşturuldu");
            }
        }
        
        // Ana method: Metni uygunsuz içerik açısından kontrol eder
        public async Task<bool> IsInappropriateContentAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            _logger.LogInformation("IsInappropriateContentAsync çağrıldı: {textSample}", 
                text.Length > 30 ? text.Substring(0, 30) + "..." : text);
            
            try
            {
                // Önce basit kelime kontrolü yap - hızlı sonuç için
                if (await QuickCheckAsync(text))
                {
                    _logger.LogWarning("Hızlı kontrol ile uygunsuz içerik tespit edildi");
                    return true;
                }
                
                // ML.NET model tahminini çalıştır
                var input = new SpamInput(text);
                var prediction = _predictionEngine.Predict(input);
                
                _logger.LogInformation("ML.NET Tahmin Sonucu - IsSpam: {isSpam}, Probability: {probability}", 
                    prediction.IsSpam, prediction.Probability);
                
                // Belirli bir eşik değerine göre karar ver (örn. 0.7)
                if (prediction.IsSpam || prediction.Probability > 0.7f)
                {
                    _logger.LogWarning("ML.NET ile uygunsuz içerik tespit edildi! Probability: {probability}", 
                        prediction.Probability);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İçerik kontrolü sırasında hata oluştu");
                // Hata durumunda basit kelime kontrolü yap
                return IsInappropriateWithWordList(text);
            }
        }
        
        // Hızlı basit kelime kontrolü - async ValueTask kullanarak optimizasyon
        public ValueTask<bool> QuickCheckAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new ValueTask<bool>(false);
                
            return new ValueTask<bool>(IsInappropriateWithWordList(text));
        }
        
        // Basit kelime listesi kontrolü
        private bool IsInappropriateWithWordList(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            var normalizedText = text.ToLower()
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "")
                .Replace("?", "");
                
            var words = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                if (_inappropriateWords.Contains(word))
                {
                    _logger.LogWarning("Uygunsuz kelime tespit edildi: {word}", word);
                    return true;
                }
            }
            
            return false;
        }
        
        // Uygunsuz kelimeler listesini yükle
        private HashSet<string> LoadInappropriateWords()
        {
            var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Türkçe uygunsuz kelimeler
                "küfür", "hakaret", "argo", "küfretmek", "amk", "sg", "siktir", 
                "yavşak", "gerizekalı", "aptal", "salak", "mal", "beyinsiz", "şerefsiz",
                "bok", "sik", "ahlaksız", "puşt", "gavat", "pezevenk", "orospu",
                
                // İngilizce uygunsuz kelimeler
                "shit", "asshole", "bitch", "damn", "cunt", "dick", "bastard",
                "whore", "slut", "retard", "idiot", "stupid", "moron", "jerk",
                "pussy", "piss", "cock", "wtf", "stfu", "bullshit", "prick", "hoe"
            };
            
            return words;
        }
        
        // Model eğitimi - örnek veri ile basit bir model
        private ITransformer TrainModel()
        {
            // Örnek eğitim verisi oluştur
            var trainingData = CreateSampleTrainingData();
            // ML.NET veri yükleme ve işleme pipeline'ı
            var pipeline = _mlContext.Transforms.Text.FeaturizeText(
                    "Features", nameof(SpamInput.Text))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());
            // Model eğitimi
            _logger.LogInformation("ML.NET modeli eğitiliyor...");
            var model = pipeline.Fit(trainingData);
            _logger.LogInformation("ML.NET modeli eğitildi");
            return model;
        }
        // Basit bir model oluştur - feature engineering olmadan
        private ITransformer CreateSimpleModel()
        {
            var trainingData = CreateSampleTrainingData();
            var simplePipeline = _mlContext.Transforms.Text.FeaturizeText(
                    "Features", nameof(SpamInput.Text))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());
            return simplePipeline.Fit(trainingData);
        }
        // Örnek eğitim verisi oluştur
        private IDataView CreateSampleTrainingData()
        {
            var trainingData = new List<(string text, bool isSpam)>
            {
                // Spam/uygunsuz örnekler
                ("Bu bir küfürdür", true),
                ("aptal ve salak insanlar", true),
                ("siktir git buradan", true),
                ("ahlaksız bir insan", true),
                ("hepiniz gerizekalısınız", true),
                ("tam bir şerefsiz", true),
                ("You are a stupid idiot", true),
                ("What the fuck is this shit", true),
                ("you motherfucker", true),
                ("This is bullshit content", true),
                ("go to hell you bastard", true),
                ("shut the fuck up", true),
                
                // Normal örnekler
                ("bugün hava çok güzel", false),
                ("kitap okumayı seviyorum", false),
                ("bu film çok güzeldi", false),
                ("yeni albüm çıkarmış", false),
                ("yarın okula gideceğim", false),
                ("akşam yemeği için ne pişirsem", false),
                ("I really enjoyed this book", false),
                ("The weather is nice today", false),
                ("thank you for your help", false),
                ("I will go to school tomorrow", false),
                ("please help me with this problem", false),
                ("this movie was amazing", false)
            };
            
            // Veriyi ML.NET formatına dönüştür
            return _mlContext.Data.LoadFromEnumerable(
                trainingData.Select(item => new SpamInput { Text = item.text })
                .Zip(trainingData.Select(item => item.isSpam), 
                    (textItem, isSpam) => (textItem, isSpam))
                .Select(item => 
                {
                    item.textItem.Text = item.textItem.Text;
                    return new
                    {
                        Text = item.textItem.Text,
                        Label = item.isSpam
                    };
                })
            );
        }
    }
}
