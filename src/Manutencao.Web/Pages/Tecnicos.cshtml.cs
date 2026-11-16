using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Web.Pages;

public class TecnicosModel(AppDbContext db) : PageModel
{
    public List<TechRow> Techs { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Techs = await db.Technicians.AsNoTracking()
            .Include(t => t.User)
            .Select(t => new TechRow(t.EmployeeCode, t.User.FullName, t.Specialties, t.LastLatitude, t.LastLongitude, t.LastLocationAt))
            .ToListAsync();
    }

    public record TechRow(string EmployeeCode, string FullName, string Specialties, double? LastLatitude, double? LastLongitude, DateTime? LastLocationAt);
}
