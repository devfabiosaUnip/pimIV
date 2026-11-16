using Manutencao.Application.Abstractions;
using Manutencao.Application.Dto;
using Manutencao.Domain.Entities;
using Manutencao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Application.Services;

public class ServiceRequestWorkflow(IAppDbContext db)
{
    public async Task<ServiceRequest> CreateAsync(Guid clientId, CreateServiceRequestDto dto, CancellationToken ct = default)
    {
        var protocol = $"SR-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var entity = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            Protocol = protocol,
            ClientId = clientId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Priority = dto.Priority,
            AddressLine = dto.AddressLine,
            City = dto.City,
            State = dto.State,
            ZipCode = dto.ZipCode,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Status = ServiceRequestStatus.PendenteAprovacao
        };
        db.ServiceRequests.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task ApproveAsync(Guid requestId, Guid operatorId, CancellationToken ct = default)
    {
        var request = await db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("Requisição não encontrada.");

        request.Status = ServiceRequestStatus.AguardandoAtribuicao;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApprovedByUserId = operatorId;
        await db.SaveChangesAsync(ct);
    }

    public async Task AssignAsync(Guid requestId, Guid technicianId, Guid operatorId, string? notes, CancellationToken ct = default)
    {
        var request = await db.ServiceRequests
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("Requisição não encontrada.");

        if (request.Assignment is not null)
            throw new InvalidOperationException("Requisição já atribuída.");

        request.Status = ServiceRequestStatus.Atribuida;
        db.VisitAssignments.Add(new VisitAssignment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = requestId,
            TechnicianId = technicianId,
            AssignedByUserId = operatorId,
            OperatorNotes = notes,
            VisitStatus = ServiceRequestStatus.Atribuida,
            ScheduledFor = DateTime.UtcNow.AddHours(2)
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateVisitStatusAsync(Guid assignmentId, ServiceRequestStatus status, CancellationToken ct = default)
    {
        var assignment = await db.VisitAssignments
            .Include(a => a.ServiceRequest)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new InvalidOperationException("Visita não encontrada.");

        assignment.VisitStatus = status;
        assignment.ServiceRequest.Status = status;
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteWithProofAsync(Guid assignmentId, string photoPath, string? notes, CancellationToken ct = default)
    {
        var assignment = await db.VisitAssignments
            .Include(a => a.ServiceRequest)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new InvalidOperationException("Visita não encontrada.");

        assignment.CompletionPhotoPath = photoPath;
        assignment.CompletionNotes = notes;
        assignment.CompletedAt = DateTime.UtcNow;
        assignment.VisitStatus = ServiceRequestStatus.Concluida;
        assignment.ServiceRequest.Status = ServiceRequestStatus.Concluida;
        await db.SaveChangesAsync(ct);
    }

    public async Task SubmitFeedbackAsync(Guid requestId, Guid clientId, int rating, string? comment, CancellationToken ct = default)
    {
        if (rating is < 1 or > 5)
            throw new InvalidOperationException("A avaliação deve ser entre 1 e 5.");

        var request = await db.ServiceRequests
            .Include(r => r.Feedback)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ClientId == clientId, ct)
            ?? throw new InvalidOperationException("Requisição não encontrada.");

        if (request.Status != ServiceRequestStatus.Concluida)
            throw new InvalidOperationException("Só é possível avaliar após a conclusão do serviço.");

        if (request.Feedback is not null)
            throw new InvalidOperationException("Esta visita já foi avaliada.");

        db.ClientFeedbacks.Add(new ClientFeedback
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = requestId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
