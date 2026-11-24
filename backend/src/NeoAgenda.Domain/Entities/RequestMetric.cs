namespace NeoAgenda.Domain.Entities;

public class RequestMetric
{
    public Guid Id { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public long DurationMs { get; private set; }
    public DateTime Timestamp { get; private set; }

    private RequestMetric() { }

    public RequestMetric(string method, string path, int statusCode, long durationMs)
    {
        Id = Guid.NewGuid();
        Method = method;
        Path = path;
        StatusCode = statusCode;
        DurationMs = durationMs;
        Timestamp = DateTime.UtcNow;
    }
}
