using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Auth;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, JwtTokenService jwt) : ControllerBase
{
    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string Token, string FullName, UserRole Role, Guid UserId);

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Credenciais inválidas." });

        var token = jwt.CreateToken(user);
        return Ok(new LoginResponse(token, user.FullName, user.Role, user.Id));
    }
}
