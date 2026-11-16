using Manutencao.Application.Abstractions;
using Manutencao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<TechnicianProfile> Technicians => Set<TechnicianProfile>();
    public DbSet<TechnicianScheduleBlock> ScheduleBlocks => Set<TechnicianScheduleBlock>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<VisitAssignment> VisitAssignments => Set<VisitAssignment>();
    public DbSet<GpsTrackPoint> GpsTrackPoints => Set<GpsTrackPoint>();
    public DbSet<ClientFeedback> ClientFeedbacks => Set<ClientFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<TechnicianProfile>(e =>
        {
            e.HasOne(x => x.User).WithOne(x => x.TechnicianProfile)
                .HasForeignKey<TechnicianProfile>(x => x.UserId);
        });

        modelBuilder.Entity<ServiceRequest>(e =>
        {
            e.HasIndex(x => x.Protocol).IsUnique();
            e.HasOne(x => x.Assignment).WithOne(x => x.ServiceRequest)
                .HasForeignKey<VisitAssignment>(x => x.ServiceRequestId);
            e.HasOne(x => x.Feedback).WithOne(x => x.ServiceRequest)
                .HasForeignKey<ClientFeedback>(x => x.ServiceRequestId);
        });

        modelBuilder.Entity<ClientFeedback>(e =>
        {
            e.HasIndex(x => x.ServiceRequestId).IsUnique();
        });
    }
}
