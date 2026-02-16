namespace Electronic.Models
{
    public class Envio
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public string ShippingCompany { get; set; }
        public string TrackingNumber { get; set; }
        public string Status { get; set; }

        // Foreign Key for Pedido
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }
    }
}
