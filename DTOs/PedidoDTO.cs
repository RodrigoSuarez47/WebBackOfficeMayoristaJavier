using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
        public class PedidoDTO
        {
            public int Id { get; set; }
            public DateTime OrderDate { get; set; }
            public string Customer { get; set; }
            public List<LineaPedidoDTO> Lines { get; set; }
            public decimal? Total { get; set; }
            public string Status { get; set; }
        }
}
