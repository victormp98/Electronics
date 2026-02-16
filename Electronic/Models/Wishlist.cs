using System;

namespace Electronic.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public DateTime DateAdded { get; set; }

        // Foreign Keys
        public int UserId { get; set; }
        public User User { get; set; }

        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
    }
}
