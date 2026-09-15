using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MenuDigitalRestaurante.Data;
using MenuDigitalRestaurante.Models;

namespace MenuDigitalRestaurante.Controllers
{
    public class PedidoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Crear(int mesaId, int numeroMesa, string metodoPago, Dictionary<int, int> cantidades, Dictionary<int, string> notas)
        {
            // 1. Validar que el usuario haya seleccionado al menos un platillo (cantidad > 0)
            var itemsSeleccionados = cantidades.Where(c => c.Value > 0).ToList();
            if (!itemsSeleccionados.Any())
            {
                TempData["Error"] = "Debes seleccionar al menos un platillo para realizar tu pedido.";
                return RedirectToAction("Index", "Menu", new { mesa = numeroMesa });
            }

            // 2. Doble validación de seguridad: Verificar que la mesa no tenga ya un pedido activo
            var sesionExistente = await _context.SesionesMesas
                .FirstOrDefaultAsync(s => s.MesaId == mesaId && s.PedidoActivo == true);

            if (sesionExistente != null)
            {
                return RedirectToAction("PedidoEnProceso", "Menu", new { mesa = numeroMesa });
            }

            // Usamos una transacción de base de datos para garantizar que todo se guarde correctamente o nada lo haga
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. Crear la cabecera del Pedido
                var pedido = new Pedido
                {
                    MesaId = mesaId,
                    MetodoPago = metodoPago ?? "Efectivo",
                    EstadoPedido = "Pendiente",
                    Total = 0, // Se calculará sumando los platillos
                    FechaPedido = DateTime.UtcNow
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync(); // Guardamos para obtener el ID generado del pedido

                decimal totalCalculado = 0;
                var detalles = new List<DetallePedido>();

                // 4. Recorrer los platillos seleccionados y consultar sus precios reales en la base de datos
                foreach (var item in itemsSeleccionados)
                {
                    int platilloId = item.Key;
                    int cantidad = item.Value;

                    var platilloDb = await _context.Platillos.FindAsync(platilloId);
                    if (platilloDb != null && platilloDb.Disponible)
                    {
                        decimal subtotal = platilloDb.Precio * cantidad;
                        totalCalculado += subtotal;

                        // Obtener notas especiales si el usuario escribió alguna para este platillo
                        string notaEspecial = string.Empty;
                        if (notas != null && notas.ContainsKey(platilloId))
                        {
                            notaEspecial = notas[platilloId];
                        }

                        detalles.Add(new DetallePedido
                        {
                            PedidoId = pedido.Id,
                            PlatilloId = platilloId,
                            Cantidad = cantidad,
                            PrecioUnitario = platilloDb.Precio,
                            NotasEspeciales = notaEspecial
                        });
                    }
                }

                // Actualizar el total real en la cabecera del pedido
                pedido.Total = totalCalculado;
                _context.DetallePedidos.AddRange(detalles);

                // 5. Crear la sesión activa para bloquear el menú de esta mesa
                var nuevaSesion = new SesionMesa
                {
                    MesaId = mesaId,
                    TokenSesion = Guid.NewGuid().ToString(),
                    PedidoActivo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                _context.SesionesMesas.Add(nuevaSesion);
                await _context.SaveChangesAsync();

                // Confirmar la transacción en la base de datos
                await transaction.CommitAsync();

                // 6. Redirigir a la pantalla de pedido en proceso
                return RedirectToAction("PedidoEnProceso", "Menu", new { mesa = numeroMesa });
            }
            catch (Exception)
            {
                // Si algo falla, revertimos toda la operación para no dejar datos corruptos
                await transaction.RollbackAsync();
                TempData["Error"] = "Ocurrió un error al procesar tu pedido. Por favor, inténtalo de nuevo.";
                return RedirectToAction("Index", "Menu", new { mesa = numeroMesa });
            }
        }
    }
}