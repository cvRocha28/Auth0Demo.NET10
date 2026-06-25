using Auth0.AspNetCore.Authentication;
using Auth0Demo.Web.Configuration;

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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UsuarioLogado", policy =>
    {
        policy.RequireAuthenticatedUser();
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