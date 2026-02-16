using Electronic.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Electronic.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // Shipping Address Details (derived from Direccion model)
        [Required(ErrorMessage = "El nombre del receptor es requerido.")]
        [Display(Name = "Nombre del Receptor")]
        public string ReceptorName { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido.")]
        [Phone(ErrorMessage = "Formato de teléfono inválido.")]
        [Display(Name = "Teléfono")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "La calle es requerida.")]
        [Display(Name = "Calle")]
        public string Street { get; set; }

        [Required(ErrorMessage = "El número exterior es requerido.")]
        [Display(Name = "Número Exterior")]
        public string ExteriorNumber { get; set; }

        [Display(Name = "Número Interior")]
        public string InteriorNumber { get; set; }

        [Required(ErrorMessage = "La colonia es requerida.")]
        [Display(Name = "Colonia")]
        public string Neighborhood { get; set; }

        [Required(ErrorMessage = "La ciudad es requerida.")]
        [Display(Name = "Ciudad")]
        public string City { get; set; }

        [Required(ErrorMessage = "El estado es requerido.")]
        [Display(Name = "Estado")]
        public string State { get; set; }

        [Required(ErrorMessage = "El código postal es requerido.")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "El código postal debe tener 5 dígitos.")]
        [Display(Name = "Código Postal")]
        public string PostalCode { get; set; }

        [Display(Name = "Referencias")]
        public string References { get; set; }

        // Cart Summary
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal CartTotal { get; set; }

        // Payment Method (for now, just a string placeholder)
        [Required(ErrorMessage = "El método de pago es requerido.")]
        [Display(Name = "Método de Pago")]
        public string PaymentMethod { get; set; }

        // Existing Addresses (for selection by authenticated users)
        public List<Direccion> ExistingAddresses { get; set; } = new List<Direccion>();
        public int SelectedAddressId { get; set; } // To select an existing address
    }
}
