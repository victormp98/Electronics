using Electronic.Data;
using Electronic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Electronic.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string productCategory)
        {
            IQueryable<Producto> products = _context.Productos;

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || p.Brand.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(productCategory))
            {
                products = products.Where(p => p.Category == productCategory);
            }
            
            // Populate categories for the view for dropdown
            ViewBag.Categories = await _context.Productos.Select(p => p.Category).Distinct().ToListAsync();
            ViewBag.SearchString = searchString; // Retain search string
            ViewBag.ProductCategory = productCategory; // Retain selected category

            return View(await products.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Productos
                                        .Include(p => p.Imagenes) // Include product images
                                        .Include(p => p.Resenas)  // Include reviews
                                            .ThenInclude(r => r.User) // Include user for reviews
                                        .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Calculate average rating
            ViewBag.AverageRating = product.Resenas.Any() ? product.Resenas.Average(r => r.Rating) : 0;

            return View(product);
        }

        [HttpGet]
        [Authorize] // Only authenticated users can rate products
        public async Task<IActionResult> RateProduct(int productId)
        {
            var product = await _context.Productos.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            // Check if user has already reviewed this product (optional, but good UX)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out int userIdInt))
            {
                var existingReview = await _context.Resenas.FirstOrDefaultAsync(r => r.ProductoId == productId && r.UserId == userIdInt);
                if (existingReview != null)
                {
                    TempData["Message"] = "Ya has enviado una reseña para este producto. Puedes editarla si deseas (funcionalidad no implementada).";
                    return RedirectToAction(nameof(Details), new { id = productId });
                }
            }

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.Name;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateProduct(int productId, int rating, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["Message"] = "No se pudo identificar al usuario. Inicie sesión para enviar una reseña.";
                return RedirectToAction("Login", "Account");
            }

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("rating", "La calificación debe estar entre 1 y 5.");
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                ModelState.AddModelError("comment", "El comentario no puede estar vacío.");
            }

            if (ModelState.IsValid)
            {
                var product = await _context.Productos.FindAsync(productId);
                if (product == null)
                {
                    return NotFound();
                }

                var review = new Resena
                {
                    ProductoId = productId,
                    UserId = userIdInt,
                    Rating = rating,
                    Comment = comment,
                    Date = DateTime.UtcNow
                };

                _context.Resenas.Add(review);
                await _context.SaveChangesAsync();

                TempData["Message"] = "¡Gracias por tu reseña!";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            // If ModelState is invalid, re-show the form
            ViewBag.ProductId = productId;
            ViewBag.ProductName = (await _context.Productos.FindAsync(productId))?.Name;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["Message"] = "No se pudo identificar al usuario. Inicie sesión para añadir a la lista de deseos.";
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Productos.FindAsync(productId);
            if (product == null)
            {
                TempData["Message"] = "Producto no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Check if already in wishlist
            var existingWishlistItem = await _context.Wishlists
                                                     .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == userIdInt);

            if (existingWishlistItem != null)
            {
                TempData["Message"] = "Este producto ya está en tu lista de deseos.";
            }
            else
            {
                var wishlistItem = new Wishlist
                {
                    ProductId = productId,
                    UserId = userIdInt,
                    DateAdded = DateTime.UtcNow
                };
                _context.Wishlists.Add(wishlistItem);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Producto añadido a tu lista de deseos.";
            }

            return RedirectToAction(nameof(Details), new { id = productId });
        }
    }
}
