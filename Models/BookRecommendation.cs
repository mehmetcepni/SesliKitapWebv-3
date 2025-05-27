using System;
using System.ComponentModel.DataAnnotations;

namespace SesliKitapWeb.Models
{
    public class BookRecommendation
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public int BookId { get; set; }
        
        public float RelevanceScore { get; set; }
        
        public DateTime GeneratedDate { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public Book Book { get; set; }
    }
} 