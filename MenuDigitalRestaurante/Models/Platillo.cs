using System.ComponentModel.DataAnnotations;

namespace MenuRestaurante.Models
{
    public class Platillo
    {
        [Key]
        public int Id { get; set; }
        public int? CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public bool Disponible { get; set; } = true;
        public string? ImagenUrl { get; set; }

        public virtual Categoria? Categoria { get; set; }
    }
}