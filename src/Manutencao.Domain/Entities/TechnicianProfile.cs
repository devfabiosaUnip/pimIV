namespace Manutencao.Domain.Entities;

public class TechnicianProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserAccount User { get; set; } = null!;

    public string EmployeeCode { get; set; } = string.Empty;
    public string Specialties { get; set; } = string.Empty;
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public DateTime? LastLocationAt { get; set; }
    public int MaxDailyVisits { get; set; } = 8;

    public ICollection<TechnicianScheduleBlock> ScheduleBlocks { get; set; } = new List<TechnicianScheduleBlock>();
    public ICollection<VisitAssignment> Assignments { get; set; } = new List<VisitAssignment>();
}
