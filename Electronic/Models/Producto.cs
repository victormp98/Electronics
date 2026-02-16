using System.Collections.Generic;

namespace Electronic.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; }
        public string Specifications { get; set; }
        public string Warranty { get; set; }

        // Navigation properties based on ER Diagram (Section 3.2)
        public ICollection<ProductoImagen> Imagenes { get; set; }
        public ICollection<Resena> Resenas { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; }
        public ICollection<DetallePedido> DetallesPedido { get; set; }
    }
}
