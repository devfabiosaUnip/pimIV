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
[Route("api/operator")]
[Authorize(Roles = "Operador,Admin")]
public class OperatorController(
    AppDbContext db,
    ServiceRequestWorkflow workflow,
    DispatchRecommendationService dispatch) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("requests/pending")]
    public async Task<ActionResult> Pending(CancellationToken ct)
    {
        var items = await db.ServiceRequests.AsNoTracking()
            .Where(r => r.Status == ServiceRequestStatus.PendenteAprovacao
                        || r.Status == ServiceRequestStatus.AguardandoAtribuicao)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new ServiceRequestSummaryDto(
                r.Id, r.Protocol, r.Title, r.Description, r.Status, r.Priority,
                r.AddressLine, r.Latitude, r.Longitude, r.CreatedAt))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("requests/{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken ct)
    {
        await workflow.ApproveAsync(id, UserId, ct);
        return NoContent();
    }

    [HttpGet("requests/{id:guid}/recommendations")]
    public async Task<ActionResult<IReadOnlyList<DispatchRecommendationDto>>> Recommendations(Guid id, CancellationToken ct) =>
        Ok(await dispatch.GetRecommendationsAsync(id, ct));

    public record AssignRequest(Guid TechnicianId, string? Notes);

    [HttpPost("requests/{id:guid}/assign")]
    public async Task<ActionResult> Assign(Guid id, [FromBody] AssignRequest body, CancellationToken ct)
    {
        await workflow.AssignAsync(id, body.TechnicianId, UserId, body.Notes, ct);
        return NoContent();
    }

    [HttpGet("technicians")]
    public async Task<ActionResult> Technicians(CancellationToken ct)
    {
        var techs = await db.Technicians.AsNoTracking()
            .Include(t => t.User)
            .Select(t => new
            {
                t.Id,
                t.EmployeeCode,
                t.User.FullName,
                t.Specialties,
                t.LastLatitude,
                t.LastLongitude,
                t.LastLocationAt
            })
            .ToListAsync(ct);
        return Ok(techs);
    }

    [HttpGet("visits")]
    public async Task<ActionResult> Visits(CancellationToken ct)
    {
        var visits = await db.VisitAssignments.AsNoTracking()
            .Include(v => v.ServiceRequest).ThenInclude(r => r.Feedback)
            .Include(v => v.Technician).ThenInclude(t => t.User)
            .OrderByDescending(v => v.AssignedAt)
            .Select(v => new
            {
                v.Id,
                v.ServiceRequestId,
                v.ServiceRequest.Protocol,
                v.ServiceRequest.Title,
                v.VisitStatus,
                TechnicianName = v.Technician.User.FullName,
                v.Technician.LastLatitude,
                v.Technician.LastLongitude,
                v.Technician.LastLocationAt,
                v.CompletionPhotoPath,
                FeedbackRating = v.ServiceRequest.Feedback != null ? v.ServiceRequest.Feedback.Rating : (int?)null
            })
            .ToListAsync(ct);
        return Ok(visits);
    }
}
