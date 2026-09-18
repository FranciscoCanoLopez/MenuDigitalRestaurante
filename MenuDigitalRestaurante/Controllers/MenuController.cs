using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MenuDigitalRestaurante.Data;
using MenuDigitalRestaurante.Models; // Agregado para acceder a Pedido, DetallePedido, etc.

namespace MenuRestaurante.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos la base de datos en el controlador
        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Esta es la ruta principal a la que entrará el QR: ej. /Menu?mesa=5
        public async Task<IActionResult> Index(int mesa)
        {
            // 1. Validar que la mesa ingresada exista en la base de datos
            var mesaDb = await _context.Mesas.FirstOrDefaultAsync(m => m.NumeroMesa == mesa);
            if (mesaDb == null)
            {
                // Si alguien inventa un número de mesa, le mostramos un error
                return Content("Error: La mesa indicada no existe. Por favor escanea el código QR nuevamente.");
            }

            // 2. Verificar si la mesa ya tiene un pedido activo (bloqueo)
            var sesionActiva = await _context.SesionesMesas
                .FirstOrDefaultAsync(s => s.MesaId == mesaDb.Id && s.PedidoActivo == true);

            if (sesionActiva != null)
            {
                // Si ya pidió, lo redirigimos a una pantalla de espera
                return RedirectToAction("PedidoEnProceso", new { mesa = mesaDb.NumeroMesa });
            }

            // 3. Si la mesa está libre, consultamos el menú completo
            // Incluimos las categorías y solo los platillos que estén disponibles
            var categoriasConPlatillos = await _context.Categorias
            .Where(c => c.Activo == true)
            .OrderBy(c => c.Orden) // <-- ORDENA POR EL CAMPO ORDEN
            .Include(c => c.Platillos.Where(p => p.Disponible == true && p.Activo == true))
                .ThenInclude(p => p.Variantes.Where(v => v.Activo == true))
            .ToListAsync();

            // 4. Mandamos datos importantes a la Vista usando ViewBag
            ViewBag.NumeroMesa = mesaDb.NumeroMesa;
            ViewBag.MesaId = mesaDb.Id;

            // Retornamos la vista (el HTML) pasándole la lista de categorías
            return View(categoriasConPlatillos);
        }

        // Esta es la pantalla que se muestra cuando intentan entrar pero ya pidieron
        public IActionResult PedidoEnProceso(int mesa)
        {
            ViewBag.NumeroMesa = mesa;
            return View();
        }

        // --- NUEVA LÓGICA DEL CARRITO ---
[HttpPost]
        public async Task<IActionResult> CrearPedido([FromBody] PedidoRequest request)
        {
            if (request == null || request.Detalles == null || !request.Detalles.Any())
            {
                return Json(new { exito = false, mensaje = "El carrito está vacío." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Crear la cabecera del Pedido
                var nuevoPedido = new Pedido
                {
                    // CS0029: Usamos MesaId (llave foránea) en lugar del objeto Mesa
                    MesaId = request.Mesa, 
                    
                    MetodoPago = request.MetodoPago ?? "Efectivo",
                    Total = request.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario)
                    
                    // Nota: Las propiedades Fecha y Estado fueron comentadas (CS0117). 
                    // Si tienes columnas para guardar la fecha y el estado en tu base de datos, 
                    // agrégalas aquí con el nombre exacto de tu modelo (Ej. FechaPedido = DateTime.Now).
                };

                _context.Pedidos.Add(nuevoPedido);
                await _context.SaveChangesAsync();

                // 2. Crear los detalles del pedido
                foreach (var item in request.Detalles)
                {
                    var detalle = new DetallePedido
                    {
                        PedidoId = nuevoPedido.Id,
                        PlatilloId = item.PlatilloId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        
                        // Nota: Subtotal y Notas fueron omitidos (CS0117). 
                        // El subtotal usualmente se calcula al vuelo o requiere que agregues 
                        // la propiedad "public decimal Subtotal {get;set;}" a tu modelo DetallePedido.

                        NotasEspeciales = item.VarianteNombre
                    };
                    
                    // CS1061: Corregido a DetallePedidos (plural estándar de EF Core)
                    _context.DetallePedidos.Add(detalle);
                }

                // 3. Actualizar la sesión (CS1061: Búsqueda por MesaId)
                var sesionMesa = await _context.SesionesMesas.FirstOrDefaultAsync(s => s.MesaId == request.Mesa);
                if (sesionMesa != null)
                {
                    sesionMesa.PedidoActivo = true;
                    _context.SesionesMesas.Update(sesionMesa);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { exito = true, mensaje = "Orden guardada correctamente." });
            }
            catch (Exception) // CS0168: Variable 'ex' removida para limpiar advertencias
            {
                await transaction.RollbackAsync();
                return Json(new { exito = false, mensaje = "Error interno al procesar la orden." });
            }
        }
    }

    // --- CLASES AUXILIARES PARA RECIBIR EL JSON DEL CELULAR ---

    public class PedidoRequest
    {
        public int Mesa { get; set; }
        public string? MetodoPago { get; set; }
        public List<DetalleRequest>? Detalles { get; set; }
    }

    public class DetalleRequest
    {
        public int PlatilloId { get; set; }
        public int? VarianteId { get; set; }
        public string? VarianteNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}