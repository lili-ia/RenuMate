namespace RenuMate.Api.Entities;

public class PendingEmail : BaseEntity
{
    public string To { get; private set; }
    
    public string Subject { get; private set; } 
    
    public string Body { get; private set; } 
    
    public int RetryCount { get; private set; }
    
    public int MaxRetries { get; init; }

    public DateTime? LastAttemptAt { get; private set; }

    public bool IsSent { get; private set; }
    
    public string? LastError { get; private set; }

    public static PendingEmail Create(string to, string subject, string body, DateTime now, string? lastError)
    {
        return new PendingEmail(to, subject, body, now, lastError);
    }

    public void MarkSent(DateTime now)
    {
        IsSent = true;
        LastAttemptAt = now;
        LastError = null;
    }

    public void RegisterFailure(string? error, DateTime now)
    {
        if (!CanRetry())
        {
            LastAttemptAt = now;
            LastError = error;
            return;
        }
        
        RetryCount++;
        LastAttemptAt = now;
        LastError = error;
    }
    
    private const int DefaultMaxRetries = 5;

    private PendingEmail() { } 
    
    public bool CanRetry() => !IsSent && RetryCount < MaxRetries;

    private PendingEmail(string to, string subject, string body, DateTime now, string? lastError)
    {
        To = to;
        Subject = subject;
        Body = body;
        CreatedAt = now;
        MaxRetries = DefaultMaxRetries;
        RetryCount = 0;
        IsSent = false;
        LastError = lastError;
    }
}
