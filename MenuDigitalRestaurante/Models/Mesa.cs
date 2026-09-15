using System.ComponentModel.DataAnnotations;

namespace MenuDigitalRestaurante.Models
{
    public class Mesa
    {
        [Key]
        public int Id { get; set; }
        public int NumeroMesa { get; set; }
        public string TokenQr { get; set; } = null!;
    }
}