using Manutencao.Domain.Enums;

namespace Manutencao.Application.Dto;

public record CreateServiceRequestDto(
    string Title,
    string Description,
    string Category,
    VisitPriority Priority,
    string AddressLine,
    string City,
    string State,
    string ZipCode,
    double Latitude,
    double Longitude);

public record ServiceRequestSummaryDto(
    Guid Id,
    string Protocol,
    string Title,
    string Description,
    ServiceRequestStatus Status,
    VisitPriority Priority,
    string AddressLine,
    double Latitude,
    double Longitude,
    DateTime CreatedAt);

public record TechnicianVisitDto(
    Guid AssignmentId,
    Guid ServiceRequestId,
    string Protocol,
    string Title,
    string Description,
    string AddressLine,
    string City,
    double Latitude,
    double Longitude,
    ServiceRequestStatus VisitStatus,
    DateTime? ScheduledFor);

public record ClientRequestDetailDto(
    Guid Id,
    Guid? AssignmentId,
    string Protocol,
    string Title,
    string Description,
    ServiceRequestStatus Status,
    VisitPriority Priority,
    string AddressLine,
    string City,
    double Latitude,
    double Longitude,
    DateTime CreatedAt,
    string? TechnicianName,
    double? TechnicianLatitude,
    double? TechnicianLongitude,
    DateTime? TechnicianLocationAt,
    string? ProofPhotoUrl,
    string? CompletionNotes,
    int? FeedbackRating,
    string? FeedbackComment);

public record TechnicianTrackingDto(
    Guid RequestId,
    Guid? AssignmentId,
    ServiceRequestStatus Status,
    bool TrackingAvailable,
    string? TechnicianName,
    double DestinationLatitude,
    double DestinationLongitude,
    double? TechnicianLatitude,
    double? TechnicianLongitude,
    DateTime? TechnicianLocationAt,
    double? DistanceKm,
    double? EtaMinutes);

public record SubmitFeedbackDto(int Rating, string? Comment);
