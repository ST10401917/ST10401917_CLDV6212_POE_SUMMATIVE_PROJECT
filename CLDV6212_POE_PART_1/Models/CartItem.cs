namespace CLDV6212_POE_PART_1.Models
{
    public class CartItem
    {
        // Id is string to support int ids or RowKey strings
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;

        public decimal LineTotal => Price * Quantity;
    }
}
