using Manutencao.Application.Services;
using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Web.Pages;

public class VisitaModel(AppDbContext db) : PageModel
{
    public VisitDetail? Visit { get; private set; }
    public string? ProofFile { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Visit = await LoadAsync(id);
        if (Visit is null) return NotFound();
        ProofFile = string.IsNullOrWhiteSpace(Visit.CompletionPhotoPath)
            ? null
            : Path.GetFileName(Visit.CompletionPhotoPath);
        return Page();
    }

    public async Task<IActionResult> OnGetLocationAsync(Guid id)
    {
        var visit = await db.VisitAssignments.AsNoTracking()
            .Include(v => v.Technician)
            .Include(v => v.ServiceRequest)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (visit is null) return NotFound();

        var lastPoint = await db.GpsTrackPoints.AsNoTracking()
            .Where(p => p.VisitAssignmentId == id)
            .OrderByDescending(p => p.RecordedAt)
            .FirstOrDefaultAsync();

        var lat = lastPoint?.Latitude ?? visit.Technician.LastLatitude;
        var lon = lastPoint?.Longitude ?? visit.Technician.LastLongitude;
        var at = lastPoint?.RecordedAt ?? visit.Technician.LastLocationAt;
        double? distance = lat is double tLat && lon is double tLon
            ? GeoHelper.DistanceKm(tLat, tLon, visit.ServiceRequest.Latitude, visit.ServiceRequest.Longitude)
            : null;
        double? eta = distance is double km ? GeoHelper.EstimateTravelMinutes(km) : null;

        return new JsonResult(new { lat, lon, at, distanceKm = distance, etaMinutes = eta });
    }

    private async Task<VisitDetail?> LoadAsync(Guid id)
    {
        return await db.VisitAssignments.AsNoTracking()
            .Include(v => v.ServiceRequest).ThenInclude(r => r.Feedback)
            .Include(v => v.Technician).ThenInclude(t => t.User)
            .Where(v => v.Id == id)
            .Select(v => new VisitDetail(
                v.Id,
                v.ServiceRequest.Protocol,
                v.ServiceRequest.Title,
                v.ServiceRequest.Description,
                v.ServiceRequest.AddressLine,
                v.ServiceRequest.City,
                v.ServiceRequest.Latitude,
                v.ServiceRequest.Longitude,
                v.Technician.User.FullName,
                v.VisitStatus,
                v.CompletionPhotoPath,
                v.CompletionNotes,
                v.ServiceRequest.Feedback != null ? v.ServiceRequest.Feedback.Rating : null,
                v.ServiceRequest.Feedback != null ? v.ServiceRequest.Feedback.Comment : null))
            .FirstOrDefaultAsync();
    }

    public static string Stars(int rating) => new string('★', rating) + new string('☆', 5 - rating);

    public record VisitDetail(
        Guid Id,
        string Protocol,
        string Title,
        string Description,
        string AddressLine,
        string City,
        double Latitude,
        double Longitude,
        string TechnicianName,
        ServiceRequestStatus VisitStatus,
        string? CompletionPhotoPath,
        string? CompletionNotes,
        int? FeedbackRating,
        string? FeedbackComment);
}
