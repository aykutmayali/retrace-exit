# Retrace Exit report

Scanned: `/workspace/money-ideas/retrace-exit/scanner/samples`
Backend hint: `azure-monitor`
Findings: 24 (15 critical, 9 warning, 0 info)

Stackify Retrace reaches end of life on **31 March 2027**. After that date you cannot log in, ingest data, or read stored data. There is no automated conversion — Retrace agents and profilers have to be replaced. Official letter: https://docs.stackify.com/docs/stackify-end-of-life-customer-letter

This report is an inventory, not a rewrite of your code. API keys and connection strings are never copied into the report.

## Summary

| Severity | Kind | Path | Line | Detail |
| --- | --- | --- | --- | --- |
| critical | appsettings | `aspnetcore-shop/appsettings.json` | 2 | Stackify configuration section found. Remove ApiKey after cut-over (the value was not copied into this report). Move AppName and Environment to OTEL resource attributes or APPLICATIONINSIGHTS_CONNECTION_STRING in the environment / Key Vault. An ApiKey value is present and was not copied into this report. |
| critical | profiler-guid | `aspnetcore-shop/Dockerfile` | 2 | Retrace .NET profiler GUID found. Replace CORECLR_PROFILER / COR_PROFILER with OpenTelemetry auto-instrumentation variables and remove StackifyProfiler.so / the Retrace agent after the new path is proven. |
| critical | nuget | `aspnetcore-shop/Sample.AspNetCoreShop.csproj` | 8 | Package 'StackifyLib' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | nuget | `aspnetcore-shop/Sample.AspNetCoreShop.csproj` | 9 | Package 'StackifyMiddleware' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | nuget | `aspnetcore-shop/Sample.AspNetCoreShop.csproj` | 10 | Package 'Serilog.Sinks.Stackify' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | nuget | `aspnetcore-shop/Sample.AspNetCoreShop.csproj` | 11 | Package 'StackifyLib.AspNetCore' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | stackify-json | `aspnetcore-shop/Stackify.json` |  | Stackify.json found. Map AppName/Environment to OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES; migrate Instrumentation[] entries to ActivitySource or additional OTel sources. AppName=Sample.AspNetCoreShop Environment=Production CaptureAllExceptions=True (OpenTelemetry records unhandled exceptions by default; first-chance exceptions need manual spans). Instrumentation entries: 1 (custom methods / StartTrace operations). |
| critical | nuget | `netframework-iis/packages.config` | 3 | Package 'StackifyLib' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | nuget | `netframework-iis/packages.config` | 4 | Package 'StackifyLib.log4net' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | nuget | `netframework-iis/Sample.LegacyIis.csproj` | 13 | Package 'StackifyLib' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | nuget | `netframework-iis/Sample.LegacyIis.csproj` | 16 | Package 'StackifyLib.log4net' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place. |
| critical | xml-config | `netframework-iis/Web.config` | 4 | Config contains 'Stackify.ApiKey'. The key value was not copied into this report. After cut-over, delete it and rotate the key if it was ever committed. An ApiKey value is present and was not copied into this report. |
| critical | xml-config | `netframework-iis/Web.config` | 5 | Config contains 'Stackify.AppName'. Map AppName/Environment to OTEL_SERVICE_NAME / OTEL_RESOURCE_ATTRIBUTES, or replace the appender/target with an OpenTelemetry log bridge. An ApiKey value is present and was not copied into this report. |
| critical | xml-config | `netframework-iis/Web.config` | 6 | Config contains 'Stackify.Environment'. Map AppName/Environment to OTEL_SERVICE_NAME / OTEL_RESOURCE_ATTRIBUTES, or replace the appender/target with an OpenTelemetry log bridge. An ApiKey value is present and was not copied into this report. |
| critical | xml-config | `netframework-iis/Web.config` | 11 | Config contains 'StackifyAppender'. Map AppName/Environment to OTEL_SERVICE_NAME / OTEL_RESOURCE_ATTRIBUTES, or replace the appender/target with an OpenTelemetry log bridge. An ApiKey value is present and was not copied into this report. |
| warning | code | `aspnetcore-shop/Program.cs` | 6 | Found 'WriteTo.Stackify'. Replace the Serilog Stackify sink with Serilog.Sinks.OpenTelemetry or a backend-specific sink. Set the endpoint in the environment, not in source. |
| warning | code | `aspnetcore-shop/Program.cs` | 10 | Found 'RequestTracerMiddleware'. Remove StackifyMiddleware.RequestTracerMiddleware and use OpenTelemetry.Instrumentation.AspNetCore, zero-code auto-instrumentation, or the Microsoft OpenTelemetry Distro. |
| warning | code | `aspnetcore-shop/Program.cs` | 11 | Found 'ConfigureStackifyLogging'. Remove app.ConfigureStackifyLogging and configure a Serilog/NLog/log4net OpenTelemetry sink or the Microsoft.Extensions.Logging bridge. |
| warning | code | `aspnetcore-shop/Program.cs` | 15 | Found 'ProfileTracer.CreateAsOperation'. Replace with ActivitySource.StartActivity (or Activity.Current) and use W3C trace context instead of Trace.CorrelationManager.ActivityId. |
| warning | code | `aspnetcore-shop/Program.cs` | 16 | Found 'Metrics.Count' (StackifyLib.Metrics). Replace Count/Average/Sum/Time/SetGauge/IncrementGauge with System.Diagnostics.Metrics (Meter, Counter, Histogram, ObservableGauge). |
| warning | code | `netframework-iis/CheckoutService.cs` | 11 | Found 'ProfileTracer.CreateAsOperation'. Replace with ActivitySource.StartActivity (or Activity.Current) and use W3C trace context instead of Trace.CorrelationManager.ActivityId. |
| warning | code | `netframework-iis/CheckoutService.cs` | 14 | Found 'Metrics.Count' (StackifyLib.Metrics). Replace Count/Average/Sum/Time/SetGauge/IncrementGauge with System.Diagnostics.Metrics (Meter, Counter, Histogram, ObservableGauge). |
| warning | code | `netframework-iis/CheckoutService.cs` | 15 | Found 'Metrics.Time' (StackifyLib.Metrics). Replace Count/Average/Sum/Time/SetGauge/IncrementGauge with System.Diagnostics.Metrics (Meter, Counter, Histogram, ObservableGauge). |
| warning | code | `netframework-iis/Web.config` | 11 | Found 'StackifyAppender'. Replace the log4net Stackify appender with an OpenTelemetry log bridge and validate correlation. |

## Suggested next steps

1. Export Retrace logs (100,000 entries or 50 MB per file) and dashboard PDFs before 31 March 2027. Alert rules have no documented export — write them down.
2. Pilot OpenTelemetry on one service and keep Retrace running until the new path is proven, then cut over in waves.
3. Map custom `ProfileTracer.CreateAsOperation` calls to `ActivitySource` and `StackifyLib.Metrics` to `System.Diagnostics.Metrics`.
4. OpenTelemetry auto-instrumentation does not copy method-level call trees, first-chance exceptions, or query-parameter capture — see BMC's official comparison in the Helix/OTel customer guide.
5. For IIS / .NET Framework 4.6.2+, use `Register-OpenTelemetryForIIS`. For ASP.NET Core to Azure Monitor, use the Microsoft OpenTelemetry Distro (`Microsoft.OpenTelemetry`). Do not initialize that distro together with the older Azure Monitor OpenTelemetry Distro.

## OpenTelemetry starter snippets

The snippets below match the `--backend azure-monitor` hint. They are starting points, not a drop-in migration.

### ASP.NET Core

```csharp
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
```

### IIS / .NET Framework

# IIS / .NET Framework 4.6.2+ (zero-code)

Source: https://opentelemetry.io/docs/zero-code/dotnet/

Requires Windows PowerShell 5.1 (Desktop). `Register-OpenTelemetryForIIS` restarts IIS.

```powershell
Import-Module "OpenTelemetry.DotNet.Auto.psm1"
Install-OpenTelemetryCore
Register-OpenTelemetryForIIS
```

Set the common `OTEL_*` values in `Web.config` `appSettings` (for example `OTEL_SERVICE_NAME`).
Target backend hint from the scan: `azure-monitor`.

Then restart IIS (`iisreset.exe`) after config changes. Keep the Retrace agent running until traces appear in the new backend, then uninstall the Stackify agent and remove profiler environment variables (`CORECLR_PROFILER={cf0d821e-299b-5307-a3d8-b283c03916da}`).