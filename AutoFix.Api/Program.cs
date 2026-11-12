using System.Security.Claims;
using System.Text;
using AutoFix.Api.Data;
using AutoFix.Api.Models;
using AutoFix.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AutoFixDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ImagenStorageService>();
builder.Services.AddScoped<InventarioService>();

var jwtKey = JwtService.ObtenerClave(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AutoFix.Api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AutoFix.Mvc",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PuedeCrearOrden", policy =>
        policy.RequireRole(RolesSistema.Administrador, RolesSistema.Recepcionista)
              .RequireClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.OrdenesCrear));

    options.AddPolicy("PuedeEditarOrden", policy =>
        policy.RequireRole(RolesSistema.Administrador, RolesSistema.Recepcionista, RolesSistema.Mecanico)
              .RequireClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.OrdenesEditar));

    options.AddPolicy("PuedeAsignar", policy =>
        policy.RequireRole(RolesSistema.Supervisor, RolesSistema.Administrador)
              .RequireClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.OrdenesAsignar));

    options.AddPolicy("PuedeAprobar", policy =>
        policy.RequireRole(RolesSistema.Supervisor, RolesSistema.Administrador)
              .RequireClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.OrdenesAprobar));

    options.AddPolicy("PuedeFinalizar", policy =>
        policy.RequireRole(RolesSistema.Supervisor, RolesSistema.Administrador)
              .RequireClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.OrdenesFinalizar));

    options.AddPolicy("PuedeGestionarInventario", policy =>
        policy.RequireRole(RolesSistema.Supervisor, RolesSistema.Administrador)
              .RequireClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.InventarioGestionar));

    options.AddPolicy("PuedeConsultarReportes", policy =>
        policy.RequireRole(RolesSistema.Supervisor, RolesSistema.Administrador)
              .RequireClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.ReportesConsultar));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AutoFix API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresa el token JWT así: Bearer {tu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AutoFixDbContext>();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
