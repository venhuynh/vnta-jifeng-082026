namespace Vnta.Hrm.Application.Integrations.AttendanceGateway;

public sealed record AttendanceGatewayIngestionResult(
    int ReceivedCount,
    int StoredCount,
    int DuplicateCount);
