using System.ComponentModel.DataAnnotations;

namespace MenuDigitalRestaurante.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        // NUEVO: Para borrado lógico
        public bool Activo { get; set; } = true;
        public virtual ICollection<Platillo> Platillos { get; set; } = new List<Platillo>();
    }
}