using CLDV6212_POE_PART_1.Filters;
using CLDV6212_POE_PART_1.Models;
using CLDV6212_POE_PART_1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLDV6212_POE_PART_1.Controllers
{
    
    public class ProductController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableStorageService _tableStorageService;

        public ProductController(BlobService blobService, TableStorageService tableStorageService)
        {
            _blobService = blobService;
            _tableStorageService = tableStorageService;
        }

        // List all products
        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllProductsAsync();
            return View(products);
        }

        // GET: Show Add Product form
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        // POST: Add product with optional image
        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile file)
        {
            if (!ModelState.IsValid)
                return View(product);

            // Optional: upload image if file is provided
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadBlobAsync(stream, file.FileName);
                product.ImageUrl = imageUrl;
            }

            product.PartitionKey = "ProductPartition";
            product.RowKey = Guid.NewGuid().ToString();

            await _tableStorageService.AddProductAsync(product);

            TempData["SuccessMessage"] = "Product added successfully.";
            return RedirectToAction("Index");
        }

        // POST: Delete product
        [HttpPost]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey, string imageUrl)
        {
            // Prevent deleting if product is used in orders
            var orders = await _tableStorageService.GetAllOrdersAsync();
            if (orders.Any(o => o.Product_Id == rowKey))
            {
                TempData["ErrorMessage"] = "Cannot delete this product because it is used in an order.";
                return RedirectToAction("Index");
            }

            // Delete blob image if exists
            if (!string.IsNullOrEmpty(imageUrl))
            {
                await _blobService.DeleteBlobAsync(imageUrl);
            }

            // Delete product from table storage
            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);

            TempData["SuccessMessage"] = "Product deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
