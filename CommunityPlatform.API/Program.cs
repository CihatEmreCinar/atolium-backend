using System.Text;
using Microsoft.AspNetCore.Diagnostics;
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
using Minio;

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
builder.Services.AddScoped<ITicketSigningService, TicketSigningService>();
builder.Services.AddHttpClient(); // ReminderDispatchJob → Expo Push API
// ─── Sosyal Feed Servisleri ───────────────────────────────────────────────────
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<SocialService>();
builder.Services.AddScoped<FeedService>();
builder.Services.AddScoped<TagService>();

// ─── Media (unified pipeline) — MinIO ─────────────────────────────────────────
builder.Services.AddSingleton<IMinioClient>(_ =>
    new MinioClient()
        .WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "localhost:9000")
        .WithCredentials(
            builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
            builder.Configuration["Minio:SecretKey"] ?? "minioadmin")
        .WithSSL(bool.Parse(builder.Configuration["Minio:UseSSL"] ?? "false"))
        .Build());
builder.Services.AddScoped<IMediaObjectStore, MinioMediaObjectStore>();

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

    // QR verify/check-in — brute-force ticketId taramasına karşı. IP yerine authenticated
    // employer'ın UserId'sine göre partition'lanır (bu endpoint zaten [Authorize] arkasında,
    // IP partition'ı NAT arkasındaki birden fazla employer'ı yanlışlıkla birbirine karıştırır).
    options.AddPolicy("ticket-verify", httpContext =>
    {
        var employerId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anon";

        return RateLimitPartition.GetFixedWindowLimiter(employerId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

var app = builder.Build();

// İl/ilçe seed'i — tablo boşsa bir kereliğine yüklenir, sonraki başlatmalarda no-op.
using (var seedScope = app.Services.CreateScope())
{
    var db = seedScope.ServiceProvider.GetRequiredService<CommunityPlatform.Infrastructure.Persistence.AppDbContext>();
    await CommunityPlatform.Infrastructure.Persistence.SeedData.LocationSeeder.SeedAsync(db);
}

// MinIO bucket yoksa oluştur — worker de aynısını yapar, ikisi de idempotent.
using (var mediaScope = app.Services.CreateScope())
{
    var mediaStore = mediaScope.ServiceProvider.GetRequiredService<IMediaObjectStore>();
    await mediaStore.EnsureBucketExistsAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ─── Global Exception Handling ───────────────────────────────────────────────
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = exception switch
    {
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };

    var isServerError = context.Response.StatusCode == StatusCodes.Status500InternalServerError;
    var message = isServerError ? "Beklenmeyen bir hata oluştu." : (exception?.Message ?? "Bir hata oluştu.");

    if (isServerError)
        app.Logger.LogError(exception, "Yakalanmamış hata: {Path}", context.Request.Path);

    await context.Response.WriteAsJsonAsync(new { message });
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// ─── Güvenlik header'ları ─────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
    await next();
});

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