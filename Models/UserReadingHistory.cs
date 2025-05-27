using System;
using System.ComponentModel.DataAnnotations;

namespace SesliKitapWeb.Models
{
    public class UserReadingHistory
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public int BookId { get; set; }
        
        [Required]
        public string Category { get; set; }
        
        public DateTime ReadDate { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public float ReadingTime { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public Book Book { get; set; }
    }
} 