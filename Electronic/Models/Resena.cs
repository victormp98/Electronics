using System;

namespace Electronic.Models
{
    public class Resena
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }

        // Foreign Keys
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
