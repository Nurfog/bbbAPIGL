using bbbAPIGL.Repositories;
using bbbAPIGL.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar servicios al contenedor (Inyección de Dependencias)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Registrar nuestros servicios personalizados ---
// Scoped: se crea una nueva instancia para cada petición HTTP.
builder.Services.AddScoped<ISalaService, SalaService>();
builder.Services.AddScoped<ISalaRepository, SalaRepository>();
builder.Services.AddScoped<ICursoRepository, MySqlCursoRepository>();
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();
builder.Services.AddScoped<IAcademicCalendarService, AcademicCalendarService>();
builder.Services.AddTransient<IEmailService, GmailService>();
// 2. Construir la aplicación
var app = builder.Build();

// 3. Configurar el pipeline de peticiones HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

// 4. Ejecutar la aplicación
app.Run();