using Microsoft.AspNetCore.Mvc;

namespace CLDV6212_POE_PART_1.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            // ✅ Only Admin allowed
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                // Customers and Anonymous users redirected to Products page
                return RedirectToAction("Products", "Product");
            }

            return View();
        }

        public IActionResult Dashboard()
        {
            // ✅ Only Admin allowed
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Products", "Product");
            }

            return View();
        }
    }
}
