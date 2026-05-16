namespace AirmasterOrderApi.Services;

using AirmasterOrderApi.Models;
using System.Text;
using System.Security.Cryptography;

/// <summary>
/// Authentication service for user login and JWT token generation.
/// In production, would integrate with Azure AD B2C for OAuth/OIDC flow.
/// This demo uses basic token generation for evaluation purposes.
/// </summary>
public class AuthenticationService
{
    // Demo users (in production, validate against Azure AD B2C)
    private readonly List<(string Email, string Password, User User)> _demoUsers = new()
    {
        ("customer@example.com", "password123", new User 
        { 
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"), 
            Email = "customer@example.com", 
            Name = "John Customer",
            Role = "Customer"
        }),
        ("admin@example.com", "admin123", new User 
        { 
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000099"), 
            Email = "admin@example.com", 
            Name = "Admin User",
            Role = "Admin"
        })
    };

    public AuthResponse Login(LoginRequest request)
    {
        var user = _demoUsers.FirstOrDefault(u => 
            u.Email == request.Email && u.Password == request.Password);

        if (user == default)
        {
            return new AuthResponse 
            { 
                Success = false, 
                Message = "Invalid email or password." 
            };
        }

        var token = GenerateJwtToken(user.User);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            User = user.User
        };
    }

    public User? ValidateToken(string token)
    {
        try
        {
            // For demo purposes, verify basic token format and extract user info
            // In production, use proper JWT validation with signing keys
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            // Decode the payload (in reality, should verify signature)
            var payload = parts[1];
            var padded = payload.Length % 4 == 0 
                ? payload 
                : payload + new string('=', 4 - payload.Length % 4);

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(padded));

            // For demo, just verify structure exists
            return decoded.Contains("sub") ? new User { UserId = Guid.NewGuid() } : null;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateJwtToken(User user)
    {
        // Simplified JWT creation for demo (NOT production-ready)
        // Production would use System.IdentityModel.Tokens.Jwt and proper signing
        var header = Base64Url.Encode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        var payload = Base64Url.Encode(
            $"{{\"sub\":\"{user.UserId}\",\"email\":\"{user.Email}\",\"role\":\"{user.Role}\",\"iat\":{UnixTimeStamp()},\"exp\":{UnixTimeStamp() + 3600}}}"
        );
        var signature = GenerateSignature($"{header}.{payload}");

        return $"{header}.{payload}.{signature}";
    }

    private static long UnixTimeStamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static string GenerateSignature(string input)
    {
        // Demo signature (NOT secure - for evaluation only)
        // Production would use proper HMAC-SHA256 with secret key
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("demo-secret-key")))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Base64Url.Encode(hash);
        }
    }
}

internal static class Base64Url
{
    public static string Encode(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(inputBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static string Encode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
