namespace RenuMate.Services.Email;

public class SmtpOptions
{
    public string Host { get; set; } = null!;
    
    public int Port { get; set; }
    
    public string FromEmail { get; set; } = null!;
    
    public string Password { get; set; } = null!;
    
    public string User { get; set; } = null!; 
}