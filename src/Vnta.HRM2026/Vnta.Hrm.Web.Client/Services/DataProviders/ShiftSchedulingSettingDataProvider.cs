using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class ShiftSchedulingSettingDataProvider(
    IShiftSchedulingSettingService shiftSchedulingSettingService)
{
    public Task<IReadOnlyList<ShiftSchedulingSettingRecord>> GetAsync(
        CancellationToken cancellationToken = default) =>
        GetFromServiceAsync(cancellationToken);

    public Task<string?> ValidateAsync(
        ShiftSchedulingSettingRecord setting,
        CancellationToken cancellationToken = default) =>
        shiftSchedulingSettingService.ValidateAsync(MapRequest(setting), cancellationToken);

    public async Task<IReadOnlyList<ShiftSchedulingSettingRecord>> SaveAsync(
        ShiftSchedulingSettingRecord setting,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        await shiftSchedulingSettingService.SaveAsync(MapRequest(setting), isNew, cancellationToken);
        return await GetFromServiceAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftSchedulingSettingRecord>> DeleteAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idSet = ids.Distinct().ToArray();
        await shiftSchedulingSettingService.DeleteAsync(idSet, cancellationToken);
        return await GetFromServiceAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ShiftSchedulingSettingRecord>> GetFromServiceAsync(
        CancellationToken cancellationToken)
    {
        var rows = await shiftSchedulingSettingService.GetAsync(cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    private static UpsertShiftSchedulingSettingRequest MapRequest(ShiftSchedulingSettingRecord source) =>
        new()
        {
            Id = source.Id,
            ShiftId = source.ShiftId,
            ClassificationType = (int)source.ClassificationType,
            Value = source.Value,
            AssignmentScopeMode = (int)source.AssignmentScopeMode,
            EffectiveFromDate = source.EffectiveFromDate.HasValue
                ? DateOnly.FromDateTime(source.EffectiveFromDate.Value.Date)
                : null,
            EffectiveToDate = source.EffectiveToDate.HasValue
                ? DateOnly.FromDateTime(source.EffectiveToDate.Value.Date)
                : null,
            IsActive = source.IsActive,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static ShiftSchedulingSettingRecord MapRecord(ShiftSchedulingSettingListItemDto source) =>
        new()
        {
            Id = source.Id,
            ShiftId = source.ShiftId,
            ShiftCode = source.ShiftCode,
            ShiftName = source.ShiftName,
            ShiftStartTime = source.ShiftStartTime,
            ShiftEndTime = source.ShiftEndTime,
            ClassificationType = (ShiftSchedulingClassificationType)source.ClassificationType,
            Value = source.Value,
            AssignmentScopeMode = (ShiftSchedulingAssignmentScopeMode)source.AssignmentScopeMode,
            EffectiveFromDate = source.EffectiveFromDate?.ToDateTime(TimeOnly.MinValue),
            EffectiveToDate = source.EffectiveToDate?.ToDateTime(TimeOnly.MinValue),
            IsActive = source.IsActive,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
