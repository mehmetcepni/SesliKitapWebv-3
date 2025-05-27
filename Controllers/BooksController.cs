using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SesliKitapWeb.Data;
using SesliKitapWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.IO;
using SesliKitapWeb.Services;
using Microsoft.Extensions.Logging;

namespace SesliKitapWeb.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TextClassificationService _textClassificationService;
        private readonly SentimentAnalysisService _sentimentAnalysisService;
        private readonly InappropriateContentService _inappropriateContentService;
        private readonly ILogger<BooksController> _logger;

        public BooksController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            TextClassificationService textClassificationService, 
            SentimentAnalysisService sentimentAnalysisService, 
            InappropriateContentService inappropriateContentService,
            ILogger<BooksController> logger)
        {
            _context = context;
            _userManager = userManager;
            _textClassificationService = textClassificationService;
            _sentimentAnalysisService = sentimentAnalysisService;
            _inappropriateContentService = inappropriateContentService;
            _logger = logger;
        }

        // GET: Books
        
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books.Include(b => b.Category).ToListAsync();
            return View(books);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Title,Author,Description,CategoryId,Content")] Book book, IFormFile coverImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Otomatik kategorilendirme
                    // Otomatik kategorilendirme için hem Türkçe hem İngilizce etiketler oluştur
                    var categories = await _context.Categories
                        .Select(c => c.Name + (c.Name == "Tarih" ? " (History)" :
                                               c.Name == "Roman" ? " (Novel)" :
                                               c.Name == "Çocuk Hikayeleri" ? " (Children's Stories)" :
                                               c.Name == "Biyografi" ? " (Biography)" :
                                               c.Name == "Fıkra" ? " (Anecdote)" : ""))
                        .ToListAsync();
                    var predictedCategory = await _textClassificationService.PredictCategoryAsync(book.Description, categories);
                    // Sadece Türkçe kısmı ile eşleştir
                    var category = await _context.Categories.FirstOrDefaultAsync(c => predictedCategory.Contains(c.Name));
                    if (category != null)
                        book.CategoryId = category.Id;

                    // Create uploads directory if it doesn't exist
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Handle cover image upload
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        var coverImageFileName = Guid.NewGuid().ToString() + Path.GetExtension(coverImage.FileName);
                        var coverImagePath = Path.Combine(uploadsFolder, coverImageFileName);
                        using (var stream = new FileStream(coverImagePath, FileMode.Create))
                        {
                            await coverImage.CopyToAsync(stream);
                        }
                        book.CoverImageUrl = "/uploads/" + coverImageFileName;
                    }

                    book.CreatedAt = DateTime.Now;
                    _context.Add(book);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Kitap eklenirken bir hata oluştu: " + ex.Message);
                }
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Description,CoverImageUrl,Content,CategoryId")] Book book, IFormFile coverImage)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
                    if (existingBook == null)
                    {
                        return NotFound();
                    }

                    // Tüm alanları güncelle
                    existingBook.Title = book.Title;
                    existingBook.Author = book.Author;
                    existingBook.Description = book.Description;
                    existingBook.Content = book.Content;
                    existingBook.CategoryId = book.CategoryId;
                    existingBook.CoverImageUrl = book.CoverImageUrl;
                    // Kapak resmi güncellemesi
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        var coverImageFileName = Guid.NewGuid().ToString() + Path.GetExtension(coverImage.FileName);
                        var coverImagePath = Path.Combine(uploadsFolder, coverImageFileName);
                        using (var stream = new FileStream(coverImagePath, FileMode.Create))
                        {
                            await coverImage.CopyToAsync(stream);
                        }
                        existingBook.CoverImageUrl = "/uploads/" + coverImageFileName;
                    }
                    _context.Entry(existingBook).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }

        // POST: Books/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddReview(int bookId, string content)
        {
            _logger.LogInformation("AddReview çağrıldı. BookId: {BookId}, Content: {ContentSnippet}", 
                bookId, content?.Length > 30 ? content.Substring(0, 30) + "..." : content);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Kullanıcı bulunamadı");
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Boş yorum gönderildi");
                TempData["SpamWarning"] = "Yorum içeriği boş olamaz.";
                return RedirectToAction(nameof(Details), new { id = bookId });
            }

            try
            {
                // NLP tabanlı uygunsuz içerik kontrolü
                _logger.LogInformation("Uygunsuz içerik kontrolü başlatılıyor");
                bool isInappropriate = await _inappropriateContentService.IsInappropriateAsync(content);
                _logger.LogInformation("Uygunsuz içerik kontrolü sonucu: {IsInappropriate}", isInappropriate);

                if (isInappropriate)
                {
                    _logger.LogWarning("Uygunsuz içerik tespit edildi");
                    TempData["SpamWarning"] = "Yorumunuzda uygunsuz içerik bulunuyor.";
                    return RedirectToAction(nameof(Details), new { id = bookId });
                }

                // Duygu analizi
                _logger.LogInformation("Duygu analizi başlatılıyor");
                var sentiment = await _sentimentAnalysisService.AnalyzeSentimentAsync(content);
                _logger.LogInformation("Duygu analizi sonucu: {Sentiment}", sentiment);

                var review = new Review
                {
                    BookId = bookId,
                    UserId = user.Id,
                    Content = content,
                    CreatedAt = DateTime.Now,
                    Sentiment = sentiment
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Yorum başarıyla kaydedildi. ReviewId: {ReviewId}", review.Id);

                return RedirectToAction(nameof(Details), new { id = bookId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yorum eklerken hata oluştu");
                TempData["ErrorMessage"] = "Yorum eklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return RedirectToAction(nameof(Details), new { id = bookId });
            }
        }

        // POST: Books/MarkAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var userBook = await _context.UserBooks
                .FirstOrDefaultAsync(ub => ub.BookId == bookId && ub.UserId == user.Id);

            if (userBook == null)
            {
                userBook = new UserBook
                {
                    BookId = bookId,
                    UserId = user.Id,
                    IsCompleted = true,
                    LastReadAt = DateTime.Now
                };
                _context.UserBooks.Add(userBook);
            }
            else
            {
                userBook.IsCompleted = true;
                userBook.LastReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = bookId });
        }

        // Kategoriler sekmesi
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.Include(c => c.Books).ToListAsync();
            return View(categories);
        }

        // Belirli kategorideki kitaplar
        public async Task<IActionResult> BooksByCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            ViewBag.CategoryName = category.Name;
            return View(category.Books);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var userBook = await _context.UserBooks
                .FirstOrDefaultAsync(ub => ub.BookId == bookId && ub.UserId == user.Id);

            if (userBook == null)
            {
                userBook = new UserBook
                {
                    BookId = bookId,
                    UserId = user.Id,
                    IsFavorite = true,
                    AddedAt = DateTime.Now
                };
                _context.UserBooks.Add(userBook);
            }
            else
            {
                userBook.IsFavorite = !userBook.IsFavorite;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = bookId });
        }

        public async Task<IActionResult> Category(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            ViewBag.CategoryName = category.Name;
            return View("Index", category.Books);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted([FromBody] MarkCompletedRequest request)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();

            var user = await _userManager.GetUserAsync(User);
            var userBook = await _context.UserBooks
                .FirstOrDefaultAsync(ub => ub.UserId == user.Id && ub.BookId == request.BookId);

            if (userBook != null)
            {
                userBook.IsCompleted = true;
                userBook.LastReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            var book = await _context.Books.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == request.BookId);

            var history = await _context.UserReadingHistories
                .FirstOrDefaultAsync(h => h.UserId == user.Id && h.BookId == request.BookId);
            if (history != null)
            {
                history.IsCompleted = true;
                history.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Kayıt yoksa yeni ekle
                _context.UserReadingHistories.Add(new UserReadingHistory
                {
                    UserId = user.Id,
                    BookId = request.BookId,
                    Category = book?.Category?.Name ?? "",
                    ReadDate = DateTime.Now,
                    IsCompleted = true,
                    ReadingTime = 0
                });
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        public class MarkCompletedRequest
        {
            public int BookId { get; set; }
        }
    }
}