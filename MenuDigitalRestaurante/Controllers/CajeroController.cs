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

        // Pantalla principal del cajero: Muestra las órdenes activas/pendientes
        public async Task<IActionResult> Index()
        {
            var pedidosActivos = await _context.Pedidos
                .Include(p => p.Mesa)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Platillo)
                .Where(p => p.EstadoPedido != "Pagado")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            return View(pedidosActivos);
        }

        // Acción para procesar el pago y liberar la mesa
        [HttpPost]
        public async Task<IActionResult> Cobrar(int pedidoId, string metodoPago)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
                if (pedido == null) return NotFound();

                pedido.EstadoPedido = "Pagado";
                pedido.MetodoPago = string.IsNullOrEmpty(metodoPago) ? "Efectivo" : metodoPago;

                var sesionActiva = await _context.SesionesMesas
                    .FirstOrDefaultAsync(s => s.MesaId == pedido.MesaId && s.PedidoActivo);

                if (sesionActiva != null)
                {
                    sesionActiva.PedidoActivo = false; 
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
        }

        // Vista de Ticket Digital para impresión o lectura rápida
        public async Task<IActionResult> Ticket(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Mesa)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Platillo)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }
    }
}