using CLDV6212_POE_PART_1.Models;
using CLDV6212_POE_PART_1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CLDV6212_POE_PART_1.Filters;


namespace CLDV6212_POE_PART_1.Controllers
{
    [AuthorizeRole("Admin")]
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        public CustomerController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        // Display all customers
        public async Task<IActionResult> Index(string searchTerm)
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                customers = customers
                    .Where(c => c.Customer_Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                             || c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewData["SearchTerm"] = searchTerm;
            return View(customers);
        }

        // GET: Show Add Customer form
        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View();
        }

        // POST: Add new customer
        [HttpPost]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            try
            {
                customer.PartitionKey = "CustomerPartition";
                customer.RowKey = Guid.NewGuid().ToString();

                await _tableStorageService.AddCustomerAsync(customer);

                TempData["SuccessMessage"] = "Customer added successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error adding customer: {ex.Message}");
                return View(customer);
            }
        }

        // POST: Delete customer
        [HttpPost]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            try
            {
                // Optionally check for orders before deleting
                var orders = await _tableStorageService.GetAllOrdersAsync();
                if (orders.Any(o => o.Customer_Id == rowKey))
                {
                    TempData["ErrorMessage"] = "Cannot delete this customer because they have existing orders.";
                    return RedirectToAction("Index");
                }

                await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
                TempData["SuccessMessage"] = "Customer deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
