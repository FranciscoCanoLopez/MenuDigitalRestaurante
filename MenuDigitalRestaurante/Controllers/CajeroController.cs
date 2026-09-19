using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MenuDigitalRestaurante.Data;
using MenuDigitalRestaurante.Models;

namespace MenuDigitalRestaurante.Controllers
{
    public class CajeroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CajeroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Pantalla principal del cajero (Carga la estructura base y el JS hace el resto)
        public IActionResult Index()
        {
            return View();
        }

        // 2. MÉTODO AJAX: Obtiene las mesas y pedidos en vivo cada 5 segundos
        [HttpGet]
        public async Task<IActionResult> ObtenerEstadoMesas()
        {
            // Traemos todas las mesas
            var mesas = await _context.Mesas.OrderBy(m => m.NumeroMesa).ToListAsync();
            
            // Traemos los pedidos activos 
            var pedidosActivos = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Platillo) 
                .Where(p => p.EstadoPedido != "Pagado" && p.EstadoPedido != "Cancelado")
                .ToListAsync();

            // Formateamos los datos para enviarlos al JavaScript del Panel
            var datos = mesas.Select(mesa => {
                var pedido = pedidosActivos.FirstOrDefault(p => p.MesaId == mesa.Id);
                
                return new {
                    mesaId = mesa.Id,
                    numeroMesa = mesa.NumeroMesa,
                    ocupada = pedido != null,
                    pedido = pedido == null ? null : new {
                        id = pedido.Id,
                        folio = "FOL-" + pedido.Id.ToString("D5"),
                        hora = pedido.FechaPedido.ToLocalTime().ToString("hh:mm tt"),
                        estado = pedido.EstadoPedido,
                        tipoAlerta = pedido.TipoAlerta,
                        metodoPago = pedido.MetodoPago,
                        total = pedido.Total,
                        detalles = pedido.Detalles.Select(d => new {
                            cantidad = d.Cantidad,
                            platillo = d.Platillo?.Nombre ?? "Producto Eliminado",
                            variante = d.NotasEspeciales,
                            precio = d.PrecioUnitario,
                            subtotal = d.Cantidad * d.PrecioUnitario
                        }).ToList()
                    }
                };
            });

            return Json(datos);
        }

        // 3. MÉTODO AJAX: Procesar el pago y liberar la mesa sin recargar la página
        [HttpPost]
        public async Task<IActionResult> CobrarPedido(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido != null)
            {
                // Actualizar estado del pedido
                pedido.EstadoPedido = "Pagado";
                pedido.TipoAlerta = "Ninguna"; 
                
                // Liberar la mesa de forma segura (sin tocar fechas)
                var sesionActiva = await _context.SesionesMesas
                    .FirstOrDefaultAsync(s => s.MesaId == pedido.MesaId && s.PedidoActivo);

                if (sesionActiva != null)
                {
                    sesionActiva.PedidoActivo = false;
                    // Le decimos a EF Core que SOLO actualice esta propiedad, ignorando las fechas
                    _context.Entry(sesionActiva).Property(s => s.PedidoActivo).IsModified = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { exito = true });
            }
            return Json(new { exito = false });
        }

        // 4. MÉTODO AJAX: Apagar una alerta de color (cuando el mesero ya atendió)
        [HttpPost]
        public async Task<IActionResult> AtenderAlerta(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido != null)
            {
                pedido.TipoAlerta = "Ninguna";
                await _context.SaveChangesAsync();
                return Json(new { exito = true });
            }
            return Json(new { exito = false });
        }

        // 5. Vista de Ticket Digital para impresión (Mantenemos tu método intacto)
        public async Task<IActionResult> Ticket(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Mesa)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Platillo)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }
    }
}