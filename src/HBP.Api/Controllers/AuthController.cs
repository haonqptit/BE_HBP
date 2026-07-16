using System.Security.Claims;
using HBP.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HBP.Api.Infrastructure;
using System.Security.Cryptography;

namespace HBP.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult Csrf()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append(AdminCsrfMiddleware.CookieName, token, new CookieOptions
        {
            HttpOnly = false, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/"
        });
        return Ok(new { token });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.Succeeded)
            return result.IsLockedOut ? StatusCode(423, new { message = "Account is temporarily locked." }) : Unauthorized();

        var user = result.User!;
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        var user = await authService.GetByIdAsync(id, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }
}
