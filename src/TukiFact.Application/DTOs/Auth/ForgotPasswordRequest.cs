namespace TukiFact.Application.DTOs.Auth;

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);
