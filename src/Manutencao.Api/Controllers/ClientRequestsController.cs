using System.Security.Claims;
using Manutencao.Application.Dto;
using Manutencao.Application.Services;
using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Api.Controllers;

[ApiController]
[Route("api/client/requests")]
[Authorize(Roles = "Cliente")]
public class ClientRequestsController(AppDbContext db, ServiceRequestWorkflow workflow) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateServiceRequestDto dto, CancellationToken ct)
    {
        var created = await workflow.CreateAsync(UserId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new { created.Id, created.Protocol, created.Status });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceRequestSummaryDto>>> GetMine(CancellationToken ct)
    {
        var list = await db.ServiceRequests.AsNoTracking()
            .Where(r => r.ClientId == UserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ServiceRequestSummaryDto(
                r.Id, r.Protocol, r.Title, r.Description, r.Status, r.Priority,
                r.AddressLine, r.Latitude, r.Longitude, r.CreatedAt))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientRequestDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var detail = await BuildDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{id:guid}/tracking")]
    public async Task<ActionResult<TechnicianTrackingDto>> Tracking(Guid id, CancellationToken ct)
    {
        var request = await db.ServiceRequests.AsNoTracking()
            .Include(r => r.Assignment!).ThenInclude(a => a.Technician).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == UserId, ct);

        if (request is null) return NotFound();

        var assignment = request.Assignment;
        var tech = assignment?.Technician;
        var trackingStatuses = new[]
        {
            ServiceRequestStatus.Atribuida,
            ServiceRequestStatus.EmDeslocamento,
            ServiceRequestStatus.EmAtendimento
        };

        double? lat = tech?.LastLatitude;
        double? lng = tech?.LastLongitude;
        DateTime? at = tech?.LastLocationAt;

        if (assignment is not null)
        {
            var lastPoint = await db.GpsTrackPoints.AsNoTracking()
                .Where(p => p.VisitAssignmentId == assignment.Id)
                .OrderByDescending(p => p.RecordedAt)
                .FirstOrDefaultAsync(ct);
            if (lastPoint is not null)
            {
                lat = lastPoint.Latitude;
                lng = lastPoint.Longitude;
                at = lastPoint.RecordedAt;
            }
        }

        double? distance = lat is double tLat && lng is double tLng
            ? GeoHelper.DistanceKm(tLat, tLng, request.Latitude, request.Longitude)
            : null;
        double? eta = distance is double km ? GeoHelper.EstimateTravelMinutes(km) : null;

        return Ok(new TechnicianTrackingDto(
            request.Id,
            assignment?.Id,
            request.Status,
            assignment is not null && trackingStatuses.Contains(request.Status),
            tech?.User.FullName,
            request.Latitude,
            request.Longitude,
            lat,
            lng,
            at,
            distance,
            eta));
    }

    [HttpPost("{id:guid}/feedback")]
    public async Task<ActionResult> Feedback(Guid id, [FromBody] SubmitFeedbackDto dto, CancellationToken ct)
    {
        try
        {
            await workflow.SubmitFeedbackAsync(id, UserId, dto.Rating, dto.Comment, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<ClientRequestDetailDto?> BuildDetailAsync(Guid id, CancellationToken ct)
    {
        var request = await db.ServiceRequests.AsNoTracking()
            .Include(r => r.Assignment!).ThenInclude(a => a.Technician).ThenInclude(t => t.User)
            .Include(r => r.Feedback)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == UserId, ct);

        if (request is null) return null;

        var tech = request.Assignment?.Technician;
        var photo = request.Assignment?.CompletionPhotoPath;
        var photoUrl = string.IsNullOrWhiteSpace(photo) ? null : $"{Request.Scheme}://{Request.Host}{photo}";

        return new ClientRequestDetailDto(
            request.Id,
            request.Assignment?.Id,
            request.Protocol,
            request.Title,
            request.Description,
            request.Status,
            request.Priority,
            request.AddressLine,
            request.City,
            request.Latitude,
            request.Longitude,
            request.CreatedAt,
            tech?.User.FullName,
            tech?.LastLatitude,
            tech?.LastLongitude,
            tech?.LastLocationAt,
            photoUrl,
            request.Assignment?.CompletionNotes,
            request.Feedback?.Rating,
            request.Feedback?.Comment);
    }
}
