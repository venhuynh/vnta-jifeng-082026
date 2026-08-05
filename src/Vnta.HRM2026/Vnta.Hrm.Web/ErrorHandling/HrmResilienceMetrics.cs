using System.Diagnostics.Metrics;

namespace Vnta.Hrm.Web.ErrorHandling;

internal sealed class HrmResilienceMetrics : IDisposable
{
    public const string MeterName = "VNTA.HRM.Resilience";

    private readonly Meter meter = new(MeterName);
    private readonly Counter<long> requestFailures;
    private readonly Counter<long> loginUnavailableFailures;
    private readonly Counter<long> readinessFailures;

    public HrmResilienceMetrics()
    {
        requestFailures = meter.CreateCounter<long>("hrm.request.failures");
        loginUnavailableFailures = meter.CreateCounter<long>("hrm.login.unavailable");
        readinessFailures = meter.CreateCounter<long>("hrm.readiness.failures");
    }

    public void RecordRequestFailure(string code, int statusCode) =>
        requestFailures.Add(1, new KeyValuePair<string, object?>("code", code), new KeyValuePair<string, object?>("status_code", statusCode));

    public void RecordLoginUnavailable() => loginUnavailableFailures.Add(1);

    public void RecordReadinessFailure() => readinessFailures.Add(1);

    public void Dispose() => meter.Dispose();
}
