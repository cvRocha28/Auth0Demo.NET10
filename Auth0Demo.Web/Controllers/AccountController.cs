using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth0Demo.Web.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public async Task Login(string returnUrl = "/")
    {
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(returnUrl)
            .Build();

        await HttpContext.ChallengeAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties
        );
    }

    [Authorize]
    [HttpGet]
    public async Task Logout()
    {
        var redirectUri = Url.Action("Index", "Home") ?? "/";
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri(redirectUri)
            .Build();

        await HttpContext.SignOutAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties
        );

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult LoginError()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult Profile()
    {
        return View();
    }
}