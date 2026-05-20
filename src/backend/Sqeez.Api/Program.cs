using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Sqeez.Api.Data;
using Sqeez.Api.Middlewares;
using Sqeez.Api.Models.Config;
using Sqeez.Api.Services;
using Sqeez.Api.Services.AuthService;
using Sqeez.Api.Services.EmailService;
using Sqeez.Api.Services.Interfaces;
using Sqeez.Api.Services.SubjectService;
using Sqeez.Api.Services.TokenService;
using Sqeez.Api.Services.UserService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var envFiles = new[] { ".env", ".env.local" }.Where(File.Exists).ToArray();
if (envFiles.Length > 0)
{
    DotNetEnv.Env.LoadMulti(envFiles);
}

builder.Configuration.AddEnvironmentVariables();

var tokenKey = builder.Configuration["TokenKey"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(tokenKey))
{
    throw new Exception("JWT TokenKey is missing! Check your .env or .env.local file.");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Database Connection String is missing from .env or .env.local!");
}

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        if (string.IsNullOrEmpty(operation.Description) && !string.IsNullOrEmpty(operation.Summary))
        {
            operation.Description = operation.Summary;
        }

        var method = context.Description.HttpMethod;
        var path = context.Description.RelativePath;
        
        // Ensure path starts with a slash
        var displayPath = path != null && !path.StartsWith("/") ? $"/{path}" : path;
        
        operation.Summary = $"{method} {displayPath}";
        return Task.CompletedTask;
    });
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<SqeezDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddMemoryCache();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISchoolClassService, SchoolClassService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IQuizQuestionService, QuizQuestionService>();
builder.Services.AddScoped<IQuizOptionService, QuizOptionService>();
builder.Services.AddScoped<IMediaAssetService, MediaAssetService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IQuizAttemptService, QuizAttemptService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IQuizStatisticsService, QuizStatisticsService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.UniqueName,

            // By default, .NET adds a 5-minute "grace period" to token expirations.
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("sqeez_access_token"))
                {
                    context.Token = context.Request.Cookies["sqeez_access_token"];
                }
                return Task.CompletedTask;
            }
        };
    });

var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();

// --- SEED SCRIPT ---
if (args.Length > 0 && (args[0].Equals("seed", StringComparison.OrdinalIgnoreCase) || args[0].Equals("seed-demo", StringComparison.OrdinalIgnoreCase)))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<SqeezDbContext>();
            var config = services.GetRequiredService<IConfiguration>();

            Console.WriteLine("Running explicitly requested DB Seed...");
            if (args[0].Equals("seed-demo", StringComparison.OrdinalIgnoreCase))
            {
                await DatabaseSeeder.SeedDemoAsync(context, config);
            }
            else
            {
                await DatabaseSeeder.SeedAsync(context, config);
            }
            Console.WriteLine("Database seeding complete!");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
            Environment.Exit(1);
        }
    }
    return; // Exit after seeding
}
// ---------------------------------

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseHttpsRedirection();
}
else
{
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseLastSeenTracking();

app.MapControllers();

app.Run();

public partial class Program { }
