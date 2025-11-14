using CLDV6212_POE_PART_1.Filters;
using CLDV6212_POE_PART_1.Models;
using CLDV6212_POE_PART_1.Services;
using CLDV6212_POE_PART_1.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace CLDV6212_POE_PART_1.Controllers
{
    [AuthorizeRole("Admin")]
    public class OrderController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueService _queueService;

        public OrderController(TableStorageService tableStorageService, QueueService queueService)
        {
            _tableStorageService = tableStorageService;
            _queueService = queueService;
        }

        // Display all orders
        public async Task<IActionResult> Index()
        {
            var orders = await _tableStorageService.GetAllOrdersAsync();
            var customers = await _tableStorageService.GetAllCustomersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            // Ensure every order has a status
            foreach (var o in orders)
            {
                if (string.IsNullOrEmpty(o.Status))
                    o.Status = "PENDING";
            }

            // Map orders to ViewModels with better customer matching
            var orderVMs = orders.Select(o =>
            {
                // Match using the Username property
                var customer = customers.FirstOrDefault(c =>
                    string.Equals(c.Username, o.Customer_Id, StringComparison.OrdinalIgnoreCase)
                );

                var product = products.FirstOrDefault(p =>
                    string.Equals(p.RowKey, o.Product_Id, StringComparison.OrdinalIgnoreCase)
                );

                return new OrderViewModel
                {
                    Order_Date = o.Order_Date,
                    Quantity = o.Quantity,
                    Total_Amount = o.Total_Amount,
                    CustomerName = customer != null
                        ? $"{customer.FirstName} {customer.LastName}" // Display full name
                        : o.Customer_Id ?? "Unknown",
                    ProductName = product?.Product_Name ?? "Unknown",
                    PartitionKey = o.PartitionKey,
                    RowKey = o.RowKey,
                    Status = string.IsNullOrEmpty(o.Status) ? "PENDING" : o.Status
                };
            }).ToList();


            return View(orderVMs);
        }




        // Show form to add a new order
        [HttpGet]
        public async Task<IActionResult> AddOrder()
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            if (customers == null || customers.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No customers available. Please add a customer first.");
            }

            if (products == null || products.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No products available. Please add a product first.");
            }

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View();
        }

        // Add order and send message to queue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrder(Order order)
        {
            

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("Validation errors: " + string.Join(", ", errors));

                ViewBag.Customers = await _tableStorageService.GetAllCustomersAsync();
                ViewBag.Products = await _tableStorageService.GetAllProductsAsync();
                return View(order);
            }


            // Set partition & row keys
            order.PartitionKey = "OrderPartition";
            order.RowKey = Guid.NewGuid().ToString();
            order.Order_Date = DateTime.SpecifyKind(order.Order_Date, DateTimeKind.Utc);

            // Save to table storage
            await _tableStorageService.AddOrderAsync(order);

            // Send queue message
            string message = $"New order created: {order.RowKey} for Customer: {order.Customer_Id}, Product: {order.Product_Id}";
            await _queueService.SendMessageAsync(message);

            return RedirectToAction("Index");
        }


        // Add this method inside the OrderController class
        // Mark an order as PROCESSED
        [HttpPost]
        public async Task<IActionResult> MarkProcessed(string partitionKey, string rowKey)
        {
            Console.WriteLine($"MarkProcessed called for {rowKey}");

            var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                Console.WriteLine($"Order {rowKey} not found!");
                return RedirectToAction("Index");
            }

            order.Status = "PROCESSED";
            await _tableStorageService.UpdateOrderAsync(order);

            // Wait briefly to ensure Azure syncs the update
            await Task.Delay(800);

            // Re-fetch order to confirm it was updated
            var refreshed = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
            Console.WriteLine($"Order {rowKey} status now: {refreshed?.Status}");

            return RedirectToAction("Index");
        }





        // Delete an order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }


    }
}
