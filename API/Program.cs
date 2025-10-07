using System.Reflection;
using System.Text;
using API.BookingService;
using API.Data;
using API.Repositories;
using API.Services;
using API.Services.Mail;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDBContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")
             ?? Environment.GetEnvironmentVariable("DATABASE_URL")
             ?? throw new InvalidOperationException("DefaultConnection missing.");

    options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure());
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddControllers();

builder.Services.Configure<LdapOptions>(builder.Configuration.GetSection("Ldap"));
builder.Services.AddSingleton<ILdapService, LdapService>();

// JWT
var jwtKey = builder.Configuration["Jwt:SecretKey"]
             ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "H2-2025-API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "H2-2025-Client";
var key = Encoding.UTF8.GetBytes(jwtKey);

// SignalR
builder.Services.AddSignalR();

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", p => p.RequireRole("Admin", "Manager"));
});

builder.Services.AddScoped<ITicketService, TicketService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; 
        options.SaveToken = true;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/api/hubs/tickets"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevAll", p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel API",
        Version = "v1",
        Description = "API til hotel booking system (JWT, EF Core, CORS)."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    c.CustomSchemaIds(t => t.FullName?.Replace("+", "."));

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Skriv 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
});

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<JwtService>();

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IMailService, MailService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("DevAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<TicketHub>("/api/hubs/tickets");
app.MapControllers();

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
