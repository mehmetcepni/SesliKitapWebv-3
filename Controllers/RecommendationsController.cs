using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SesliKitapWeb.Models;
using SesliKitapWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace SesliKitapWeb.Controllers
{
    [Authorize]
    public class RecommendationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecommendationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            // Kullanıcının okuma geçmişini al
            var userHistory = await _context.UserReadingHistories
                .Where(h => h.UserId == user.Id)
                .Include(h => h.Book)
                .ToListAsync();

            // Kullanıcının en çok okuduğu kategorileri bul
            var favoriteCategories = userHistory
                .GroupBy(h => h.Category)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            var readBookIds = userHistory.Select(h => h.BookId).ToList();

            // Önerileri oluştur
            var recommendations = new List<BookRecommendation>();

            foreach (var category in favoriteCategories)
            {
                var booksInCategory = await _context.Books
                    .Include(b => b.Category)
                    .Where(b => b.Category != null && b.Category.Name == category)
                    .Where(b => !readBookIds.Contains(b.Id))
                    .Take(5)
                    .ToListAsync();

                foreach (var book in booksInCategory)
                {
                    recommendations.Add(new BookRecommendation
                    {
                        UserId = user.Id,
                        BookId = book.Id,
                        RelevanceScore = 1.0f, // Basit bir skor(düzelticem)
                        GeneratedDate = DateTime.Now,
                        Book = book
                    });
                }
            }

            return View(recommendations);
        }
    }
} 