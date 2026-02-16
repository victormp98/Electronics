using Electronic.Data;
using Electronic.Extensions;
using Electronic.Models;
using Electronic.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Electronic.Controllers
{
    [Authorize] // Checkout requires authentication
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "CartItems";

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            List<CartItemViewModel> cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>(CartSessionKey) ?? new List<CartItemViewModel>();

            if (!cart.Any())
            {
                // Cart is empty, redirect to cart page or product catalog
                TempData["Message"] = "Tu carrito está vacío. Añade productos para proceder al pago.";
                return RedirectToAction("Index", "Cart");
            }

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                CartTotal = cart.Sum(item => item.Total)
            };

            // If user is authenticated, load their existing addresses
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int userIdInt))
                {
                    viewModel.ExistingAddresses = await _context.Direcciones
                                                              .Where(d => d.UserId == userIdInt)
                                                              .OrderByDescending(d => d.IsDefault) // Show default first
                                                              .ToListAsync();
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            List<CartItemViewModel> cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>(CartSessionKey) ?? new List<CartItemViewModel>();

            if (!cart.Any())
            {
                ModelState.AddModelError(string.Empty, "Tu carrito está vacío.");
                return View(model);
            }

            // Manually re-populate CartItems and CartTotal, ExistingAddresses for model state validation failure
            model.CartItems = cart;
            model.CartTotal = cart.Sum(item => item.Total);
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(currentUserId, out int currentUserIdInt))
                {
                    model.ExistingAddresses = await _context.Direcciones
                                                              .Where(d => d.UserId == currentUserIdInt)
                                                              .OrderByDescending(d => d.IsDefault)
                                                              .ToListAsync();
                }
            }


            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userId, out int userIdInt))
                {
                    ModelState.AddModelError(string.Empty, "Usuario no autenticado.");
                    return View(model);
                }

                // Create new address if not using an existing one, or update existing one
                Direccion shippingAddress;
                if (model.SelectedAddressId > 0)
                {
                    shippingAddress = await _context.Direcciones.FirstOrDefaultAsync(d => d.Id == model.SelectedAddressId && d.UserId == userIdInt);
                    if (shippingAddress == null)
                    {
                        ModelState.AddModelError(string.Empty, "Dirección seleccionada no válida.");
                        return View(model);
                    }
                    // Update existing address details if they were modified in the form (optional)
                    // For now, we assume if an existing address is selected, its details are used as-is.
                }
                else
                {
                    shippingAddress = new Direccion
                    {
                        UserId = userIdInt,
                        ReceptorName = model.ReceptorName,
                        PhoneNumber = model.PhoneNumber,
                        Street = model.Street,
                        ExteriorNumber = model.ExteriorNumber,
                        InteriorNumber = model.InteriorNumber,
                        Neighborhood = model.Neighborhood,
                        City = model.City,
                        State = model.State,
                        PostalCode = model.PostalCode,
                        References = model.References,
                        IsDefault = false // New addresses are not default by default
                    };
                    _context.Add(shippingAddress);
                    await _context.SaveChangesAsync(); // Save address to get its ID
                }
                
                // Create the Order
                var order = new Pedido
                {
                    UserId = userIdInt,
                    DireccionId = shippingAddress.Id,
                    Date = DateTime.UtcNow,
                    Total = model.CartTotal,
                    Status = PedidoStatus.PENDIENTE // Initial status
                };
                _context.Add(order);
                await _context.SaveChangesAsync();

                // Create Order Details
                foreach (var item in cart)
                {
                    var orderDetail = new DetallePedido
                    {
                        PedidoId = order.Id,
                        ProductoId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };
                    _context.Add(orderDetail);
                }
                await _context.SaveChangesAsync();

                // Clear the cart from session
                HttpContext.Session.Remove(CartSessionKey);

                TempData["Message"] = $"¡Tu pedido #{order.Id} ha sido realizado con éxito!";
                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }

            // If ModelState is not valid, the model with errors will be returned to the view,
            // which will automatically display validation messages.

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Pedidos.Include(o => o.User).Include(o => o.Direccion).FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Message"] = "Orden no encontrada.";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            var order = await _context.Pedidos.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Message"] = "Orden no encontrada.";
                return RedirectToAction("Index", "Home");
            }

            // Simulate successful payment
            order.Status = PedidoStatus.PAGADO;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"¡El pago del pedido #{orderId} ha sido procesado con éxito!";
            return RedirectToAction("OrderConfirmation", new { orderId = orderId });
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["Message"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account"); // Redirect to login if user ID is not found
            }

            var userOrders = await _context.Pedidos
                                           .Where(o => o.UserId == userIdInt)
                                           .Include(o => o.Direccion) // Include shipping address
                                           .OrderByDescending(o => o.Date)
                                           .ToListAsync();

            return View(userOrders);
        }

        [HttpGet]
        public async Task<IActionResult> RequestReturn(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["Message"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Pedidos
                                      .Include(o => o.User)
                                      .Include(o => o.DetallesPedido)
                                          .ThenInclude(dp => dp.Producto) // Include product details for return form
                                      .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userIdInt);

            if (order == null || order.Status != PedidoStatus.ENTREGADO)
            {
                TempData["Message"] = "No se puede solicitar devolución para esta orden.";
                return RedirectToAction(nameof(MyOrders));
            }
            
            // For now, just pass the order. A more complex form might use a ViewModel
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(int orderId, string returnReason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["Message"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Pedidos.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userIdInt);

            if (order == null || order.Status != PedidoStatus.ENTREGADO)
            {
                TempData["Message"] = "No se puede procesar la devolución para esta orden.";
                return RedirectToAction(nameof(MyOrders));
            }

            order.Status = PedidoStatus.DEVOLUCION;
            // Potentially save returnReason to a dedicated Return entity or an order log
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Se ha solicitado la devolución para el pedido #{orderId} por la razón: {returnReason}.";
            return RedirectToAction(nameof(MyOrders));
        }
    }
}
