using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SASI.Aplicacion.Servicios;
using SASI.Authorization;
using SASI.Caching;
using SASI.Configuration;
using SASI.Dominio.Repositories;
using SASI.Filters;
using SASI.Infraestructura.Identity;
using SASI.Infraestructura.Repositories;
using SASI.Logging;
using SASI.Middleware;
using SASI.Servicios;
using Serilog;
using SistemaConvocatorias.Infraestructura.Datos;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog antes de construir la app
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "Logs"));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Destructure.With<PiiDestructuringPolicy>()
    .Enrich.With<PiiLogEventEnricher>()
    .CreateLogger();

// Nombre de la política
var CorsPolicyName = "AllowLocalDev";

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://127.0.0.1:8001", "http://localhost:8001", "http://127.0.0.1:8002", "http://localhost:8002", "http://localhost:4200", "https://localhost:44320", "https://localhost:3000" };

// 1) Registrar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsPolicyName, policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Host.UseSerilog(); // Usar Serilog como logger

builder.Services.AddDbContext<SasiDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b =>
        {
            b.MigrationsAssembly("SASI");
            b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        }));

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b =>
        {
            b.MigrationsAssembly("SASI");
            b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        }));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, SasiUserClaimsPrincipalFactory>();

builder.Services.AddTransient<ISistemaRepository, SistemaRepository>();
builder.Services.AddTransient<IRolRepository, RolRepository>();
builder.Services.AddTransient<IObjetoRepository, ObjetoRepository>();
builder.Services.AddTransient<IRolObjetoRepository, RolObjetoRepository>();
builder.Services.AddTransient<IUsuarioSistemaRepository, UsuarioSistemaRepository>();
builder.Services.AddTransient<ICorrelativoRepository, CorrelativoRepository>();
builder.Services.AddTransient<IOficinaRepository, OficinaRepository>();

builder.Services.AddScoped<ISistemaServicio, SistemaServicio>();
builder.Services.AddScoped<IRolServicio, RolServicio>();
builder.Services.AddScoped<IOficinaServicio, OficinaServicio>();
builder.Services.AddScoped<IObjetoServicio, ObjetoServicio>();
builder.Services.AddScoped<IUsuarioSistemaServicio, UsuarioSistemaServicio>();

builder.Services.AddScoped<CuentaServicio>();
builder.Services.AddScoped<AutenticacionServicio>();
builder.Services.AddScoped<GestionUsuariosServicio>();

/*builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\claveproteccion"))
    .SetApplicationName("SASI");*/

var dataProtectionPath = builder.Configuration["SasiDataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("SASI");

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".SASI.Auth";
    options.Cookie.Path = "/SASI";
    options.LoginPath = "/SASI/Cuenta/Login";
    options.LogoutPath = "/SASI/Cuenta/Logout";
    options.AccessDeniedPath = "/SASI/Cuenta/AccesoDenegado";

    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Agregar eventos para corregir rutas
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            // Redirigir directamente al root (/SASI) sin ReturnUrl
            context.Response.Redirect("/SASI");
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/SASI/Cuenta/AccesoDenegado");
            return Task.CompletedTask;
        }
    };
});

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key no está configurado o no tiene al menos 256 bits. " +
        "Configurelo mediante la variable de entorno Jwt__Key (o user-secrets) antes de iniciar la aplicación.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Jwt:Issuer y Jwt:Audience deben estar configurados.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddScoped<IAuthorizationHandler, AccesoModuloHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AccesoModulo", policy => policy.Requirements.Add(new AccesoModuloRequirement()));
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AutoValidateAntiforgeryFilter>();
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = ".SASI.Antiforgery";
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var cacheProvider = builder.Configuration["Cache:Provider"] ?? "Memory";

if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IDistributedCache>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<ResilientDistributedCache>>();
        var primario = new RedisCache(Options.Create(new RedisCacheOptions
        {
            Configuration = builder.Configuration["Cache:Redis:Configuration"]
                ?? builder.Configuration.GetConnectionString("Redis"),
            InstanceName = "SASI"
        }));
        var respaldo = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new ResilientDistributedCache(primario, respaldo, logger);
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddEndpointsApiExplorer();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SASI API",
            Version = "v1",
            Description = "API del sistema SASI. Para autenticarse: obtenga un token en POST /api/auth/login y haga clic en Authorize."
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingrese el token JWT. Ejemplo: eyJhbGciOiJIUzI1NiIs..."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

builder.Services.Configure<ConfiguracionSistemaSASI>(
    builder.Configuration.GetSection("SistemaSASI"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var sasiDbContext = scope.ServiceProvider.GetRequiredService<SasiDbContext>();
    await sasiDbContext.Database.MigrateAsync();

    var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await identityDbContext.Database.MigrateAsync();
}

//using (var scope = app.Services.CreateScope())
//{
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

//    var user = new ApplicationUser
//    {
//        UserName = "admin@correo.com",
//        Email = "admin@correo.com",
//        NombreCompleto = "Administrador",
//        AuditUsuarioCreacion = "system",
//        IpCreacion = "192.168.18.4",
//        AuditFechaCreacion = DateTime.UtcNow,
//    };

//    var result = await userManager.CreateAsync(user, "Admin123!"); // Usa una contraseña segura

//    if (result.Succeeded)
//    {
//        // Usuario creado
//    }
//}

//if (!app.Environment.IsDevelopment())
//{
//app.UseExceptionHandler("/Error/500");
//app.UseStatusCodePagesWithReExecute("/Error/{0}");
//}

app.UsePathBase("/SASI");

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionProblemDetailsMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 2) Usar CORS antes de UseAuthorization/UseEndpoints
app.UseCors(CorsPolicyName);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cuenta}/{action=Login}/{id?}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}/SASI" }
            };
        });
    });

    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/SASI/swagger/v1/swagger.json", "SASI API V1");
        c.RoutePrefix = "swagger";
    });
}

app.Run();