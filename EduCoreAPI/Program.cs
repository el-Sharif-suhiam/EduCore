using EduCore_API.Middlewares;
using EduCoreAPI.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using QuestPDF.Infrastructure;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// JWT Authentication Configuration
// ===============================

DotNetEnv.Env.Load();
QuestPDF.Settings.License = LicenseType.Community;

// Register authentication services in the dependency injection container.
// JwtBearerDefaults.AuthenticationScheme tells ASP.NET Core that
// JWT Bearer authentication will be the default authentication method.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TokenValidationParameters define how incoming JWTs will be validated.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Ensures the token was issued by a trusted issuer.
            ValidateIssuer = true,


            // Ensures the token is intended for this API (audience check).
            ValidateAudience = true,


            // Ensures the token has not expired.
            ValidateLifetime = true,


            // Ensures the token signature is valid and was signed by the API.
            ValidateIssuerSigningKey = true,


            // The expected issuer value (must match the issuer used when creating the JWT).
            ValidIssuer = "EduCoreApi",


            // The expected audience value (must match the audience used when creating the JWT).
            ValidAudience = "EduCoreApiUsers",


            // The secret key used to validate the JWT signature.
            // This must be the same key used when generating the token.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")))
        };
    });

// ===============================
// Authorization Configuration
// ===============================


// Register authorization services.
// This enables attributes like [Authorize] and role-based authorization.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserOwnerOrAdmin", policy =>
    {
        policy.Requirements.Add(new UserOwnerOrAdminRequirement());
    });
    options.AddPolicy("UserOwnerOnly", policy =>
    {
        policy.Requirements.Add(new UserOwnerOnlyRequirement());
    });
    options.AddPolicy("IsUserEnrolledOrAdmin", policy =>
    {
        policy.Requirements.Add(new IsUserEnrolledOrAdminRequirement());
    });
    options.AddPolicy("InstructorOwnership", policy =>
    {
        policy.Requirements.Add(new InstructorOwnershipRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, UserOwnerOrAdminHandler>();

builder.Services.AddSingleton<
    IAuthorizationHandler,
    UserOwnerOnlyHandler>();


builder.Services.AddSingleton<
    IAuthorizationHandler,
    InstructorOwnershipHandler>();


builder.Services.AddSingleton<
    IAuthorizationHandler,
    IsUserEnrolledOrAdminHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("RegistrationLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// Register controller support.
builder.Services.AddControllers();


// ===============================
// Swagger Configuration
// ===============================


// Enables Swagger endpoint discovery.
builder.Services.AddEndpointsApiExplorer();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(options =>
{
    // ===============================
    // 1) Define the JWT Bearer security scheme
    // ===============================
    //
    // Configures Swagger to recognize JWT Bearer authentication.
    // This adds the "Authorize" button in the Swagger UI.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // The name of the HTTP header used to pass the token.
        Name = "Authorization",

        // Specifies this is an HTTP-based authentication scheme.
        Type = SecuritySchemeType.Http,

        // The authentication scheme, must be "Bearer" for JWT.
        Scheme = "Bearer",

        // Describes the format of the token (optional).
        BearerFormat = "JWT",

        // Indicates the token is provided in the request header.
        In = ParameterLocation.Header,

        // Description shown in Swagger UI to guide users.
        Description = "Enter: Bearer {your JWT token}"
    });

    // ===============================
    // 2) Apply the Bearer security requirement globally
    // ===============================
    //
    // Requires the Bearer token for all endpoints tagged with [Authorize].
    // This ensures secured endpoints are locked by default in the UI.
    //options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {

    //        new OpenApiSecurityScheme
    //        {
    //            // References the "Bearer" scheme defined above.
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        // No OAuth scopes are used in this setup.
    //        Array.Empty<string>()
    //    }
    //});


    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("EduCoreApiCorsPolicies", policy =>
    {
        policy.WithOrigins("https://localhost:7009",
            "http://localhost:5087")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ExceptionMiddleware>();


app.UseHttpsRedirection();
app.UseCors("EduCoreApiCorsPolicies");

app.UseRateLimiter();
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests
        && !context.Response.HasStarted
        && !context.Response.ContentLength.HasValue)
    {
        await context.Response.WriteAsync("Too many requests. Please try again later.");
    }
});

// IMPORTANT:
// Authentication middleware must run BEFORE authorization middleware.
// Authentication identifies the user.
// Authorization decides what the user is allowed to do.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
