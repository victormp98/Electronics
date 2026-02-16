namespace Electronic.Models
{
    public class ProductoImagen
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public bool IsMain { get; set; }

        // Foreign Key for Producto
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
    }
}
