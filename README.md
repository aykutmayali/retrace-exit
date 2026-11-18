# Retrace.Exit

A `dotnet tool` that scans a .NET solution for [Stackify Retrace](https://stackify.com/retrace/) usage and writes an OpenTelemetry migration report.

BMC Helix is retiring Retrace on **31 March 2027**. After that date you cannot log in, ingest data, or read stored data, and there is no automated conversion off Retrace's agents and profilers. Official announcement: [Stackify Retrace End of Life letter](https://docs.stackify.com/docs/stackify-end-of-life-customer-letter).

Retrace.Exit inventories what is in your repo so you can plan the move. It does **not** rewrite your code, it does **not** send data over the network, and it **never prints API keys**.

## Install

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (or a later major, via roll-forward).

```bash
dotnet tool install -g Retrace.Exit
```

From a local pack (this repository):

```bash
dotnet pack src/Retrace.Exit/Retrace.Exit.csproj -c Release
dotnet tool install -g Retrace.Exit --add-source ./nupkg
```

## Usage

```bash
retrace-exit ./src
retrace-exit ./src --format markdown --out retrace-exit-report.md
retrace-exit ./src --format html --out retrace-exit-report.html
retrace-exit ./src --backend azure-monitor --emit-snippets ./otel-snippets
retrace-exit --help
```

| Option | Meaning |
| --- | --- |
| `path` | Directory to scan (default: current directory). `bin`, `obj`, `.git`, `node_modules`, `.vs`, `packages`, and `nupkg` are skipped. |
| `--format markdown|html` | Report format (default: `markdown`). |
| `--out <file>` | Write the report to a file instead of stdout. |
| `--backend azure-monitor|grafana|otlp|signoz` | Chooses which starter snippets to include (default: `azure-monitor`). |
| `--emit-snippets <dir>` | Also write starter snippet files into a directory. |

## What it looks for

- NuGet / `packages.config` references (`StackifyLib`, `StackifyMiddleware`, `Serilog.Sinks.Stackify`, `NLog.Targets.Stackify`, `StackifyLib.log4net`, `Stackify.AzureWebApps`, and other official packages)
- `Stackify.json` (AppName, Environment, `Instrumentation[]`, `CaptureAllExceptions`)
- `appsettings*.json` `Stackify` sections and `Web.config` / `App.config` keys (`Stackify.ApiKey`, `Stackify.AppName`, `Stackify.Environment`)
- Log appenders and sinks (`WriteTo.Stackify()`, `StackifyAppender`, `StackifyTarget`)
- Code: `ProfileTracer.CreateAsOperation`, `StackifyLib.Metrics`, `RequestTracerMiddleware`, `ConfigureStackifyLogging`, `StackifyHttpModule`
- The Retrace profiler GUID (`cf0d821e-299b-5307-a3d8-b283c03916da`) in Dockerfiles, Compose, and environment files

The report includes a findings table, next-step checklist, and starter OpenTelemetry snippets:

- ASP.NET Core → [Microsoft OpenTelemetry Distro](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable) for Azure Monitor, or an OTLP exporter for Grafana / SigNoz
- IIS / .NET Framework 4.6.2+ → [OpenTelemetry .NET zero-code instrumentation](https://opentelemetry.io/docs/zero-code/dotnet/) (`Register-OpenTelemetryForIIS`)

OpenTelemetry auto-instrumentation is **not** a like-for-like copy of the Retrace profiler (method-level call trees, first-chance exceptions, and query-parameter capture differ). The official comparison is in BMC's [Helix AIOps with OpenTelemetry guide](https://docs.stackify.com/docs/helix-aiops-with-opentelemetry-a-guide-for-stackify-retrace-customers).

## Sample output

Against the fixtures in [`samples/`](samples/):

```text
# Retrace Exit report

Scanned: `./samples`
Backend hint: `azure-monitor`
Findings: 24 (15 critical, 9 warning, 0 info)

Stackify Retrace reaches end of life on **31 March 2027**. ...

## Summary

| Severity | Kind | Path | Line | Detail |
| --- | --- | --- | --- | --- |
| critical | nuget | `aspnetcore-shop/Sample.AspNetCoreShop.csproj` | 8 | Package 'StackifyMiddleware' is referenced. ... |
| critical | stackify-json | `aspnetcore-shop/Stackify.json` |  | Stackify.json found. ... AppName=Sample.AspNetCoreShop ... |
| critical | xml-config | `netframework-iis/Web.config` | 4 | Config contains 'Stackify.ApiKey'. The key value was not copied into this report. ... |
| warning | code | `netframework-iis/CheckoutService.cs` | 12 | Found 'ProfileTracer.CreateAsOperation'. Replace with ActivitySource.StartActivity ... |
```

The full report then appends suggested next steps and copy-paste snippets for the chosen backend. API key values such as those in the samples are never written into the report.

## Build and test

```bash
dotnet test Retrace.Exit.slnx
dotnet pack src/Retrace.Exit/Retrace.Exit.csproj -c Release
```

The sample projects under `samples/` are scan fixtures only; they are not built (they intentionally reference Stackify packages and contain a fake API key).

## Need help migrating?

If you want a hands-on move off Retrace for ASP.NET Core or IIS / .NET Framework — inventory, OpenTelemetry pilot, Azure Monitor or Grafana landing, then removal of the Retrace agents — book a 15-minute fit check:

- https://cal.com/aykut-ovvudn/15min
- https://aykutmayali.github.io/launch-audit/

## License

MIT © 2026 Aykut Mayali. See [LICENSE](LICENSE).
