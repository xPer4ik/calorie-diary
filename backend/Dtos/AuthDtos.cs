namespace CalorieDiary.Api.Dtos;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName);

public record LoginRequest(
    string Email,
    string Password);

public record CurrentUserResponse(
    int Id,
    string Email,
    string DisplayName);

public record AuthResponse(
    string Token,
    CurrentUserResponse User);

public record ErrorResponse(
    string Error,
    object? Details = null);
