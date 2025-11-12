namespace RenuMate.Services.Email;

public class EmailSenderOptions
{
    public string ApiKey { get; set; } = null!;
    
    public string FromEmail { get; set; } = null!;
    
    public string FromUser { get; set; } = null!; 
}