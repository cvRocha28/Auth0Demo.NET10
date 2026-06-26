using Auth0.AspNetCore.Authentication;
using Auth0Demo.Web.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

#region Configuration

builder.Services.Configure<Auth0Options>(
    builder.Configuration.GetSection(Auth0Options.SectionName));

var auth0Options = builder.Configuration
    .GetSection(Auth0Options.SectionName)
    .Get<Auth0Options>()
    ?? throw new InvalidOperationException("A configuração do Auth0 não foi encontrada.");

#endregion

#region Services

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = auth0Options.Domain;
        options.ClientId = auth0Options.ClientId;
        options.ClientSecret = auth0Options.ClientSecret;

        options.OpenIdConnectEvents = new OpenIdConnectEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;

                var rolesClaim = context.Principal?.FindFirst("https://auth0demo.com/roles");

                if (identity != null && rolesClaim != null)
                {
                    var roles = rolesClaim.Value
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var role in roles)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }

                return Task.CompletedTask;
            },

            OnRemoteFailure = context =>
            {
                context.HandleResponse();

                var message = context.Failure?.Message ?? string.Empty;

                if (message.Contains("access_denied", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/Account/AccessDenied");
                    return Task.CompletedTask;
                }

                context.Response.Redirect("/Account/LoginError");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UsuarioLogado", policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("Dashboard", policy =>
    {
        policy.RequireRole("Admin");
    });
});

#endregion

var app = builder.Build();

#region Middleware

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var auth0Domain = $"https://{auth0Options.Domain}";

    context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
    context.Response.Headers[HeaderNames.XFrameOptions] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

    context.Response.Headers[HeaderNames.ContentSecurityPolicy] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self' " + auth0Domain + "; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "connect-src 'self' " + auth0Domain + "; " +
        "frame-src " + auth0Domain + ";";

    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

#endregion

#region Routes

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

#endregion

app.Run();