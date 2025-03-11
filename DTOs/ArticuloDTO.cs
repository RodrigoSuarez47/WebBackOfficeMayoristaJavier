using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class ArticuloDTO
    {
        public int Id { get; set; }

        [Display(Name = "Imagen")]
        [Required(ErrorMessage = "La Imagen es obligatoria.")]
        public string ImageUrl { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El Nombre es obligatorio.")]
        public string Name { get; set; }

        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [Display(Name = "Precio de Compra")]
        public decimal PurchasePrice { get; set; }

        [Display(Name = "Precio Mayorista")]
        [Required(ErrorMessage = "El Precio Mayorista es obligatorio.")]
        public decimal SalePrice { get; set; }

        [Display(Name = "Precio Unitario")]
        public decimal UnitSalePrice { get; set; }

        [Display(Name = "Mínimo de Compra")]
        [Required(ErrorMessage = "El Mínimo de Compra es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Mínimo de Compra debe ser mayor que 0.")]
        public int MinimumPurchase { get; set; }

        [Display(Name = "Stock")]
        [Required(ErrorMessage = "El Stock es obligatorio.")]
        public int Stock { get; set; }
        [Display(Name = "Vencimiento")]
        public DateTime? ExpirationDate { get; set; }
        public int SupplierId { get; set; }
        [Display(Name = "Visible")]
        public bool IsVisible { get; set; }
        [Display(Name = "Venta por Peso")]
        public bool IsSoldByWeight { get; set; }
    }
}
