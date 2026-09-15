using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MenuDigitalRestaurante.Data;

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
                .Include(c => c.Platillos.Where(p => p.Disponible))
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
    }
}