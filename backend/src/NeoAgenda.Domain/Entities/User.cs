namespace NeoAgenda.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Admin";
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(string username, string passwordHash, string name, string role = "Admin")
    {
        Id = Guid.NewGuid();
        Username = username.ToLowerInvariant().Trim();
        PasswordHash = passwordHash;
        Name = name;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }
}
