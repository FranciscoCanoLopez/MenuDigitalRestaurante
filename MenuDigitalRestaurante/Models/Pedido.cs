using System.ComponentModel.DataAnnotations;

namespace MenuDigitalRestaurante.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }
        public int? MesaId { get; set; } // Nullable para pedidos a domicilio
        public string MetodoPago { get; set; } = null!;
        public string EstadoPedido { get; set; } = "Pendiente";
        public decimal Total { get; set; }
        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
        public string TipoAlerta { get; set; } = "Ninguna";
        public virtual Mesa? Mesa { get; set; }
        public virtual ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    }
}