using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SesliKitapWeb.Data;
using SesliKitapWeb.Models;
using SesliKitapWeb.Services;

namespace SesliKitapWeb.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SentimentAnalysisService _sentimentAnalysisService;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SentimentAnalysisService sentimentAnalysisService)
        {
            _context = context;
            _userManager = userManager;
            _sentimentAnalysisService = sentimentAnalysisService;
        }

        // POST: Reviews/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            // Sadece admin veya yorumun sahibi silebilir
            if (User.IsInRole("Admin") || review.UserId == user?.Id)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Books", new { id = review.BookId });
        }

        // Yorum ekleme (duygu analizi ile)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(int bookId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // NLP ile duygu analizi
            var sentiment = await _sentimentAnalysisService.AnalyzeSentimentAsync(content);

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

            return RedirectToAction("Details", "Books", new { id = bookId });
        }
    }
}