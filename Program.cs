using bbbAPIGL.Repositories;
using bbbAPIGL.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar servicios al contenedor (Inyección de Dependencias)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Registrar nuestros servicios personalizados ---
// Scoped: se crea una nueva instancia para cada petición HTTP.
builder.Services.AddScoped<ISalaService, SalaService>();
builder.Services.AddScoped<ISalaEmpresaService, SalaEmpresaService>();
builder.Services.AddScoped<ISalaRepository, SalaRepository>();
builder.Services.AddScoped<ICursoRepository, MySqlCursoRepository>();
builder.Services.AddScoped<ICursoEmpresaRepository, MySqlCursoEmpresaRepository>();
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();
builder.Services.AddScoped<IAcademicCalendarService, AcademicCalendarService>();
builder.Services.AddTransient<IEmailService, GmailService>();
// 2. Construir la aplicación
var app = builder.Build();

// 3. Configurar el pipeline de peticiones HTTP
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference();
}

// En producción, Nginx maneja HTTPS. HttpsRedirection puede causar problemas si no está bien configurado.
// app.UseHttpsRedirection();
app.MapControllers();

// 4. Ejecutar la aplicación
app.Run();