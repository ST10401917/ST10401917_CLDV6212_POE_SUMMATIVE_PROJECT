using CLDV6212_POE_PART_1.Models;
using CLDV6212_POE_PART_1.Services;
using CLDV6212_POE_PART_1.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CLDV6212_POE_PART_1.Controllers
{
    public class CartController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueService _queueService;

        public CartController(TableStorageService tableStorageService, QueueService queueService)
        {
            _tableStorageService = tableStorageService;
            _queueService = queueService;
        }

        // Display all cart items
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (username == null) return RedirectToAction("Index", "Login");

            var orders = await _tableStorageService.GetAllOrdersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            // Filter only this customer's orders
            var customerOrders = orders.Where(o => o.Customer_Id == username).ToList();

            // Map to ViewModel including ProductName
            var cartItems = customerOrders.Select(o =>
            {
                var product = products.FirstOrDefault(p => p.RowKey == o.Product_Id);
                return new OrderViewModel
                {
                    Order_Date = o.Order_Date,
                    Quantity = o.Quantity,
                    Total_Amount = o.Total_Amount,
                    ProductName = product?.Product_Name ?? o.Product_Id,
                    Status = string.IsNullOrEmpty(o.Status) ? "PENDING" : o.Status,
                    PartitionKey = o.PartitionKey,
                    RowKey = o.RowKey
                };
            }).ToList();

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (username == null) return RedirectToAction("Index", "Login");

            var total = await CalculateTotal(productId, quantity);

            var order = new Order
            {
                PartitionKey = "OrderPartition",
                RowKey = Guid.NewGuid().ToString(),
                Customer_Id = username,
                Product_Id = productId,
                Quantity = quantity,
                Total_Amount = total,
                Order_Date = DateTime.UtcNow,
                Status = "PENDING"
            };

            await _tableStorageService.AddOrderAsync(order);

            // Optionally send a queue message
            string message = $"New order created: {order.RowKey} for Customer: {order.Customer_Id}, Product: {order.Product_Id}";
            await _queueService.SendMessageAsync(message);

            TempData["Message"] = "Item added to cart successfully!";
            return RedirectToAction("Index");
        }

        private async Task<int> CalculateTotal(string productId, int quantity)
        {
            var products = await _tableStorageService.GetAllProductsAsync();
            var product = products.FirstOrDefault(p => p.RowKey == productId);
            return product?.Price * quantity ?? 0;
        }
    }
}
