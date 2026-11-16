namespace Manutencao.Domain.Entities;

public class TechnicianScheduleBlock
{
    public Guid Id { get; set; }
    public Guid TechnicianId { get; set; }
    public TechnicianProfile Technician { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
