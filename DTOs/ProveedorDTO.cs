using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class ProveedorDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido.")]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la empresa es requerido.")]
        [Display(Name = "Nombre de la Empresa")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono principal es requerido.")]
        [Display(Name = "Teléfono Principal")]
        [RegularExpression(@"^\d{8,9}$", ErrorMessage = "El teléfono debe tener 8 o 9 dígitos.")]
        public string PrincipalPhone { get; set; }

        [Display(Name = "Teléfono Alternativo")]
        [RegularExpression(@"^\d{8,9}$", ErrorMessage = "El teléfono debe tener 8 o 9 dígitos.")]
        public string AlternativePhone { get; set; }
    }
}
