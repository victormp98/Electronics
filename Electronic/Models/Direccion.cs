namespace Electronic.Models
{
    public class Direccion
    {
        public int Id { get; set; }
        public string ReceptorName { get; set; }
        public string PhoneNumber { get; set; }
        public string Street { get; set; }
        public string ExteriorNumber { get; set; }
        public string InteriorNumber { get; set; }
        public string Neighborhood { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string References { get; set; }
        public bool IsDefault { get; set; }

        // Foreign Key for User
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
