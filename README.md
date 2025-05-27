# SesliKitapWeb

SesliKitapWeb, sesli kitapların yönetilebileceği, kullanıcıların kitapları okuyabileceği, yorum yapabileceği ve kişiselleştirilmiş öneriler alabileceği bir web uygulamasıdır.

## Özellikler

- **Kitap Yönetimi**: Kitap ekleme, düzenleme, silme ve kategorilendirme
- **Kullanıcı Sistemi**: Kayıt olma, giriş yapma ve profil düzenleme
- **Yorum Sistemi**: Kitaplara yorum yapma ve puan verme
- **Sesli İçerik**: Kitapların sesli versiyonlarını kullanıcılara sunma
- **Favoriler**: Kullanıcıların sevdikleri kitapları favorilere ekleyebilmesi
- **Okuma Geçmişi**: Kullanıcıların okudukları kitapların takibi 
- **Kitap Önerileri**: Kullanıcının okuma geçmişine göre kişiselleştirilmiş öneriler
- **NLP Özellikleri**: 
  - Duygu Analizi: Yorumların duygusal tonunun analizi
  - İçerik Sınıflandırma: Kitapların içeriğe göre otomatik kategorilendirme
  - Uygunsuz İçerik Kontrolü: Yorumlarda uygunsuz içeriklerin tespiti

## Teknolojiler

- **ASP.NET Core**: Web uygulaması çatısı
- **Entity Framework Core**: Veritabanı erişim ve yönetimi
- **C#**: Backend programlama dili
- **Bootstrap**: Ön yüz tasarımı
- **Identity**: Kullanıcı yönetimi ve yetkilendirme
- **SQLite/SQL Server**: Veritabanı
- **Hugging Face API**: NLP servisleri için dış API entegrasyonu
- **ML.NET**: Makine öğrenmesi modelleri

## Kurulum

1. Repository'yi klonlayın:
   ```
   git clone https://github.com/username/SesliKitapWebv-3
   ```

2. Proje klasörüne gidin:
   ```
   cd SesliKitapWebv-3-master
   ```

3. Bağımlılıkları yükleyin:
   ```
   dotnet restore
   ```

4. Veritabanını oluşturun:
   ```
   dotnet ef database update
   ```

5. Uygulamayı çalıştırın:
   ```
   dotnet run
   ```

6. Tarayıcınızda aşağıdaki adrese gidin:
   ```
   http://localhost:5206
   ```

## Bağımlılıklar

- .NET 9.0
- Entity Framework Core
- ASP.NET Core Identity
- Bootstrap 5
- JQuery

## Veritabanı Yapısı

- **Books**: Kitap bilgilerini içerir
- **Categories**: Kitap kategorilerini içerir
- **Reviews**: Kullanıcı yorumlarını içerir
- **UserBooks**: Kullanıcı-kitap ilişkilerini içerir
- **UserReadingHistory**: Kullanıcıların okuma geçmişini içerir
- **BookRecommendation**: Kullanıcılara önerilen kitapları içerir
- **ApplicationUser**: Kullanıcı bilgilerini içerir

## API Kullanımı

Duygu analizi, içerik sınıflandırma ve uygunsuz içerik kontrolü için Hugging Face API kullanılmaktadır. API anahtarınızı appsettings.json dosyasında aşağıdaki şekilde yapılandırın:

```json
"HuggingFace": {
  "ApiToken": "YOUR_API_KEY"
}
```

## Kullanıcı Rolleri

- **Admin**: Kitap ve kategori yönetimi yapabilir
- **Kullanıcı**: Kitapları okuyabilir, yorum yapabilir ve favorilere ekleyebilir

## Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some amazing feature'`)
4. Branch'inize push edin (`git push origin feature/amazing-feature`)
5. Pull request oluşturun


## İletişim

Proje Sahibi - [mehmetcepni343@gmail.com](mailto:mehmetcepni3434@gmail.com)

Proje Linki: [https://github.com/mehmetcepni/SesliKitapWebv-3](https://github.com/mehmetcepni/SesliKitapWebv-3)
