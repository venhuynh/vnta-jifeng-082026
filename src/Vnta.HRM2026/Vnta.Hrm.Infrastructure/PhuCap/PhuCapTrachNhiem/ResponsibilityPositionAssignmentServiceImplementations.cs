using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

internal sealed class DatabaseResponsibilityPositionAssignmentReadService(ApplicationDbContext dbContext)
    : ResponsibilityPositionAssignmentPersistenceOperations(dbContext),
        IResponsibilityPositionAssignmentReadService,
        IResponsibilityPositionAssignmentExportReadService;

internal sealed class DatabaseResponsibilityPositionAssignmentCommandService(ApplicationDbContext dbContext)
    : ResponsibilityPositionAssignmentPersistenceOperations(dbContext),
        IResponsibilityPositionAssignmentCommandService,
        IResponsibilityPositionAssignmentCopyService;

/// <summary>Compatibility facade for direct legacy callers; feature DI registers focused services instead.</summary>
[Obsolete("Use focused responsibility position assignment contracts.")]
public sealed class DatabaseResponsibilityPositionAssignmentService(ApplicationDbContext dbContext)
    : ResponsibilityPositionAssignmentPersistenceOperations(dbContext),
        IResponsibilityPositionAssignmentReadService,
        IResponsibilityPositionAssignmentCommandService,
        IResponsibilityPositionAssignmentCopyService,
        IResponsibilityPositionAssignmentExportReadService;
