using Manutencao.Application.Abstractions;
using Manutencao.Application.Services;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
});

var dbPath = builder.Configuration["ConnectionStrings:Default"]
             ?? Path.Combine(builder.Environment.ContentRootPath, "..", "Manutencao.Api", "manutencao.db");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source={Path.GetFullPath(dbPath)}"));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<DispatchRecommendationService>();
builder.Services.AddScoped<ServiceRequestWorkflow>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Login";
        o.LogoutPath = "/Logout";
        o.ExpireTimeSpan = TimeSpan.FromHours(12);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SqliteSchemaPatch.ApplyAsync(db);
    await SeedData.InitializeAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

var proofsDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Manutencao.Api", "wwwroot", "uploads", "proofs"));
app.MapGet("/provas/{file}", (string file) =>
{
    if (string.IsNullOrWhiteSpace(file) || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        return Results.BadRequest();
    var path = Path.GetFullPath(Path.Combine(proofsDir, file));
    if (!path.StartsWith(proofsDir, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        return Results.NotFound();
    return Results.File(path, "image/jpeg");
}).RequireAuthorization();

app.Run();
