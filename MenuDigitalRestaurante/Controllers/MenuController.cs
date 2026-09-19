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
                return Content("Error: La mesa indicada no existe. Por favor escanea el código QR nuevamente.");
            }

            // --- SEGURIDAD POR COOKIES (TOKEN DE DISPOSITIVO) ---
            string? tokenCelular = Request.Cookies["TokenRestaurante"];
            SesionMesa? sesionDispositivo = null!;

            if (!string.IsNullOrEmpty(tokenCelular))
            {
                sesionDispositivo = await _context.SesionesMesas.FirstOrDefaultAsync(s => s.TokenSesion == tokenCelular);
            }

            // REGLA 1: Si este celular ya hizo un pedido, bloquearlo en su pantalla de proceso (Evita que navegue a otras mesas)
            if (sesionDispositivo != null && sesionDispositivo.PedidoActivo)
            {
                var mesaAsignada = await _context.Mesas.FindAsync(sesionDispositivo.MesaId);
                return RedirectToAction("PedidoEnProceso", new { mesa = mesaAsignada!.NumeroMesa });
            }

            // REGLA 2: Verificar si la mesa que intenta abrir está ocupada por OTRO celular
            var sesionMesaOcupada = await _context.SesionesMesas
                .FirstOrDefaultAsync(s => s.MesaId == mesaDb.Id && s.PedidoActivo == true);

            if (sesionMesaOcupada != null)
            {
                // Si la mesa tiene pedido activo, y el token de la mesa NO es el de este celular -> Acceso Denegado
                if (sesionDispositivo == null || sesionDispositivo.TokenSesion != sesionMesaOcupada.TokenSesion)
                {
                    return Content("❌ ACCESO DENEGADO: Esta mesa ya está ocupada por otro dispositivo y tiene un pedido en curso. Si es un error, contacte a un mesero.");
                }
            }

            // REGLA 3: Generar un Token Nuevo si es un cliente nuevo (o si su sesión anterior ya se cobró)
            if (sesionDispositivo == null || sesionDispositivo.MesaId != mesaDb.Id)
            {
                string nuevoToken = Guid.NewGuid().ToString();

                // Limpiamos la base de datos borrando sesiones viejas inactivas de esta mesa
                var sesionesViejas = _context.SesionesMesas.Where(s => s.MesaId == mesaDb.Id && s.PedidoActivo == false);
                _context.SesionesMesas.RemoveRange(sesionesViejas);

                var nuevaSesion = new SesionMesa
                {
                    MesaId = mesaDb.Id,
                    TokenSesion = nuevoToken,
                    FechaCreacion = DateTime.UtcNow,
                    PedidoActivo = false
                };
                
                _context.SesionesMesas.Add(nuevaSesion);
                await _context.SaveChangesAsync();

                // Guardar la Cookie en el celular (Expira en 4 horas automáticamente)
                Response.Cookies.Append("TokenRestaurante", nuevoToken, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(4),
                    HttpOnly = true,
                    IsEssential = true
                });
            }
            // ---------------------------------------------------

            // Si pasó todas las validaciones, cargamos el menú normal
            var categoriasConPlatillos = await _context.Categorias
                .Where(c => c.Activo == true)
                .OrderBy(c => c.Orden)
                .Include(c => c.Platillos.Where(p => p.Disponible == true && p.Activo == true))
                    .ThenInclude(p => p.Variantes.Where(v => v.Activo == true))
                .ToListAsync();

            ViewBag.NumeroMesa = mesaDb.NumeroMesa;
            ViewBag.MesaId = mesaDb.Id;

            return View(categoriasConPlatillos);
        }

        // Pantalla de bloqueo seguro
        public async Task<IActionResult> PedidoEnProceso(int mesa)
        {
            // Validar que realmente tenga permiso de estar en esta pantalla
            string? tokenCelular = Request.Cookies["TokenRestaurante"];
            if (string.IsNullOrEmpty(tokenCelular)) return RedirectToAction("Index", new { mesa = mesa });

            var sesionDispositivo = await _context.SesionesMesas.FirstOrDefaultAsync(s => s.TokenSesion == tokenCelular);
            if (sesionDispositivo == null || !sesionDispositivo.PedidoActivo) 
            {
                return RedirectToAction("Index", new { mesa = mesa });
            }

            // Tomamos la mesa real de la base de datos (por si el usuario alteró el número en la URL)
            var mesaReal = await _context.Mesas.FindAsync(sesionDispositivo.MesaId);
            ViewBag.NumeroMesa = mesaReal!.NumeroMesa;

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

            // --- NUEVO CANDADO DE SEGURIDAD ---
            // Verificamos si la mesa ya tiene un pedido activo
            var sesionExistente = await _context.SesionesMesas
                .FirstOrDefaultAsync(s => s.MesaId == request.Mesa && s.PedidoActivo == true);

            if (sesionExistente != null)
            {
                return Json(new { exito = false, mensaje = "Esta mesa ya tiene una orden en proceso. Refresca la página." });
            }
            // ----------------------------------

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Crear la cabecera del Pedido
                var nuevoPedido = new Pedido
                {
                    MesaId = request.Mesa, 
                    FechaPedido = DateTime.UtcNow,
                    MetodoPago = request.MetodoPago ?? "Efectivo",
                    Total = request.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario)
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
                        NotasEspeciales = item.VarianteNombre
                    };
                    
                    _context.DetallePedidos.Add(detalle);
                }

                // 3. Actualizar la sesión de la mesa de forma segura
                var sesionMesa = await _context.SesionesMesas.FirstOrDefaultAsync(s => s.MesaId == request.Mesa);
                if (sesionMesa != null)
                {
                    sesionMesa.PedidoActivo = true;
                    _context.Entry(sesionMesa).Property(s => s.PedidoActivo).IsModified = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { exito = true, mensaje = "Orden guardada correctamente." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Json(new { exito = false, mensaje = "Error interno al procesar la orden." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> LlamarMesero([FromBody] AlertaRequest request)
        {
            // Buscamos el pedido más reciente de esa mesa que siga "Pendiente" o "En Proceso"
            var pedidoMesa = await _context.Pedidos
                .Where(p => p.MesaId == request.Mesa && p.EstadoPedido != "Completado" && p.EstadoPedido != "Pagado")
                .OrderByDescending(p => p.FechaPedido)
                .FirstOrDefaultAsync();

            if (pedidoMesa != null)
            {
                // Actualizamos el tipo de alerta (Pago, Mesero, Queja)
                pedidoMesa.TipoAlerta = request.TipoAlerta!;
                
                // Le decimos a EF Core que solo modifique esta columna
                _context.Entry(pedidoMesa).Property(p => p.TipoAlerta).IsModified = true;
                await _context.SaveChangesAsync();
                
                return Json(new { exito = true });
            }

            return Json(new { exito = false });
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
    public class AlertaRequest
    {
        public int Mesa { get; set; }
        public string? TipoAlerta { get; set; }
    }
}