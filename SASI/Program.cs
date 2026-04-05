using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SASI.Configuration;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using SASI.Infraestructura.Repositories;
using Serilog;
using SistemaConvocatorias.Infraestructura.Datos;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog antes de construir la app
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Nombre de la política
var CorsPolicyName = "AllowLocalDev";

// 1) Registrar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsPolicyName, policy =>
    {
        // En desarrollo: permitir sólo el origen de tu frontend
        policy.WithOrigins("http://127.0.0.1:8001", "http://localhost:8001", "http://127.0.0.1:8002", "http://localhost:8002", "http://localhost:4200", "https://localhost:44320", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              // .AllowCredentials() // habilitar solo si usas cookies/autenticación por cookie
              ;
    });
});

builder.Host.UseSerilog(); // Usar Serilog como logger

builder.Services.AddDbContext<SasiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("SASI")));

//builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ISasiService, SasiService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddTransient<ISistemaRepository, SistemaRepository>();
builder.Services.AddTransient<IRolRepository, RolRepository>();
builder.Services.AddTransient<IObjetoRepository, ObjetoRepository>();
builder.Services.AddTransient<IRolObjetoRepository, RolObjetoRepository>();
builder.Services.AddTransient<IUsuarioSistemaRepository, UsuarioSistemaRepository>();
builder.Services.AddTransient<ICorrelativoRepository, CorrelativoRepository>();
builder.Services.AddTransient<IOficinaRepository, OficinaRepository>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\claveproteccion"))
    .SetApplicationName("SASI");

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;
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
    //options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = false;
    options.Cookie.MaxAge = null;

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };
    });

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    //options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ConfiguracionSistemaSASI>(
    builder.Configuration.GetSection("SistemaSASI"));

var app = builder.Build();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

app.Run();