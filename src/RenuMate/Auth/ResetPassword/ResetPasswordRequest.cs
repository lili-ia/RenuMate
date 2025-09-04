namespace RenuMate.Auth.ResetPassword;

public class ResetPasswordRequest
{
    public string Token { get; set; } = null!;
    
    public string NewPassword { get; set; } = null!;
}