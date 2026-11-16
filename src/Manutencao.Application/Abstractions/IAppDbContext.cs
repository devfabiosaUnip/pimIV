using Manutencao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<UserAccount> Users { get; }
    DbSet<TechnicianProfile> Technicians { get; }
    DbSet<TechnicianScheduleBlock> ScheduleBlocks { get; }
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<VisitAssignment> VisitAssignments { get; }
    DbSet<GpsTrackPoint> GpsTrackPoints { get; }
    DbSet<ClientFeedback> ClientFeedbacks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
