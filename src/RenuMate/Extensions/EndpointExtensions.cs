using RenuMate.Auth.ConfirmEmail;
using RenuMate.Auth.Login;
using RenuMate.Auth.Register;
using RenuMate.Auth.RequestPasswordReset;
using RenuMate.Auth.ResetPassword;
using RenuMate.Users.Deactivate;
using RenuMate.Users.Reactivate;
using RenuMate.Users.RequestReactivate;

namespace RenuMate.Extensions;

public static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        ConfirmEmailEndpoint.Map(app);
        LoginUserEndpoint.Map(app);
        RegisterUserEndpoint.Map(app);
        PasswordResetRequestEndpoint.Map(app);
        ResetPasswordEndpoint.Map(app);
        
        DeactivateUserEndpoint.Map(app);
        ReactivateUserEndpoint.Map(app);
        RequestUserReactivateEndpoint.Map(app);
    }
}