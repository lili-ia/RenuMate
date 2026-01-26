namespace RenuMate.Api.Services.Email;

public class EmailSenderOptions
{
    public string Host { get; set; } = null!;
    
    public int Port { get; set; } 
    
    public string UserName { get; set; } = null!; 
    
    public string Password { get; set; } = null!; 
    
    public string FromUser { get; set; } = null!; 
    
    public string FromEmail { get; set; } = null!; 
}