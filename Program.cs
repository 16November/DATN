using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Repository.Repositories;
using DoAnTotNghiep.Service.Service;
using DoAnTotNghiep.Services.IService;
using DoAnTotNghiep.Services.Service;
using DoAnTotNghiep.Support;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Net.WebSockets;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using DoAnTotNghiep.Services.ServiceImplement;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IFFmpegService, FFmpegService>();
builder.Services.AddSingleton<ITeacherStreamService, TeacherStreamService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 512 * 1024 * 1024; // 512MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DataContext")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:7128")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    context.Token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? authHeader.Substring("Bearer ".Length).Trim()
                        : authHeader;
                }
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddIdentityCore<User>()
    .AddRoles<Role>()
    .AddTokenProvider<DataProtectorTokenProvider<User>>("DataContext")
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.AllowedForNewUsers = true;
});

builder.Services.AddScoped<IUserInfoRepository, UserInfoRepository>();
builder.Services.AddScoped<IUserExamRepository, UserExamRepository>();
builder.Services.AddScoped<IUserAnswerRepository, UserAnswerRepository>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
builder.Services.AddScoped<ICheatingEventRepository, CheatingRepository>();

builder.Services.AddScoped<IUserAnswerService, UserAnswerService>();
builder.Services.AddScoped<IUserInfoService, UserInfoService>();
builder.Services.AddScoped<IUserExamService, UserExamService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICheatingService, CheatingService>();


builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(10);
});

builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

var app = builder.Build();

// Tạo thư mục wwwroot/live nếu chưa có
var wwwrootLiveFolder = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "live");
if (!Directory.Exists(wwwrootLiveFolder))
{
    Directory.CreateDirectory(wwwrootLiveFolder);
}

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
provider.Mappings[".ts"] = "video/MP2T";

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom middleware để thêm CORS header cho static file trong /live
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/live"))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:5173";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Origin, X-Requested-With, Content-Type, Accept";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            return;
        }
    }
    await next();
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseRouting();

app.UseStaticFiles(); // Phục vụ wwwroot

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootLiveFolder),
    RequestPath = "/live",
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        ctx.Context.Response.Headers["Pragma"] = "no-cache";
        ctx.Context.Response.Headers["Expires"] = "0";

        // Lặp lại CORS header ở đây để đảm bảo nếu middleware trên không chạy
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:5173";
        ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
    }
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/student") && context.Request.Path.Value.EndsWith("/ws"))
    {
        try
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket request expected.");
                return;
            }

            var pathSegments = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length < 3)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid WebSocket path format");
                return;
            }

            var streamIdStr = pathSegments[2];
            if (!Guid.TryParse(streamIdStr, out var streamId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid streamId format");
                return;
            }

            var teacherStreamService = context.RequestServices.GetRequiredService<ITeacherStreamService>();

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            try
            {
                await teacherStreamService.HandleStudentStreamDataAsync(streamId, webSocket);
            }
            catch (Exception streamEx)
            {
                Console.WriteLine($"Error handling stream data for {streamId}: {streamEx.Message}");
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Stream processing error", CancellationToken.None);
                }
            }

            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in WebSocket middleware: {ex.Message}");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error");
            }
            return;
        }
    }

    await next();
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<NotificationHub>("/notificationHub");
    endpoints.MapControllers();
});

app.MapControllers();

app.Run();
