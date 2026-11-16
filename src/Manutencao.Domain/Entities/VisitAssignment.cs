using Manutencao.Domain.Enums;

namespace Manutencao.Domain.Entities;

public class VisitAssignment
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Guid TechnicianId { get; set; }
    public TechnicianProfile Technician { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid AssignedByUserId { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public ServiceRequestStatus VisitStatus { get; set; } = ServiceRequestStatus.Atribuida;
    public string? OperatorNotes { get; set; }
    public string? CompletionNotes { get; set; }
    public string? CompletionPhotoPath { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<GpsTrackPoint> TrackPoints { get; set; } = new List<GpsTrackPoint>();
}
