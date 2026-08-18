using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace qauthgoogle.Controllers;

[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    // Step 1: Start the OAuth 2.0 flow.
    // Redirects the browser to Google's consent screen.
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string returnUrl = "/profile.html")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(LoginCallback), new { returnUrl })
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    // Step 2: Google redirects back here after the user consents.
    // At this point the cookie has been issued and the user is signed in.
    [HttpGet("login-callback")]
    public IActionResult LoginCallback([FromQuery] string returnUrl = "/profile.html")
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Unauthorized();
        }

        return LocalRedirect(returnUrl);
    }

    // Returns the signed-in user's profile as JSON.
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var user = new
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Name = User.FindFirstValue(ClaimTypes.Name),
            Email = User.FindFirstValue(ClaimTypes.Email),
            GivenName = User.FindFirstValue(ClaimTypes.GivenName),
            Surname = User.FindFirstValue(ClaimTypes.Surname),
            Picture = User.FindFirstValue("urn:google:picture")
        };

        return Ok(user);
    }

    // Signs the user out by clearing the authentication cookie.
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/");
    }
}
