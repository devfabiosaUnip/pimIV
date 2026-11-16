using Manutencao.Domain.Enums;

namespace Manutencao.Domain.Entities;

public class ServiceRequest
{
    public Guid Id { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public UserAccount Client { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public VisitPriority Priority { get; set; } = VisitPriority.Normal;
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.PendenteAprovacao;

    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public VisitAssignment? Assignment { get; set; }
    public ClientFeedback? Feedback { get; set; }
}
