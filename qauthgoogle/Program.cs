using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// --- "Sign in with Google" OAuth 2.0 configuration ---
// Cookie authentication stores the signed-in user; Google handles the OAuth 2.0 login.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        // Create these credentials in the Google Cloud Console:
        // https://console.cloud.google.com/apis/credentials
        // Store them via user secrets or environment variables (never commit real secrets).
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Missing config: Authentication:Google:ClientId");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Missing config: Authentication:Google:ClientSecret");

        // Must match an Authorized redirect URI registered in the Google Cloud Console.
        options.CallbackPath = "/signin-google";

        // Request the user's basic profile and email.
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = true;

        // Map the profile picture URL from Google's userinfo response into a claim.
        options.Events.OnCreatingTicket = context =>
        {
            if (context.User.TryGetProperty("picture", out var picture) &&
                context.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim("urn:google:picture", picture.GetString() ?? string.Empty));
            }

            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
