using System.ComponentModel.DataAnnotations;

namespace MenuRestaurante.Models
{
    public class DetallePedido
    {
        [Key]
        public int Id { get; set; }
        public int? PedidoId { get; set; }
        public int? PlatilloId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string? NotasEspeciales { get; set; } // Sin cebolla, etc.

        public virtual Pedido? Pedido { get; set; }
        public virtual Platillo? Platillo { get; set; }
    }
}