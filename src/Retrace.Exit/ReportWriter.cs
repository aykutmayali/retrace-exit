using System.Net;
using System.Text;

namespace Retrace.Exit;

public static class ReportWriter
{
    public static string Render(ScanResult result, string format, string backend)
    {
        var md = RenderMarkdown(result, backend);
        if (format.Equals("html", StringComparison.OrdinalIgnoreCase))
            return RenderHtml(result, backend);
        return md;
    }

    public static string RenderMarkdown(ScanResult result, string backend)
    {
        var findings = result.Findings;
        var critical = findings.Count(f => f.Severity == "critical");
        var warning = findings.Count(f => f.Severity == "warning");
        var info = findings.Count(f => f.Severity == "info");
        var rows = findings.Count == 0
            ? "| — | — | — | — | No Stackify Retrace traces were found in this directory. |"
            : string.Join("\n", findings.Select(f =>
                $"| {f.Severity} | {f.Kind} | `{f.Path}` | {f.Line?.ToString() ?? ""} | {Escape(f.Detail)} |"));

        return $"""
# Retrace Exit report

Scanned: `{result.Root}`
Backend hint: `{backend}`
Findings: {findings.Count} ({critical} critical, {warning} warning, {info} info)

Stackify Retrace reaches end of life on **31 March 2027**. After that date you cannot log in, ingest data, or read stored data. There is no automated conversion — Retrace agents and profilers have to be replaced. Official letter: https://docs.stackify.com/docs/stackify-end-of-life-customer-letter

This report is an inventory, not a rewrite of your code. API keys and connection strings are never copied into the report.

## Summary

| Severity | Kind | Path | Line | Detail |
| --- | --- | --- | --- | --- |
{rows}

## Suggested next steps

1. Export Retrace logs (100,000 entries or 50 MB per file) and dashboard PDFs before 31 March 2027. Alert rules have no documented export — write them down.
2. Pilot OpenTelemetry on one service and keep Retrace running until the new path is proven, then cut over in waves.
3. Map custom `ProfileTracer.CreateAsOperation` calls to `ActivitySource` and `StackifyLib.Metrics` to `System.Diagnostics.Metrics`.
4. OpenTelemetry auto-instrumentation does not copy method-level call trees, first-chance exceptions, or query-parameter capture — see BMC's official comparison in the Helix/OTel customer guide.
5. For IIS / .NET Framework 4.6.2+, use `Register-OpenTelemetryForIIS`. For ASP.NET Core to Azure Monitor, use the Microsoft OpenTelemetry Distro (`Microsoft.OpenTelemetry`). Do not initialize that distro together with the older Azure Monitor OpenTelemetry Distro.

## OpenTelemetry starter snippets

The snippets below match the `--backend {backend}` hint. They are starting points, not a drop-in migration.

### ASP.NET Core

```csharp
{SnippetGenerator.AspNetCore(backend)}
```

### IIS / .NET Framework

{SnippetGenerator.IisNotes(backend)}
""";
    }

    public static string RenderHtml(ScanResult result, string backend)
    {
        var findings = result.Findings;
        var critical = findings.Count(f => f.Severity == "critical");
        var warning = findings.Count(f => f.Severity == "warning");
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<title>Retrace Exit report</title>");
        sb.AppendLine("<style>body{font-family:system-ui,sans-serif;max-width:960px;margin:2rem auto;padding:0 1rem;line-height:1.45;color:#1b1b1b}table{border-collapse:collapse;width:100%;font-size:0.95rem}th,td{border:1px solid #ccc;padding:0.4rem 0.55rem;text-align:left;vertical-align:top}th{background:#f4f4f4}code,pre{font-family:ui-monospace,monospace}pre{background:#f7f7f7;padding:1rem;overflow:auto}.critical{color:#8b0000}.warning{color:#8a5a00}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>Retrace Exit report</h1>");
        sb.AppendLine($"<p>Scanned: <code>{Enc(result.Root)}</code><br>Backend hint: <code>{Enc(backend)}</code><br>Findings: {findings.Count} ({critical} critical, {warning} warning)</p>");
        sb.AppendLine("<p>Stackify Retrace reaches end of life on <strong>31 March 2027</strong>. After that date you cannot log in, ingest data, or read stored data. There is no automated conversion. Official letter: <a href=\"https://docs.stackify.com/docs/stackify-end-of-life-customer-letter\">BMC Helix EOL announcement</a>.</p>");
        sb.AppendLine("<p>This report is an inventory, not a rewrite of your code. API keys and connection strings are never copied into the report.</p>");
        sb.AppendLine("<h2>Summary</h2>");
        sb.AppendLine("<table><thead><tr><th>Severity</th><th>Kind</th><th>Path</th><th>Line</th><th>Detail</th></tr></thead><tbody>");
        if (findings.Count == 0)
        {
            sb.AppendLine("<tr><td colspan=\"5\">No Stackify Retrace traces were found in this directory.</td></tr>");
        }
        else
        {
            foreach (var f in findings)
            {
                sb.AppendLine($"<tr><td class=\"{Enc(f.Severity)}\">{Enc(f.Severity)}</td><td>{Enc(f.Kind)}</td><td><code>{Enc(f.Path)}</code></td><td>{f.Line?.ToString() ?? ""}</td><td>{Enc(f.Detail)}</td></tr>");
            }
        }
        sb.AppendLine("</tbody></table>");
        sb.AppendLine("<h2>Suggested next steps</h2><ol>");
        sb.AppendLine("<li>Export Retrace logs (100,000 entries or 50 MB per file) and dashboard PDFs before 31 March 2027. Alert rules have no documented export — write them down.</li>");
        sb.AppendLine("<li>Pilot OpenTelemetry on one service and keep Retrace running until the new path is proven, then cut over in waves.</li>");
        sb.AppendLine("<li>Map custom <code>ProfileTracer.CreateAsOperation</code> calls to <code>ActivitySource</code> and <code>StackifyLib.Metrics</code> to <code>System.Diagnostics.Metrics</code>.</li>");
        sb.AppendLine("<li>OpenTelemetry auto-instrumentation does not copy method-level call trees, first-chance exceptions, or query-parameter capture.</li>");
        sb.AppendLine("<li>For IIS / .NET Framework 4.6.2+, use <code>Register-OpenTelemetryForIIS</code>. For ASP.NET Core to Azure Monitor, use the Microsoft OpenTelemetry Distro (<code>Microsoft.OpenTelemetry</code>).</li>");
        sb.AppendLine("</ol>");
        sb.AppendLine("<h2>OpenTelemetry starter snippets</h2>");
        sb.AppendLine("<h3>ASP.NET Core</h3>");
        sb.AppendLine($"<pre><code>{Enc(SnippetGenerator.AspNetCore(backend))}</code></pre>");
        sb.AppendLine("<h3>IIS / .NET Framework</h3>");
        sb.AppendLine($"<pre><code>{Enc(SnippetGenerator.IisNotes(backend))}</code></pre>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    static string Escape(string s) => s.Replace("|", "\\|").Replace("\n", " ");

    static string Enc(string s) => WebUtility.HtmlEncode(s);
}
