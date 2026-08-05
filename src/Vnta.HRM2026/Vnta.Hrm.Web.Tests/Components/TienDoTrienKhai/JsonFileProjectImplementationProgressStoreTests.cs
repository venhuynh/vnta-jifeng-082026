using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Persistence;
using Vnta.Hrm.Web.Services.TienDoTrienKhai;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Components.TienDoTrienKhai;

public sealed class JsonFileProjectImplementationProgressStoreTests
{
    [Fact]
    public async Task UpdateTaskAsync_PersistsTaskChangesAcrossStoreInstances()
    {
        var contentRoot = Path.Combine(
            Path.GetTempPath(),
            "vnta-hrm-project-implementation-progress-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);

        try
        {
            var environment = new TestHostEnvironment(contentRoot);
            var initialStore = new JsonFileProjectImplementationProgressStore(environment);
            var initialSnapshot = await initialStore.LoadAsync();
            var initialTask = initialSnapshot.Phases
                .SelectMany(phase => phase.Milestones)
                .SelectMany(milestone => milestone.Tasks)
                .First();

            var expectedStartDate = new DateOnly(2026, 8, 4);
            var expectedEndDate = new DateOnly(2026, 8, 11);
            await initialStore.UpdateTaskAsync(new UpdateProjectImplementationTaskRequest(
                initialTask.Id,
                "Công việc đã cập nhật từ DxGrid",
                ProjectImplementationTaskOwner.Jifeng,
                expectedStartDate,
                expectedEndDate,
                ProjectImplementationTaskStatus.InProgress,
                45));

            var reloadedStore = new JsonFileProjectImplementationProgressStore(environment);
            var reloadedSnapshot = await reloadedStore.LoadAsync();
            var persistedTask = reloadedSnapshot.Phases
                .SelectMany(phase => phase.Milestones)
                .SelectMany(milestone => milestone.Tasks)
                .Single(task => task.Id == initialTask.Id);

            Assert.Equal("Công việc đã cập nhật từ DxGrid", persistedTask.WorkItem);
            Assert.Equal(ProjectImplementationTaskOwner.Jifeng, persistedTask.Owner);
            Assert.Equal(expectedStartDate, persistedTask.StartDate);
            Assert.Equal(expectedEndDate, persistedTask.EndDate);
            Assert.Equal(ProjectImplementationTaskStatus.InProgress, persistedTask.Status);
            Assert.Equal(45, persistedTask.CompletionPercent);

            var storagePath = Path.Combine(contentRoot, "App_Data", "project-implementation-progress.json");
            Assert.True(File.Exists(storagePath));
            var persistedJson = await File.ReadAllTextAsync(storagePath);
            Assert.DoesNotContain("\"detailTasks\"", persistedJson);
            Assert.DoesNotContain("\"durationText\"", persistedJson);
        }
        finally
        {
            if(Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }
    }

    private sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";

        public string ApplicationName { get; set; } = nameof(JsonFileProjectImplementationProgressStoreTests);

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
