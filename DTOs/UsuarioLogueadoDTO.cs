using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class UsuarioLogueadoDTO
    {
        public int Id { get; set; }
        [Display(Name = "Nombre")]
        public string Name { get; set; }
        public string Token { get; set; }
    }
}
