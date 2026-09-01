using BiometricClockingSystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = RequireConfiguration(builder.Configuration, "ConnectionStrings:DefaultConnection");
var jwtKey = RequireConfiguration(builder.Configuration, "Jwt:Key");
var jwtIssuer = RequireConfiguration(builder.Configuration, "Jwt:Issuer");
var jwtAudience = RequireConfiguration(builder.Configuration, "Jwt:Audience");
var allowedOrigins = GetAllowedOrigins(builder.Configuration);
_ = RequireConfiguration(builder.Configuration, "AllowedHosts");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be provided by a secure environment variable and be at least 32 characters long.");

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,

        IssuerSigningKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtKey)),

        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };

    // The browser sends the session JWT only in an HttpOnly cookie. This also
    // lets EventSource authenticate without exposing a token in its URL.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["verity_session"];

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtBearer");

            logger.LogWarning(
                context.Exception,
                "JWT authentication failed for {Path}",
                context.Request.Path);

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var subject = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var securityStamp = context.Principal?.FindFirst("security_stamp")?.Value;

            if (!Guid.TryParse(subject, out var userId) || string.IsNullOrWhiteSpace(securityStamp))
            {
                context.Fail("The authentication token is invalid.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var isCurrentSession = await db.Users.AsNoTracking().AnyAsync(user =>
                user.Id == userId && user.IsActive && user.SecurityStamp == securityStamp);

            if (!isCurrentSession)
                context.Fail("The authentication session has expired.");
        }
    };
});


// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
//builder.Services.AddSingleton<BiometricClockingSystem.Api.Services.IFacialRecognitionService,
   // BiometricClockingSystem.Api.Services.FacialRecognitionService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.Configure<BiometricClockingSystem.Api.Services.SendGridOptions>(
    builder.Configuration.GetSection(BiometricClockingSystem.Api.Services.SendGridOptions.SectionName));
builder.Services.AddScoped<BiometricClockingSystem.Api.Services.IOtpService, BiometricClockingSystem.Api.Services.OtpService>();
builder.Services.AddScoped<BiometricClockingSystem.Api.Services.IAttendanceService, BiometricClockingSystem.Api.Services.AttendanceService>();
builder.Services.AddSingleton<AdminNotificationService>();

//face record
builder.Services.AddScoped<IFacialRecognitionService, FacialRecognitionService>();
//builder.Services.AddScoped<IFingerprintMatchingService, FingerprintMatchingService>();

// Controllers
builder.Services.AddControllers()
 .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many requests. Please wait and try again." },
            cancellationToken);
    };
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("otp", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("kiosk", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("privileged", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? ClientPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    options.SwaggerDoc("v1", new()
    {
        Title = "Biometric Clocking API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Secure-by-default: every controller action must opt in to anonymous access.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("PasswordReady", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Admin") ||
        context.User.HasClaim("password_change_required", "false")));
});

var app = builder.Build();

// Initialize the independent side-project database on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// Render terminates TLS before forwarding requests to this container. The
// container is not directly exposed, so its proxy headers can be trusted.
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        app.Logger.LogError(exception, "Unhandled request error. TraceId: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "An unexpected server error occurred.",
            traceId = context.TraceIdentifier
        });
    }));
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Permissions-Policy", "camera=(self), geolocation=(self), microphone=()");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'");
    await next();
});

app.UseCors("ReactPolicy");

app.UseRateLimiter();

app.UseAuthentication();

// Cookie-authenticated write requests require a token bound to the signed
// session. Cross-origin forms cannot add this custom header, preventing CSRF.
app.Use(async (context, next) =>
{
    var isUnsafeMethod = HttpMethods.IsPost(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method)
        || HttpMethods.IsPatch(context.Request.Method)
        || HttpMethods.IsDelete(context.Request.Method);

    if (isUnsafeMethod && context.User.Identity?.IsAuthenticated == true)
    {
        var expectedToken = context.User.FindFirst("csrf")?.Value;
        var suppliedToken = context.Request.Headers["X-CSRF-Token"].ToString();
        var matches = !string.IsNullOrWhiteSpace(expectedToken)
            && !string.IsNullOrWhiteSpace(suppliedToken)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedToken),
                Encoding.UTF8.GetBytes(suppliedToken));

        if (!matches)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "CSRF validation failed." });
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();

static string ClientPartitionKey(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static string RequireConfiguration(IConfiguration configuration, string key)
{
    var value = configuration[key]?.Trim();
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"{key} must be provided through environment configuration.");

    return value;
}

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = RequireConfiguration(configuration, "Cors:AllowedOrigins")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(origin => origin.TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (configuredOrigins.Length == 0 || configuredOrigins.Any(origin =>
        !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
        !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must contain comma-separated HTTP(S) origins without paths.");
    }

    return configuredOrigins;
}
