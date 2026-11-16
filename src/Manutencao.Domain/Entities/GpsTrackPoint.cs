namespace Manutencao.Domain.Entities;

public class GpsTrackPoint
{
    public Guid Id { get; set; }
    public Guid VisitAssignmentId { get; set; }
    public VisitAssignment VisitAssignment { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? SpeedKmh { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
