using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class UsuarioDTO
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El Nombre es obligatorio.")]
        public string Name { get; set; }

        [Display(Name = "Contraseña")]
        [Required(ErrorMessage = "La Contraseña es obligatoria.")]
        public string Password { get; set; }
    }
}
