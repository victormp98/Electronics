using Electronic.Data;
using Electronic.Extensions;
using Electronic.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Electronic.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "CartItems";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Productos.Include(p => p.Imagenes).FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound(); // Product not found
            }

            List<CartItemViewModel> cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>(CartSessionKey) ?? new List<CartItemViewModel>();

            var existingCartItem = cart.FirstOrDefault(item => item.ProductId == productId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Brand = product.Brand,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.Imagenes.FirstOrDefault()?.ImageUrl // Take the first image if available
                });
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);

            return RedirectToAction("Index", "Product"); // Redirect back to product catalog
        }

        [HttpGet]
        public IActionResult Index()
        {
            List<CartItemViewModel> cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>(CartSessionKey) ?? new List<CartItemViewModel>();
            return View(cart);
        }
    }
}
