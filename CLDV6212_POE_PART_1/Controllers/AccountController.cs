using Microsoft.AspNetCore.Mvc;
using CLDV6212_POE_PART_1.Data;
using System.Linq;

namespace CLDV6212_POE_PART_1.Controllers
{
    public class AccountController : Controller
    {
        private readonly CLDV6212_POE_PART_1Context _context;

        public AccountController(CLDV6212_POE_PART_1Context context)
        {
            _context = context;
        }

        public IActionResult Profile()
        {
            // Get username from session
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Fetch user
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return RedirectToAction("Index", "Login");

            return View(user);
        }
    }
}
