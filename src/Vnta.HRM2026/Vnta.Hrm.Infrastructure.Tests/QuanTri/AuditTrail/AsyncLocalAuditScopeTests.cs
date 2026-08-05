using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.QuanTri.AuditTrail;

public sealed class AsyncLocalAuditScopeTests
{
    [Fact]
    public void Nested_scopes_restore_the_refined_outer_command_when_the_inner_scope_ends()
    {
        var scope = new AsyncLocalAuditScope();
        var outerCommand = CreateCommand(AuditActions.Shift.Save);
        var innerCommand = CreateCommand(AuditActions.WorkCalendarDay.Delete);

        using (scope.Begin(outerCommand))
        {
            scope.RefineAction(AuditActions.Shift.Updated);
            Assert.Equal(AuditActions.Shift.Updated, scope.Current?.ActionIntent);

            using (scope.Begin(innerCommand))
            {
                scope.RefineAction(AuditActions.WorkCalendarDay.Deleted);
                Assert.Equal(AuditActions.WorkCalendarDay.Deleted, scope.Current?.ActionIntent);
                Assert.Equal(innerCommand.OperationId, scope.Current?.OperationId);
            }

            Assert.Equal(AuditActions.Shift.Updated, scope.Current?.ActionIntent);
            Assert.Equal(outerCommand.OperationId, scope.Current?.OperationId);
        }

        Assert.Null(scope.Current);
    }

    [Fact]
    public void Begin_rejects_invalid_commands_and_refinement_requires_a_valid_active_scope()
    {
        var scope = new AsyncLocalAuditScope();
        var command = CreateCommand(AuditActions.Shift.Save);

        Assert.Throws<InvalidOperationException>(() => scope.RefineAction(AuditActions.Shift.Updated));
        Assert.Throws<ArgumentException>(() => scope.Begin(command with { OperationId = Guid.Empty }));
        Assert.Throws<ArgumentException>(() => scope.Begin(command with { EventKey = "retry-1" }));
        Assert.Throws<ArgumentException>(() => scope.Begin(command with
        {
            CaptureMode = AuditCaptureMode.OperationOnly,
            EventKey = " "
        }));

        using (scope.Begin(command))
        {
            Assert.Throws<ArgumentException>(() => scope.RefineAction(" "));
            Assert.Throws<ArgumentOutOfRangeException>(() => scope.RefineAction(new string('a', 101)));
        }
    }

    [Fact]
    public void Out_of_order_dispose_keeps_the_outer_lease_recoverable()
    {
        var scope = new AsyncLocalAuditScope();
        var outer = scope.Begin(CreateCommand(AuditActions.Shift.Save));
        var inner = scope.Begin(CreateCommand(AuditActions.WorkCalendarDay.Delete));

        Assert.Throws<InvalidOperationException>(() => outer.Dispose());
        Assert.Equal(AuditActions.WorkCalendarDay.Delete, scope.Current?.ActionIntent);

        inner.Dispose();
        Assert.Equal(AuditActions.Shift.Save, scope.Current?.ActionIntent);

        outer.Dispose();
        Assert.Null(scope.Current);
    }

    [Fact]
    public void Correlation_scopes_restore_the_parent_value_after_a_nested_scope_ends()
    {
        var scope = new AsyncLocalAuditCorrelationScope();

        using (scope.Begin("outer-correlation"))
        {
            Assert.Equal("outer-correlation", scope.Current);

            using (scope.Begin("inner-correlation"))
            {
                Assert.Equal("inner-correlation", scope.Current);
            }

            Assert.Equal("outer-correlation", scope.Current);
        }

        Assert.Null(scope.Current);
    }

    private static AuditCommand CreateCommand(string action) =>
        new(
            Guid.NewGuid(),
            action,
            new AuditActor("test-user", "Test User", AuditActorKind.User, AuditSource.InteractiveServer),
            "scope-test-correlation");
}
