namespace CLDV6212_POE_PART_1.ViewModels;
using System;


public class OrderViewModel
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }

    public DateTime Order_Date { get; set; }
    public int Quantity { get; set; }
    public decimal Total_Amount { get; set; }

    public string CustomerName { get; set; }
    public string ProductName { get; set; }
    public string? Status { get; set; } = "PENDING";
}
