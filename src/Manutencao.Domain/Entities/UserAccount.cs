using Manutencao.Domain.Enums;

namespace Manutencao.Domain.Entities;

public class UserAccount
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TechnicianProfile? TechnicianProfile { get; set; }
    public ICollection<ServiceRequest> ClientRequests { get; set; } = new List<ServiceRequest>();
}
