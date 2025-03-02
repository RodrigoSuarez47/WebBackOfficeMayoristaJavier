using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class UsuarioLogueadoDTO
    {
        public int Id { get; set; }
        [Display (Name = "Nombre")]
        public string Name { get; set; }
        public string Token { get; set; }
    }
}
