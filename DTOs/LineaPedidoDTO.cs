using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class LineaPedidoDTO
    {
            public int Id { get; set; }

            [Required(ErrorMessage = "El artículo es requerido.")]
            public int ArticleId { get; set; }

            [Required(ErrorMessage = "La cantidad es requerida.")]
            [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
            public int Quantity { get; set; }

            [Required(ErrorMessage = "El precio unitario es requerido.")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor a 0.")]
            public decimal UnitPrice { get; set; }

            public bool Assembled { get; set; } = false;

            public decimal? Weight { get; set; }

            public decimal GetSubtotal() => Quantity * UnitPrice;
    }
}
