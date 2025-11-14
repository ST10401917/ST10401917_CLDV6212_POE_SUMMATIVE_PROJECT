using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace CLDV6212_POE_PART_1.Models
{
    public class Order : ITableEntity
    {
        [Key]
        public int Order_Id { get; set; }

        [Required(ErrorMessage = "Please enter a order date")]
        public DateTime Order_Date { get; set; }

        [Required(ErrorMessage = "Please enter a quautity")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Please enter a total Amount")]
        public int Total_Amount { get; set; }

        public string Status { get; set; } = "Pending"; 

        public string? Customer_Id { get; set; }
        public string? Product_Id { get; set; }


        public string? CustomerUsername { get; set; }  
        public string? CustomerName { get; set; }      

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
