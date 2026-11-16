using Manutencao.Domain.Entities;
using Manutencao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var admin = new UserAccount
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            Email = "operador@empresa.local",
            FullName = "Operador Central",
            Phone = "11999990001",
            Role = UserRole.Operador,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Operador@123")
        };

        var client = new UserAccount
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222201"),
            Email = "cliente@demo.local",
            FullName = "Cliente Demo",
            Phone = "11999990002",
            Role = UserRole.Cliente,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cliente@123")
        };

        var techUser = new UserAccount
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333301"),
            Email = "tecnico@empresa.local",
            FullName = "João Silva",
            Phone = "11999990003",
            Role = UserRole.Tecnico,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tecnico@123")
        };

        var tech = new TechnicianProfile
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
            UserId = techUser.Id,
            EmployeeCode = "TEC-001",
            Specialties = "Elétrica,Hidráulica,Ar condicionado",
            LastLatitude = -23.5505,
            LastLongitude = -46.6333,
            LastLocationAt = DateTime.UtcNow
        };

        db.Users.AddRange(admin, client, techUser);
        db.Technicians.Add(tech);

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            db.ScheduleBlocks.Add(new TechnicianScheduleBlock
            {
                Id = Guid.NewGuid(),
                TechnicianId = tech.Id,
                DayOfWeek = day,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(18, 0)
            });
        }

        await db.SaveChangesAsync();
    }
}
