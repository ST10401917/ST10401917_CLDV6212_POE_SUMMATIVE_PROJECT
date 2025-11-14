using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace CLDV6212_POE_PART_1.Models
{
    public class Customer : ITableEntity
    {
        [Key]
        public int Customer_Id { get; set; }
        [Required(ErrorMessage = "Please enter a customer name")]
        public string? Customer_Name { get; set; }

        [Required(ErrorMessage = "Please enter a Email Address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Please enter a Phone Number")]
        public string? Phone_Number { get; set; }

        [Required(ErrorMessage = "Please enter a Address")]
        public string? Address { get; set; }

        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }

    }
}
