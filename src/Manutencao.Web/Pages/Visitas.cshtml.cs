using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Web.Pages;

public class VisitasModel(AppDbContext db) : PageModel
{
    public List<VisitRow> Visits { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Visits = await db.VisitAssignments.AsNoTracking()
            .Include(v => v.ServiceRequest).ThenInclude(r => r.Feedback)
            .Include(v => v.Technician).ThenInclude(t => t.User)
            .OrderByDescending(v => v.AssignedAt)
            .Select(v => new VisitRow(
                v.Id,
                v.ServiceRequest.Protocol,
                v.ServiceRequest.Title,
                v.Technician.User.FullName,
                v.VisitStatus,
                v.Technician.LastLatitude,
                v.Technician.LastLongitude,
                v.CompletionPhotoPath,
                v.ServiceRequest.Feedback != null ? v.ServiceRequest.Feedback.Rating : null))
            .ToListAsync();
    }

    public record VisitRow(
        Guid Id,
        string Protocol,
        string Title,
        string TechnicianName,
        ServiceRequestStatus VisitStatus,
        double? LastLatitude,
        double? LastLongitude,
        string? CompletionPhotoPath,
        int? FeedbackRating);
}
