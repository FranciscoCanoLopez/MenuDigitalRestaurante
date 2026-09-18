using System.ComponentModel.DataAnnotations;

namespace MenuDigitalRestaurante.Models
{
    public class Platillo
    {
        [Key]
        public int Id { get; set; }
        public int? CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public bool Disponible { get; set; } // Sirve para indicar "Agotado hoy"
        public bool Activo { get; set; } = true; // NUEVO: Sirve para Borrado Lógico (Descontinuado)
        public string? ImagenUrl { get; set; }
        public ICollection<VariantePlatillo> Variantes { get; set; } = new List<VariantePlatillo>();
        public virtual Categoria? Categoria { get; set; }
    }
}