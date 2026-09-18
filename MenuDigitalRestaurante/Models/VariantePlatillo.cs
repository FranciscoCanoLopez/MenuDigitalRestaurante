namespace MenuDigitalRestaurante.Models
{
    public class VariantePlatillo
    {
        public int Id { get; set; }
        public int PlatilloId { get; set; }
        public Platillo? Platillo { get; set; }
        
        // Ejemplos: "Sencillo", "Doble", "Chico", "Mediano", "Grande", "1/2 Litro", "1 Litro", "6 Piezas"
        public string NombreVariante { get; set; } = string.Empty;
        
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;
    }
}