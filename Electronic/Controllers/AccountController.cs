using Electronic.Data;
using Electronic.Models;
using Electronic.Models.ViewModels;
using Electronic.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Electronic.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Good practice for POST actions
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user with this email already exists
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "El email ya está registrado.");
                    return View(model);
                }

                var hashedPassword = _passwordHasher.HashPassword(model.Password);

                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = hashedPassword, // Store hashed password
                    PhoneNumber = model.PhoneNumber,
                    ProfilePictureUrl = "" // Default empty for now, user can upload later
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                // TODO: Implement user login/session management here

                return RedirectToAction("Index", "Home"); // Redirect to home or a success page
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.Password))
                {
                    ModelState.AddModelError(string.Empty, "Intento de inicio de sesión no válido.");
                    return View(model);
                }

                // User found and password verified, sign in
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim("FullName", user.Name), // Custom claim
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    // Allow to refresh authentication session
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Set expiration for 30 minutes
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home"); // Redirect to a success page or dashboard
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyWishlist()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["Message"] = "No se pudo identificar al usuario. Inicie sesión para ver su lista de deseos.";
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = await _context.Wishlists
                                              .Where(w => w.UserId == userIdInt)
                                              .Include(w => w.Producto) // Include product details
                                                  .ThenInclude(p => p.Imagenes) // Include product images
                                              .OrderByDescending(w => w.DateAdded)
                                              .ToListAsync();
            return View(wishlistItems);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(int wishlistId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["Message"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            var wishlistItem = await _context.Wishlists.FirstOrDefaultAsync(w => w.Id == wishlistId && w.UserId == userIdInt);

            if (wishlistItem == null)
            {
                TempData["Message"] = "El artículo no se encontró en tu lista de deseos.";
            }
            else
            {
                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Producto eliminado de tu lista de deseos.";
            }

            return RedirectToAction(nameof(MyWishlist));
        }
    }
}
