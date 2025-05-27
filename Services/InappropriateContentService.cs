using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SesliKitapWeb.Services
{    
    public class InappropriateContentService
    {
        private readonly ILogger<InappropriateContentService> _logger;
        private readonly IContentModerationService _contentModerationService;

        public InappropriateContentService(ILogger<InappropriateContentService> logger, IContentModerationService contentModerationService)
        {
            _logger = logger;
            _contentModerationService = contentModerationService;
            
            _logger.LogInformation("InappropriateContentService başlatıldı. ML.NET tabanlı spam kontrolü kullanılıyor.");
        }public async Task<bool> IsInappropriateAsync(string text)
        {
            _logger.LogInformation("IsInappropriateAsync çağrıldı. Text: {textSample}", 
                text.Length > 30 ? text.Substring(0, 30) + "..." : text);

            try
            {
                // Sadece ML.NET tabanlı servis ile kontrol
                bool isInappropriate = await _contentModerationService.IsInappropriateContentAsync(text);
                if (isInappropriate)
                {
                    _logger.LogWarning("ML.NET ile uygunsuz içerik tespit edildi!");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ML.NET işleminde hata oluştu");
                // Hata durumunda false döndür (veya istenirse logla)
                return false;
            }
        }
    }
}
