using System;
using System.Collections.Generic; // Import this for List<>
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingApp.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        [Index(IsUnique = true)]
        public string Email { get; set; }

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; }

        [MaxLength(50)]
        public string Role { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties (add these based on your database relationships)
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Restaurant> Restaurants { get; set; } // For restaurants owned by this user
        public virtual ICollection<Review> Reviews { get; set; }

        public User()
        {
            Notifications = new HashSet<Notification>();
            Orders = new HashSet<Order>();
            Restaurants = new HashSet<Restaurant>();
            Reviews = new HashSet<Review>();
        }
    }
}