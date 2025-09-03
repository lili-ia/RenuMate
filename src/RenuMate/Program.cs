using FastEndpoints;
using FastEndpoints.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services
    .AddAuthenticationJwtBearer(s => 
        s.SigningKey = builder.Configuration["Jwt:SigningKey"])
    .AddAuthorization()
    .AddFastEndpoints();

var app = builder.Build();

app.UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();