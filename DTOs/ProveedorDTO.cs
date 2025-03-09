using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class ProveedorDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la empresa es requerido.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono principal es requerido.")]
        public string PrincipalPhone { get; set; }

        public string AlternativePhone { get; set; }
     }

}
