using CLDV6212_POE_PART_1.Data;
using CLDV6212_POE_PART_1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLDV6212_POE_PART_1.Controllers
{
    public class LoginController : Controller
    {
        private readonly CLDV6212_POE_PART_1Context _context;

        public LoginController(CLDV6212_POE_PART_1Context context)
        {
            _context = context;
        }

        //  Login Page 
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Please enter username and password";
                return RedirectToAction("Index");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                TempData["Error"] = "Invalid username or password";
                return RedirectToAction("Index");
            }

            // Save user info in session
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            // Redirect based on role
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Home"); // Admin home
            else
                return RedirectToAction("Index", "Home"); // Customer home
        }

        //  Logout 
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // Register Page 
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            // Check if username or email already exists
            if (_context.Users.Any(u => u.Username == user.Username || u.Email == user.Email))
            {
                TempData["Error"] = "Username or Email already exists";
                return View(user);
            }

            user.Role = "Customer"; // default role
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Account created successfully! You can now login.";
            return RedirectToAction("Index");
        }

    }
}
