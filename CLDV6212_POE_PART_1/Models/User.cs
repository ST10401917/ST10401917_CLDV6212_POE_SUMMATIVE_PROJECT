using System.ComponentModel.DataAnnotations;

namespace CLDV6212_POE_PART_1.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        // Login username
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        // Simple password (you can hash later)
        [Required]
        public string Password { get; set; }

        // Default role = Customer
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        // Additional Account Info
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }
    }
}
