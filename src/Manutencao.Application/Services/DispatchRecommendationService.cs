using Manutencao.Application.Abstractions;
using Manutencao.Application.Dto;
using Manutencao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Application.Services;

public class DispatchRecommendationService(IAppDbContext db)
{
    public async Task<IReadOnlyList<DispatchRecommendationDto>> GetRecommendationsAsync(
        Guid serviceRequestId,
        CancellationToken cancellationToken = default)
    {
        var request = await db.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == serviceRequestId, cancellationToken);

        if (request is null)
            return Array.Empty<DispatchRecommendationDto>();

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;
        var dayOfWeek = now.DayOfWeek;
        var nowTime = TimeOnly.FromDateTime(now);

        var technicians = await db.Technicians
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.ScheduleBlocks)
            .Include(t => t.Assignments)
            .Where(t => t.User.IsActive)
            .ToListAsync(cancellationToken);

        var results = new List<DispatchRecommendationDto>();

        foreach (var tech in technicians)
        {
            var activeToday = tech.Assignments.Count(a =>
                a.AssignedAt.Date == today &&
                a.VisitStatus is not (ServiceRequestStatus.Concluida or ServiceRequestStatus.Cancelada));

            var remaining = Math.Max(0, tech.MaxDailyVisits - activeToday);
            if (remaining == 0)
                continue;

            var inSchedule = tech.ScheduleBlocks.Any(b =>
                b.DayOfWeek == dayOfWeek && b.StartTime <= nowTime && b.EndTime >= nowTime);

            var distanceKm = tech.LastLatitude.HasValue && tech.LastLongitude.HasValue
                ? GeoHelper.DistanceKm(tech.LastLatitude.Value, tech.LastLongitude.Value, request.Latitude, request.Longitude)
                : 999;

            var specialtyMatch = string.IsNullOrWhiteSpace(request.Category)
                || tech.Specialties.Contains(request.Category, StringComparison.OrdinalIgnoreCase);

            var eta = distanceKm < 900
                ? GeoHelper.EstimateTravelMinutes(distanceKm)
                : (double?)null;

            var score = 100.0;
            score -= Math.Min(distanceKm * 4, 60);
            score -= activeToday * 8;
            if (!inSchedule) score -= 25;
            if (!specialtyMatch) score -= 20;
            if (request.Priority >= VisitPriority.Alta) score += inSchedule ? 10 : 0;
            if (eta.HasValue && eta <= 30) score += 15;

            var rationaleParts = new List<string>();
            if (eta.HasValue) rationaleParts.Add($"ETA ~{eta:F0} min");
            rationaleParts.Add($"{distanceKm:F1} km");
            rationaleParts.Add($"{activeToday} visitas hoje");
            if (specialtyMatch) rationaleParts.Add("especialidade compatível");
            if (inSchedule) rationaleParts.Add("em escala agora");
            else rationaleParts.Add("fora da escala");

            results.Add(new DispatchRecommendationDto(
                tech.Id,
                tech.User.FullName,
                tech.EmployeeCode,
                tech.Specialties,
                Math.Round(Math.Max(0, score), 1),
                Math.Round(distanceKm, 2),
                activeToday,
                remaining,
                inSchedule,
                eta.HasValue ? Math.Round(eta.Value, 0) : null,
                string.Join(" · ", rationaleParts)));
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.DistanceKm).ToList();
    }
}
