using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MenuDigitalRestaurante.Data;
using MenuDigitalRestaurante.Models;

namespace MenuDigitalRestaurante.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- GESTIÓN DE PLATILLOS ---

        // Vista principal del administrador: Lista de Platillos
        public async Task<IActionResult> Index()
        {
            var platillos = await _context.Platillos
                .Include(p => p.Categoria)
                .Where(p => p.Activo == true) // Solo trae los que no han sido borrados
                .OrderBy(p => p.CategoriaId)
                .ToListAsync();

            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View(platillos);
        }

        // Crear o Editar Platillo (POST)
        [HttpPost]
        public async Task<IActionResult> GuardarPlatillo(Platillo platillo)
        {
            if (platillo.Id == 0)
            {
                // Nuevo Platillo
                _context.Platillos.Add(platillo);
            }
            else
            {
                // Editar Platillo existente
                _context.Platillos.Update(platillo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Alternar Disponibilidad (Activar/Desactivar platillo en tiempo real)
        [HttpPost]
        public async Task<IActionResult> CambiarDisponibilidad(int id)
        {
            var platillo = await _context.Platillos.FindAsync(id);
            if (platillo != null)
            {
                platillo.Disponible = !platillo.Disponible;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Eliminar Platillo (borrado lógico, no físico)
        [HttpPost]
        public async Task<IActionResult> EliminarPlatillo(int id)
        {
            var platillo = await _context.Platillos.FindAsync(id);
            if (platillo != null)
            {
                // En lugar de _context.Platillos.Remove(platillo); hacemos esto:
                platillo.Activo = false; 
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Platillo eliminado (descontinuado) correctamente del menú.";
            }
            return RedirectToAction(nameof(Index));
        }

        // --- GESTIÓN DE CATEGORÍAS ---

        // Vista para gestionar categorías
        public async Task<IActionResult> Categorias()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Activo == true)
                .ToListAsync();
                
            return View(categorias);
        }

        // Guardar o Actualizar Categoría
        [HttpPost]
        public async Task<IActionResult> GuardarCategoria(Categoria categoria)
        {
            if (categoria.Id == 0)
            {
                _context.Categorias.Add(categoria);
            }
            else
            {
                _context.Categorias.Update(categoria);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categorias));
        }

        // Eliminar Categoría
        [HttpPost]
        public async Task<IActionResult> EliminarCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria != null)
            {
                // En lugar de Remove, cambiamos el estado
                categoria.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Categoría eliminada correctamente.";
            }
            return RedirectToAction(nameof(Categorias));
        }
    }
}