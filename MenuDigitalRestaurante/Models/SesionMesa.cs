using System.ComponentModel.DataAnnotations;

namespace MenuDigitalRestaurante.Models
{
    public class SesionMesa
    {
        [Key]
        public int Id { get; set; }
        public int? MesaId { get; set; }
        public string TokenSesion { get; set; } = null!;
        public bool PedidoActivo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public virtual Mesa? Mesa { get; set; }
    }
}