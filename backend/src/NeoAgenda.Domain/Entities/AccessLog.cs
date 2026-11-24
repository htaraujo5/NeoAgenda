namespace NeoAgenda.Domain.Entities;

public class AccessLog
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty; // ex: "LOGIN_SUCCESS", "LOGIN_FAILED", "TOKEN_REFRESH", "USER_CREATED", "ACCESS_REVOKED"
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AccessLog() { }

    public AccessLog(string username, string action, string ipAddress, string userAgent, bool success, string? details = null)
    {
        Id = Guid.NewGuid();
        Username = username;
        Action = action;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Success = success;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}
