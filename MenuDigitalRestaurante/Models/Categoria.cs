using System.ComponentModel.DataAnnotations;

namespace MenuDigitalRestaurante.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        
        public virtual ICollection<Platillo> Platillos { get; set; } = new List<Platillo>();
    }
}