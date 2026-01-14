using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tringelty.Core.Entities;
using Tringelty.Infrastructure.Data;
using Microsoft.OpenApi.Models;
using Tringelty.Core.Interfaces;
using Tringelty.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using Tringelty.Api.Data;
using Tringelty.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
// --- Services Configuration ---

// Registers controllers to handle API requests
builder.Services.AddControllers();


// --- 1. Infrastructure Layer ---
builder.Services.AddInfrastructureServices(builder.Configuration);

// Swagger/OpenAPI configuration for API documentation and testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tringelty API", Version = "v1" });

    // 1. Описываем схему авторизации (Bearer JWT)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // 2. Добавляем требование безопасности ко всем методам
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Database Context configuration
// We use Npgsql provider for PostgreSQL. Connection string is loaded from appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 🛠 ВРЕМЕННЫЙ DEBUG ЛОГ (УДАЛИТЬ ПОСЛЕ ИСПРАВЛЕНИЯ) 🛠 ---
Console.WriteLine("=================================================");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("🚨 ОШИБКА: Connection String == NULL или пустая!");
}
else
{
    // Выводим строку, чтобы увидеть, не попал ли туда мусор типа "${{...}}"
    Console.WriteLine($"✅ Connection String: '{connectionString}'");
}
Console.WriteLine("=================================================");
// -------------------------------------------------------------

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity Registration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
    {
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT CONFIGURATION
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            
        };
    });


// CORS Policy Configuration
// NOTE: 'AllowAll' policy is acceptable for development (MVP).
// TODO: For production, replace .AllowAnyOrigin() with specific frontend domains to prevent security risks.

// 1. Получаем список разрешенных доменов из конфига
var corsOrigins = builder.Configuration.GetSection("AppSettings:CorsOrigins").Get<string[]>() 
                  ?? new[] { "http://localhost:4200" }; // Фолбек на всякий случай

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(corsOrigins) // <-- Сюда подставится массив
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Если используешь Cookie/Auth
    });
});

var app = builder.Build();

// --- HTTP Request Pipeline ---

// Enable Swagger UI only in Development environment to avoid exposing API structure in Prod
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication(); 
app.UseAuthorization();




app.MapControllers();

// === ЗАПУСК SEEDER ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        await DbSeeder.SeedRolesAndAdminAsync(services);
        
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // 1. Применяем миграции (если есть новые)
        await context.Database.MigrateAsync();
        
        // 2. ЗАПУСКАЕМ НАШ ФИКСЕР ФЛАГОВ
        await DbSeeder.FixWorkerFlagsAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
