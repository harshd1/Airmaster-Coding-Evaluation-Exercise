namespace AirmasterOrderApi.Models;

/// <summary>
/// Authentication request model for user login.
/// In production, would validate against Azure AD B2C or similar identity provider.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public User? User { get; set; }
}

public class User
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer"; // Customer, Admin
}
