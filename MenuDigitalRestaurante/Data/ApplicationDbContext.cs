using Microsoft.EntityFrameworkCore;
using MenuDigitalRestaurante.Models;

namespace MenuDigitalRestaurante.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Platillo> Platillos { get; set; }
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<SesionMesa> SesionesMesas { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallePedidos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo estricto de minúsculas para tablas y columnas en PostgreSQL
            modelBuilder.Entity<Categoria>(entity => {
                entity.ToTable("categorias");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Activo).HasColumnName("activo");
            });

            modelBuilder.Entity<Platillo>(entity => {
                entity.ToTable("platillos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CategoriaId).HasColumnName("categoriaid");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
                entity.Property(e => e.Precio).HasColumnName("precio");
                entity.Property(e => e.Disponible).HasColumnName("disponible");
                entity.Property(e => e.ImagenUrl).HasColumnName("imagenurl");
                entity.Property(e => e.Activo).HasColumnName("activo");
            });

            modelBuilder.Entity<Mesa>(entity => {
                entity.ToTable("mesas");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NumeroMesa).HasColumnName("numeromesa");
                entity.Property(e => e.TokenQr).HasColumnName("tokenqr");
            });

            modelBuilder.Entity<SesionMesa>(entity => {
                entity.ToTable("sesionesmesas");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MesaId).HasColumnName("mesaid");
                entity.Property(e => e.TokenSesion).HasColumnName("tokensesion");
                entity.Property(e => e.PedidoActivo).HasColumnName("pedidoactivo");
                entity.Property(e => e.FechaCreacion).HasColumnName("fechacreacion");
            });

            modelBuilder.Entity<Pedido>(entity => {
                entity.ToTable("pedidos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MesaId).HasColumnName("mesaid");
                entity.Property(e => e.MetodoPago).HasColumnName("metodopago");
                entity.Property(e => e.EstadoPedido).HasColumnName("estadopedido");
                entity.Property(e => e.Total).HasColumnName("total");
                entity.Property(e => e.FechaPedido).HasColumnName("fechapedido");
            });

            modelBuilder.Entity<DetallePedido>(entity => {
                entity.ToTable("detallepedidos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PedidoId).HasColumnName("pedidoid");
                entity.Property(e => e.PlatilloId).HasColumnName("platilloid");
                entity.Property(e => e.Cantidad).HasColumnName("cantidad");
                entity.Property(e => e.PrecioUnitario).HasColumnName("preciounitario");
                entity.Property(e => e.NotasEspeciales).HasColumnName("notasespeciales");
            });
        }
    }
}