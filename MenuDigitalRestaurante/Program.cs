using Microsoft.EntityFrameworkCore;
using MenuDigitalRestaurante.Data; // Verifica que el namespace coincida con tu proyecto

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar el DbContext con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Servicios MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configuración del pipeline...
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
