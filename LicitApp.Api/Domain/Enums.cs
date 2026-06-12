namespace LicitApp.Api.Domain;

public enum UserRole
{
    constructor,
    corralon
}

public enum SolicitudStatus
{
    OPEN,
    CLOSED,
    CANCELLED,
    EXPIRED
}

public enum ShippingType
{
    FREE,
    CHARGED,
    FIXED_PRICE
}

public enum OfertaStatus
{
    ACTIVE,
    WON,
    LOST,
    EXPIRED,
    WITHDRAWN
}

public enum NotificationType
{
    NEW_OFFER,
    DEADLINE_NEAR,
    OFFER_WON,
    OFFER_LOST,
    NEW_REQUEST
}
