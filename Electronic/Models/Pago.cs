using System;

namespace Electronic.Models
{
    public class Pago
    {
        public int Id { get; set; }
        public string Method { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }

        // Foreign Key for Pedido
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }
    }
}
