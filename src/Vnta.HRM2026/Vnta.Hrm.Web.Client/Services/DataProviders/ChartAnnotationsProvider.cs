using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public class ChartAnnotationsProvider(Action onChanged) {
    public List<Annotation> ChartAnnotations { get; } = [];

    public void Notify() {
        onChanged();
    }
}

