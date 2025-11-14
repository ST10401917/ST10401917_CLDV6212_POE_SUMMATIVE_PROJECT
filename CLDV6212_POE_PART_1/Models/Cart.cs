namespace CLDV6212_POE_PART_1.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal TotalPrice => Items.Sum(i => i.Price * i.Quantity);
    }
}
