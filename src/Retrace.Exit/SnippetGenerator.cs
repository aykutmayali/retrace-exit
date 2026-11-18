namespace Retrace.Exit;

public static class SnippetGenerator
{
    public static void Write(string dir, string backend, ScanResult result)
    {
        File.WriteAllText(Path.Combine(dir, "aspnetcore-Program.cs.txt"), AspNetCore(backend));
        File.WriteAllText(Path.Combine(dir, "iis-netframework-notes.md"), IisNotes(backend));
        File.WriteAllText(Path.Combine(dir, "README.txt"),
            $"Generated for a scan of {result.Root}. Review before applying. These snippets are starters, not a drop-in migration.");
    }

    public static string AspNetCore(string backend)
    {
        var key = backend.Trim().ToLowerInvariant();
        if (key is "grafana" or "otlp" or "signoz")
        {
            return """
// ASP.NET Core — OTLP exporter (Grafana / SigNoz / generic Collector)
// Add packages:
//   OpenTelemetry.Extensions.Hosting
//   OpenTelemetry.Instrumentation.AspNetCore
//   OpenTelemetry.Instrumentation.Http
//   OpenTelemetry.Exporter.OpenTelemetryProtocol
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "replace-with-app-name"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
// Set OTEL_EXPORTER_OTLP_ENDPOINT (and headers) in the environment, not in source.
var app = builder.Build();
app.Run();
""";
        }

        return """
// ASP.NET Core — Microsoft OpenTelemetry Distro → Azure Monitor
// Official: https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable
//   dotnet add package Microsoft.OpenTelemetry
// Do not initialize this together with the older Azure Monitor OpenTelemetry Distro.
using Microsoft.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
builder.UseMicrosoftOpenTelemetry(options =>
{
    options.Exporters = ExportTarget.AzureMonitor;
});
// Set APPLICATIONINSIGHTS_CONNECTION_STRING in the environment before startup.
var app = builder.Build();
app.Run();
""";
    }

    public static string IisNotes(string backend)
    {
        var guid = Scanner.ProfilerGuid;
        return $$"""
# IIS / .NET Framework 4.6.2+ (zero-code)

Source: https://opentelemetry.io/docs/zero-code/dotnet/

Requires Windows PowerShell 5.1 (Desktop). `Register-OpenTelemetryForIIS` restarts IIS.

```powershell
Import-Module "OpenTelemetry.DotNet.Auto.psm1"
Install-OpenTelemetryCore
Register-OpenTelemetryForIIS
```

Set the common `OTEL_*` values in `Web.config` `appSettings` (for example `OTEL_SERVICE_NAME`).
Target backend hint from the scan: `{{backend}}`.

Then restart IIS (`iisreset.exe`) after config changes. Keep the Retrace agent running until traces appear in the new backend, then uninstall the Stackify agent and remove profiler environment variables (`CORECLR_PROFILER={{{guid}}}`).
""";
    }
}
