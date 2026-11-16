namespace Manutencao.Application.Dto;

public record DispatchRecommendationDto(
    Guid TechnicianId,
    string TechnicianName,
    string EmployeeCode,
    string Specialties,
    double Score,
    double DistanceKm,
    int ActiveVisitsToday,
    int VisitsRemainingToday,
    bool InScheduleNow,
    double? EtaMinutes,
    string Rationale);
