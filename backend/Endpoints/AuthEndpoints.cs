using System.Net.Mail;
using CalorieDiary.Api.Data;
using CalorieDiary.Api.Dtos;
using CalorieDiary.Api.Models;
using CalorieDiary.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CalorieDiary.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .AllowAnonymous()
            .WithOpenApi();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .WithOpenApi();

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AppDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        JwtTokenService jwtTokenService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(AuthEndpoints));
        var errors = ValidateRegisterRequest(request);

        if (errors.Count > 0)
        {
            logger.LogInformation("Registration rejected because request is invalid.");
            return Results.BadRequest(new ErrorResponse("Validation failed.", errors));
        }

        var email = NormalizeEmail(request.Email);
        var displayName = request.DisplayName.Trim();

        if (await dbContext.Users.AnyAsync(user => user.Email == email))
        {
            logger.LogInformation("Registration rejected because email {Email} already exists.", email);
            return Results.Conflict(new ErrorResponse("Email is already registered."));
        }

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Registration failed while saving user with email {Email}.", email);
            return Results.Conflict(new ErrorResponse("Email is already registered."));
        }

        logger.LogInformation("User {UserId} registered.", user.Id);

        return Results.Created(
            "/api/auth/me",
            new AuthResponse(jwtTokenService.CreateToken(user), ToCurrentUserResponse(user)));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        JwtTokenService jwtTokenService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(AuthEndpoints));
        var errors = ValidateLoginRequest(request);

        if (errors.Count > 0)
        {
            logger.LogInformation("Login rejected because request is invalid.");
            return Results.BadRequest(new ErrorResponse("Validation failed.", errors));
        }

        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Email == email);

        if (user is null)
        {
            logger.LogInformation("Login failed for unknown email {Email}.", email);
            return Unauthorized("Invalid email or password.");
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            logger.LogInformation("Login failed for user {UserId}.", user.Id);
            return Unauthorized("Invalid email or password.");
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Password hash rehashed for user {UserId}.", user.Id);
        }

        logger.LogInformation("User {UserId} logged in.", user.Id);

        return Results.Ok(new AuthResponse(
            jwtTokenService.CreateToken(user),
            ToCurrentUserResponse(user)));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(AuthEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Authorized request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId.Value);

        if (user is null)
        {
            logger.LogWarning("User {UserId} from token was not found.", userId);
            return Results.NotFound(new ErrorResponse("User from token was not found."));
        }

        logger.LogInformation("Current user requested for user {UserId}.", user.Id);

        return Results.Ok(ToCurrentUserResponse(user));
    }

    private static Dictionary<string, string[]> ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = ValidateLoginRequest(new LoginRequest(request.Email, request.Password));

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors["displayName"] = ["Display name is required."];
        }
        else if (request.DisplayName.Trim().Length > 100)
        {
            errors["displayName"] = ["Display name must be 100 characters or fewer."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateLoginRequest(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors["email"] = ["Email is required."];
        }
        else if (email.Length > 256)
        {
            errors["email"] = ["Email must be 256 characters or fewer."];
        }
        else if (!IsValidEmail(email))
        {
            errors["email"] = ["Email format is invalid."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["Password is required."];
        }
        else if (request.Password.Length < 6)
        {
            errors["password"] = ["Password must contain at least 6 characters."];
        }

        return errors;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static CurrentUserResponse ToCurrentUserResponse(User user)
    {
        return new CurrentUserResponse(user.Id, user.Email, user.DisplayName);
    }

    private static IResult Unauthorized(string error)
    {
        return Results.Json(
            new ErrorResponse(error),
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
