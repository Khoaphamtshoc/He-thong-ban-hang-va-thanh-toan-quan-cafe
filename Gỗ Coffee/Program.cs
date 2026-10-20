using System.Security.Claims;
using CuaHangTienLoi.Web.Components;
using CuaHangTienLoi.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/forbidden";
        options.Cookie.Name = "GoCoffee.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<CredentialService>();
builder.Services.AddScoped<StoreDb>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Form POST (full HTTP request): cookie is issued BEFORE a Blazor circuit starts.
// Minimal API form binding enforces antiforgery validation by default.
app.MapPost("/auth/login", async (HttpContext http, [FromForm] string username,
    [FromForm] string password, CredentialService credentials) =>
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        return Results.LocalRedirect("/login?error=invalid");
    try
    {
        var user = await credentials.ValidateAsync(username.Trim(), password, http.RequestAborted);
        if (user is null) return Results.LocalRedirect("/login?error=invalid");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.MaNhanVien.ToString()),
            new Claim(ClaimTypes.Name, user.TenNhanVien),
            new Claim(ClaimTypes.Email, user.TenDangNhap),
            new Claim(ClaimTypes.Role, user.VaiTro ?? "STAFF")
        };
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Results.LocalRedirect(user.VaiTro switch
        {
            "STAFF" => "/sales", "WAREHOUSE" => "/inventory", _ => "/dashboard"
        });
    }
    catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException or InvalidOperationException)
    {
        app.Logger.LogError(ex, "Cannot authenticate against PostgreSQL");
        return Results.LocalRedirect("/login?error=database");
    }
}).AllowAnonymous();

app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
}).RequireAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", application = "Go Coffee" }));
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
