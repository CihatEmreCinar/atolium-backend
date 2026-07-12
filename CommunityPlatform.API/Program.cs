using System.Text;
using RabbitMQ.Client;
using CommunityPlatform.API.Services;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure;
using CommunityPlatform.Infrastructure.Services;
using CommunityPlatform.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CommunityPlatform.Infrastructure.Storage;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// RabbitMQ — singleton, tek bağlantı tüm uygulama boyunca paylaşılır
builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
        Port     = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();
builder.Services.AddHttpClient(); // ReminderDispatchJob → Expo Push API
// ─── Sosyal Feed Servisleri ───────────────────────────────────────────────────
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<SocialService>();
builder.Services.AddScoped<FeedService>();
builder.Services.AddScoped<TagService>();

// ─── Background Jobs ──────────────────────────────────────────────────────────
builder.Services.AddHostedService<EngagementScoreJob>();
builder.Services.AddHostedService<ReminderDispatchJob>();

// ─── Auth ─────────────────────────────────────────────────────────────────────
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireCafeRole", p => p.RequireRole("cafe"));
    options.AddPolicy("RequireEmployerRole", p => p.RequireRole("employer"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ─── Static Files (uploads) ──────────────────────────────────────────────────
// LocalStorageProvider dosyaları {WebRootPath}/uploads/ altına yazar.
// WebRootPath = {ContentRootPath}/wwwroot, bu yüzden uploadsPath aşağıdaki gibi.
var uploadsPath = Path.Combine(builder.Environment.WebRootPath
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"), "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();