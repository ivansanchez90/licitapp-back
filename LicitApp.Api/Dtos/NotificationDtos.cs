using LicitApp.Api.Domain;

namespace LicitApp.Api.Dtos;

public record NotificationDto(
    Guid Id,
    string UserId,
    NotificationType Type,
    string Title,
    string Body,
    Guid? SolicitudId,
    Guid? OfertaId,
    bool Read,
    DateTime CreatedAt);
