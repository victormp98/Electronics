using Electronic.Data;
using Electronic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Electronic.Controllers
{
    [Authorize] // For now, just authorize, later could be [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await _context.Productos.ToListAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Producto creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Productos.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Producto actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        private bool ProductExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }

        // GET: Admin/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Productos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Productos.FindAsync(id);
            if (product != null)
            {
                _context.Productos.Remove(product);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Producto eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var products = await _context.Productos.OrderBy(p => p.Name).ToListAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int productId, int newStock)
        {
            var product = await _context.Productos.FindAsync(productId);

            if (product == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction(nameof(Inventory));
            }

            if (newStock < 0)
            {
                TempData["Error"] = "El stock no puede ser negativo.";
                return RedirectToAction(nameof(Inventory));
            }

            product.Stock = newStock;
            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Stock del producto '{product.Name}' actualizado a {newStock}.";
            return RedirectToAction(nameof(Inventory));
        }

        [HttpGet]
        public async Task<IActionResult> SalesReports()
        {
            var totalRevenue = await _context.Pedidos.Where(p => p.Status == PedidoStatus.PAGADO).SumAsync(p => p.Total);
            var totalOrders = await _context.Pedidos.CountAsync(p => p.Status == PedidoStatus.PAGADO);

            var topSellingProducts = await _context.DetallePedidos
                .Where(dp => dp.Pedido.Status == PedidoStatus.PAGADO)
                .GroupBy(dp => dp.Producto)
                .Select(g => new
                {
                    Product = g.Key,
                    QuantitySold = g.Sum(dp => dp.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();

            // Use a dynamic object or a dedicated ViewModel for the report data
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TopSellingProducts = topSellingProducts;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManageReturns()
        {
            var returnRequests = await _context.Pedidos
                                               .Where(o => o.Status == PedidoStatus.DEVOLUCION)
                                               .Include(o => o.User)
                                               .OrderByDescending(o => o.Date)
                                               .ToListAsync();
            return View(returnRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReturnStatus(int orderId, PedidoStatus newStatus)
        {
            var order = await _context.Pedidos.FindAsync(orderId);

            if (order == null)
            {
                TempData["Error"] = "Orden no encontrada.";
                return RedirectToAction(nameof(ManageReturns));
            }

            // Only allow status changes for return-related states
            if (newStatus == PedidoStatus.DEVOLUCION_APROBADA || newStatus == PedidoStatus.DEVOLUCION_RECHAZADA)
            {
                order.Status = newStatus;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"El estado de la devolución para la orden #{orderId} ha sido actualizado a {newStatus}.";
            }
            else
            {
                TempData["Error"] = "Estado de devolución no válido.";
            }

            return RedirectToAction(nameof(ManageReturns));
        }

        [HttpGet]
        public async Task<IActionResult> ManageReviews()
        {
            var reviews = await _context.Resenas
                                        .Include(r => r.User)
                                        .Include(r => r.Producto)
                                        .OrderByDescending(r => r.Date)
                                        .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var review = await _context.Resenas.FindAsync(reviewId);

            if (review == null)
            {
                TempData["Error"] = "Reseña no encontrada.";
                return RedirectToAction(nameof(ManageReviews));
            }

            _context.Resenas.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Reseña eliminada exitosamente.";
            return RedirectToAction(nameof(ManageReviews));
        }

        [HttpGet]
        public async Task<IActionResult> ManageCoupons()
        {
            var coupons = await _context.Cupones.OrderByDescending(c => c.StartDate).ToListAsync();
            return View(coupons);
        }

        [HttpGet]
        public IActionResult CreateCoupon()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCoupon(Cupon coupon)
        {
            if (ModelState.IsValid)
            {
                _context.Cupones.Add(coupon);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Cupón creado exitosamente.";
                return RedirectToAction(nameof(ManageCoupons));
            }
            return View(coupon);
        }

        [HttpGet]
        public async Task<IActionResult> EditCoupon(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Cupones.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCoupon(int id, Cupon coupon)
        {
            if (id != coupon.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coupon);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Cupón actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageCoupons));
            }
            return View(coupon);
        }

        private bool CouponExists(int id)
        {
            return _context.Cupones.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteCoupon(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Cupones.FirstOrDefaultAsync(m => m.Id == id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        [HttpPost, ActionName("DeleteCoupon")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCouponConfirmed(int id)
        {
            var coupon = await _context.Cupones.FindAsync(id);
            if (coupon != null)
            {
                _context.Cupones.Remove(coupon);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Cupón eliminado exitosamente.";
            return RedirectToAction(nameof(ManageCoupons));
        }
    }
}
