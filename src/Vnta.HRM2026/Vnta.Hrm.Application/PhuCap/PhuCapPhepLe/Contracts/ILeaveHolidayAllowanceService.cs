namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

/// <summary>
/// Backward-compatible facade for consumers that still need both read and command operations.
/// New consumers should depend on the narrower contracts. This facade is retained only
/// until the external-consumer retirement review completes.
/// </summary>
[Obsolete("Compatibility facade; use capability-specific contracts instead. Remove after legacy consumers are retired.")]
public interface ILeaveHolidayAllowanceService :
    ILeaveHolidayAllowanceReadService,
    ILeaveHolidayAllowanceCommandService;
