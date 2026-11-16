using System.Security.Claims;
using Manutencao.Api.Hubs;
using Manutencao.Application.Dto;
using Manutencao.Application.Services;
using Manutencao.Domain.Entities;
using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Api.Controllers;

[ApiController]
[Route("api/intranet/technician")]
[Authorize(Roles = "Tecnico")]
public class IntranetTechnicianController(
    AppDbContext db,
    ServiceRequestWorkflow workflow,
    IHubContext<TrackingHub> hub,
    IWebHostEnvironment env) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("visits")]
    public async Task<ActionResult<IEnumerable<TechnicianVisitDto>>> Visits(CancellationToken ct)
    {
        var techId = await db.Technicians.AsNoTracking()
            .Where(t => t.UserId == UserId)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (techId == Guid.Empty) return NotFound();

        var visits = await db.VisitAssignments.AsNoTracking()
            .Include(v => v.ServiceRequest)
            .Where(v => v.TechnicianId == techId &&
                        v.VisitStatus != ServiceRequestStatus.Cancelada)
            .OrderBy(v => v.VisitStatus == ServiceRequestStatus.Concluida)
            .ThenBy(v => v.ScheduledFor)
            .Select(v => new TechnicianVisitDto(
                v.Id,
                v.ServiceRequestId,
                v.ServiceRequest.Protocol,
                v.ServiceRequest.Title,
                v.ServiceRequest.Description,
                v.ServiceRequest.AddressLine,
                v.ServiceRequest.City,
                v.ServiceRequest.Latitude,
                v.ServiceRequest.Longitude,
                v.VisitStatus,
                v.ScheduledFor))
            .ToListAsync(ct);

        return Ok(visits);
    }

    public record LocationUpdate(double Latitude, double Longitude, double? SpeedKmh);
    public record StatusUpdate(ServiceRequestStatus Status);

    [HttpPost("location")]
    public async Task<ActionResult> UpdateLocation([FromBody] LocationUpdate body, CancellationToken ct)
    {
        var tech = await db.Technicians.FirstOrDefaultAsync(t => t.UserId == UserId, ct);
        if (tech is null) return NotFound();

        tech.LastLatitude = body.Latitude;
        tech.LastLongitude = body.Longitude;
        tech.LastLocationAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("visits/{assignmentId:guid}/track")]
    public async Task<ActionResult> Track(Guid assignmentId, [FromBody] LocationUpdate body, CancellationToken ct)
    {
        var tech = await db.Technicians.FirstOrDefaultAsync(t => t.UserId == UserId, ct);
        if (tech is null) return NotFound();

        var assignment = await db.VisitAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.TechnicianId == tech.Id, ct);
        if (assignment is null) return NotFound();

        tech.LastLatitude = body.Latitude;
        tech.LastLongitude = body.Longitude;
        tech.LastLocationAt = DateTime.UtcNow;

        db.GpsTrackPoints.Add(new GpsTrackPoint
        {
            Id = Guid.NewGuid(),
            VisitAssignmentId = assignmentId,
            Latitude = body.Latitude,
            Longitude = body.Longitude,
            SpeedKmh = body.SpeedKmh,
            RecordedAt = DateTime.UtcNow
        });

        if (assignment.VisitStatus == ServiceRequestStatus.Atribuida)
            assignment.VisitStatus = ServiceRequestStatus.EmDeslocamento;

        if (assignment.VisitStatus == ServiceRequestStatus.EmDeslocamento)
        {
            var request = await db.ServiceRequests.FirstAsync(r => r.Id == assignment.ServiceRequestId, ct);
            if (request.Status is ServiceRequestStatus.Atribuida)
                request.Status = ServiceRequestStatus.EmDeslocamento;
        }

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group(assignmentId.ToString()).SendAsync("locationUpdated", new
        {
            lat = body.Latitude,
            lon = body.Longitude,
            speedKmh = body.SpeedKmh,
            at = DateTime.UtcNow
        }, ct);

        return NoContent();
    }

    [HttpPost("visits/{assignmentId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid assignmentId, [FromBody] StatusUpdate body, CancellationToken ct)
    {
        var techId = await db.Technicians.Where(t => t.UserId == UserId).Select(t => t.Id).FirstAsync(ct);
        var assignment = await db.VisitAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.TechnicianId == techId, ct);
        if (assignment is null) return NotFound();
        if (body.Status == ServiceRequestStatus.Concluida)
            return BadRequest(new { error = "Para concluir, envie a foto do reparo." });

        await workflow.UpdateVisitStatusAsync(assignmentId, body.Status, ct);
        return NoContent();
    }

    [HttpPost("visits/{assignmentId:guid}/complete")]
    [RequestSizeLimit(12_000_000)]
    public async Task<ActionResult> Complete(Guid assignmentId, IFormFile photo, [FromForm] string? notes, CancellationToken ct)
    {
        if (photo is null || photo.Length == 0)
            return BadRequest(new { error = "Fotografe o reparo para concluir o serviço." });

        var techId = await db.Technicians.Where(t => t.UserId == UserId).Select(t => t.Id).FirstAsync(ct);
        var assignment = await db.VisitAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.TechnicianId == techId, ct);
        if (assignment is null) return NotFound();

        var ext = Path.GetExtension(photo.FileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
            ext = ".jpg";

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", "proofs");
        Directory.CreateDirectory(dir);
        var fileName = $"{assignmentId:N}_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(dir, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
            await photo.CopyToAsync(stream, ct);

        await workflow.CompleteWithProofAsync(assignmentId, $"/uploads/proofs/{fileName}", notes, ct);
        return NoContent();
    }
}
