using System.Collections.Generic;

namespace Electronic.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Note: Should be stored as a hash
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }

        // Navigation properties based on ER Diagram (Section 3.2)
        public ICollection<Direccion> Direcciones { get; set; }
        public ICollection<Pedido> Pedidos { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; }
        public ICollection<Resena> Resenas { get; set; }
    }
}
