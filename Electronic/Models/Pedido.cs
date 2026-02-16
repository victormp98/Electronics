using System;
using System.Collections.Generic;

namespace Electronic.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public PedidoStatus Status { get; set; }

        // Foreign Keys
        public int UserId { get; set; }
        public User User { get; set; }

        public int DireccionId { get; set; }
        public Direccion Direccion { get; set; }

        // Navigation properties based on ER Diagram (Section 3.2)
        public ICollection<DetallePedido> DetallesPedido { get; set; }
        public Pago Pago { get; set; }
        public Envio Envio { get; set; }
    }
}
