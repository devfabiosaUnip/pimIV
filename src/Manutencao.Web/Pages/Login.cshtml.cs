using System.Security.Claims;
using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Web.Pages;

[AllowAnonymous]
public class LoginModel(AppDbContext db) : PageModel
{
    [BindProperty] public string Email { get; set; } = "operador@empresa.local";
    [BindProperty] public string Password { get; set; } = "Operador@123";
    public string? Error { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == Email && u.IsActive);
        if (user is null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
        {
            Error = "Credenciais inválidas.";
            return Page();
        }

        if (user.Role is not UserRole.Operador and not UserRole.Admin)
        {
            Error = "Este painel é exclusivo da operação.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToPage("/Index");
    }
}
