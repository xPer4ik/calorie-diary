using System.Text;
using CalorieDiary.Api.Dtos;
using CalorieDiary.Api.Data;
using CalorieDiary.Api.Endpoints;
using CalorieDiary.Api.Models;
using CalorieDiary.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

const string FrontendCorsPolicy = "FrontendCors";

var builder = WebApplication.CreateBuilder(args);
var sqliteConnectionString = builder.Configuration.GetConnectionString("SQLite")
    ?? throw new InvalidOperationException("Connection string 'SQLite' is not configured.");
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT token. Example: Bearer eyJhbGciOi..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            []
        }
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<CalorieCalculatorService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(
                    new ErrorResponse("Authentication is required or the token is invalid."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(
                    new ErrorResponse("You do not have access to this resource."));
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.Logger.LogInformation(
    "Starting Calorie Diary API in {Environment} environment.",
    app.Environment.EnvironmentName);

using (var scope = app.Services.CreateScope())
{
    var dbLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseStartup");
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    dbLogger.LogInformation("Ensuring SQLite database is created.");
    await dbContext.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(dbContext, dbLogger);
}

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("Swagger UI enabled.");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapCalculatorEndpoints();
app.MapProfileEndpoints();
app.MapFoodEndpoints();
app.MapDiaryEndpoints();

app.Run();
