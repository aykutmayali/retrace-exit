using System.Text.Json;
using System.Text.RegularExpressions;

namespace Retrace.Exit;

public sealed record Finding(
    string Severity,
    string Kind,
    string Path,
    int? Line,
    string Detail);

public sealed record ScanResult(string Root, IReadOnlyList<Finding> Findings);

public static class Scanner
{
    public static readonly string[] PackageNames =
    [
        "StackifyLib",
        "StackifyLib.signed",
        "StackifyMiddleware",
        "StackifyHttpModule",
        "StackifyLib.AspNetCore",
        "StackifyLib.log4net",
        "StackifyLib.log4net.v1_2_10",
        "StackifyLib.nLog",
        "NLog.Targets.Stackify",
        "Serilog.Sinks.Stackify",
        "StackifyLib.CoreLogger",
        "StackifyLib.Elmah",
        "StackifyLib.Elmah.MVC",
        "StackifyLib.StackifyTraceListener",
        "Stackify.AzureWebApps",
        "Stackify.EBS",
        "StackifyWCFInspector",
        "MMP.Herald.Sinks.Stackify"
    ];

    public const string ProfilerGuid = "cf0d821e-299b-5307-a3d8-b283c03916da";

    static readonly string[] SkipDirs = ["bin", "obj", ".git", "node_modules", ".vs", "packages", "nupkg"];

    static readonly string[] CodeTokens =
    [
        "StackifyLib.ProfileTracer",
        "ProfileTracer.CreateAsOperation",
        "StackifyLib.Metrics",
        "StackifyLib.Logger",
        "RequestTracerMiddleware",
        "ConfigureStackifyLogging",
        "WriteTo.Stackify",
        "StackifyTarget",
        "StackifyAppender",
        "StackifyHttpModule"
    ];

    static readonly string[] MetricsMethods =
    [
        "Metrics.Count",
        "Metrics.Average",
        "Metrics.Sum",
        "Metrics.Time",
        "Metrics.SetGauge",
        "Metrics.IncrementGauge",
        "Metrics.CountAndTime"
    ];

    public static ScanResult Scan(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var findings = new List<Finding>();
        foreach (var file in EnumerateFiles(root))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var name = Path.GetFileName(file);
            string text;
            try { text = File.ReadAllText(file); }
            catch { continue; }

            if (ext is ".csproj" or ".fsproj" or ".vbproj" || name.Equals("packages.config", StringComparison.OrdinalIgnoreCase))
                ScanPackages(root, file, text, findings);

            if (name.Equals("Stackify.json", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("stackify.json", StringComparison.OrdinalIgnoreCase))
                ScanStackifyJson(root, file, text, findings);

            if (name.Contains("appsettings", StringComparison.OrdinalIgnoreCase) && ext == ".json")
                ScanAppSettings(root, file, text, findings);

            if (name.Equals("web.config", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("app.config", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("nlog", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("log4net", StringComparison.OrdinalIgnoreCase))
                ScanXmlConfig(root, file, text, findings);

            if (ext is ".cs" or ".vb" or ".fs" or ".config" or ".json" or ".yml" or ".yaml" or
                ".env" or ".xml" || name.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("docker-compose", StringComparison.OrdinalIgnoreCase))
            {
                ScanTokens(root, file, text, findings);
                ScanProfilerGuid(root, file, text, findings);
            }
        }

        findings.Sort((a, b) =>
        {
            var s = SeverityRank(a.Severity).CompareTo(SeverityRank(b.Severity));
            if (s != 0) return s;
            var p = string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            if (p != 0) return p;
            return (a.Line ?? 0).CompareTo(b.Line ?? 0);
        });

        return new ScanResult(root, findings);
    }

    public static bool ReferencesPackage(string text, string packageId)
    {
        var escaped = Regex.Escape(packageId);
        var pattern = $@"(?ix)
            (PackageReference \s+ [^>]* \b Include \s*=\s* ['""] {escaped} ['""])
          | (<package \s+ [^>]* \b id \s*=\s* ['""] {escaped} ['""])
          | (Reference \s+ [^>]* \b Include \s*=\s* ['""] {escaped} (['""] | \s*,))
        ";
        return Regex.IsMatch(text, pattern);
    }

    static void ScanPackages(string root, string file, string text, List<Finding> findings)
    {
        foreach (var pkg in PackageNames)
        {
            if (!ReferencesPackage(text, pkg))
                continue;
            findings.Add(new("critical", "nuget", Rel(root, file), LineOf(text, pkg),
                $"Package '{pkg}' is referenced. Keep it until OpenTelemetry (or the chosen backend) is proven on this service, then remove it. Do not drop a logging package until a log bridge is in place."));
        }
    }

    static void ScanStackifyJson(string root, string file, string text, List<Finding> findings)
    {
        var detail = "Stackify.json found. Map AppName/Environment to OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES; migrate Instrumentation[] entries to ActivitySource or additional OTel sources.";
        try
        {
            using var doc = JsonDocument.Parse(text);
            var rootEl = doc.RootElement;
            var bits = new List<string>();
            if (rootEl.TryGetProperty("AppName", out var app) && app.ValueKind == JsonValueKind.String)
                bits.Add($"AppName={app.GetString()}");
            if (rootEl.TryGetProperty("Environment", out var env) && env.ValueKind == JsonValueKind.String)
                bits.Add($"Environment={env.GetString()}");
            if (rootEl.TryGetProperty("CaptureAllExceptions", out var cap) && cap.ValueKind is JsonValueKind.True or JsonValueKind.False)
                bits.Add($"CaptureAllExceptions={cap.GetBoolean()} (OpenTelemetry records unhandled exceptions by default; first-chance exceptions need manual spans).");
            if (rootEl.TryGetProperty("DisableTracing", out var dis) && dis.ValueKind is JsonValueKind.True or JsonValueKind.False)
                bits.Add($"DisableTracing={dis.GetBoolean()}");
            if (rootEl.TryGetProperty("Instrumentation", out var inst) && inst.ValueKind == JsonValueKind.Array)
                bits.Add($"Instrumentation entries: {inst.GetArrayLength()} (custom methods / StartTrace operations).");
            if (bits.Count > 0)
                detail += " " + string.Join(" ", bits);
        }
        catch (JsonException)
        {
            detail += " The file could not be parsed as JSON; inspect it by hand.";
        }

        findings.Add(new("critical", "stackify-json", Rel(root, file), null, detail));
    }

    static void ScanAppSettings(string root, string file, string text, List<Finding> findings)
    {
        if (text.Contains("\"Stackify\"", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Stackify.ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new("critical", "appsettings", Rel(root, file), LineOf(text, "Stackify"),
                "Stackify configuration section found. Remove ApiKey after cut-over (the value was not copied into this report). Move AppName and Environment to OTEL resource attributes or APPLICATIONINSIGHTS_CONNECTION_STRING in the environment / Key Vault."
                + SecretNote(text)));
        }
    }

    static void ScanXmlConfig(string root, string file, string text, List<Finding> findings)
    {
        foreach (var key in new[] { "Stackify.ApiKey", "Stackify.AppName", "Stackify.Environment", "StackifyAppender", "StackifyTarget" })
        {
            if (!text.Contains(key, StringComparison.OrdinalIgnoreCase))
                continue;
            findings.Add(new("critical", "xml-config", Rel(root, file), LineOf(text, key),
                $"Config contains '{key}'."
                + (key == "Stackify.ApiKey"
                    ? " The key value was not copied into this report. After cut-over, delete it and rotate the key if it was ever committed."
                    : " Map AppName/Environment to OTEL_SERVICE_NAME / OTEL_RESOURCE_ATTRIBUTES, or replace the appender/target with an OpenTelemetry log bridge.")
                + SecretNote(text)));
        }
    }

    static void ScanTokens(string root, string file, string text, List<Finding> findings)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        if (ext is not ".cs" and not ".vb" and not ".fs")
        {
            // Still look in config/json for WriteTo.Stackify / targets, but only first hit per token is enough there.
            foreach (var token in CodeTokens)
            {
                if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new("warning", "code", Rel(root, file), LineOf(text, token),
                        TokenAdvice(token)));
                }
            }
            return;
        }

        var stackifyInScope = text.Contains("using StackifyLib", StringComparison.OrdinalIgnoreCase)
            || text.Contains("StackifyLib.", StringComparison.OrdinalIgnoreCase);
        var tokens = CodeTokens.AsEnumerable();
        if (stackifyInScope)
            tokens = tokens.Concat(MetricsMethods);

        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var token in tokens)
            {
                if (lines[i].Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new("warning", "code", Rel(root, file), i + 1, TokenAdvice(token)));
                }
            }
        }
    }

    static void ScanProfilerGuid(string root, string file, string text, List<Finding> findings)
    {
        if (!text.Contains(ProfilerGuid, StringComparison.OrdinalIgnoreCase))
            return;
        findings.Add(new("critical", "profiler-guid", Rel(root, file), LineOf(text, ProfilerGuid),
            "Retrace .NET profiler GUID found. Replace CORECLR_PROFILER / COR_PROFILER with OpenTelemetry auto-instrumentation variables and remove StackifyProfiler.so / the Retrace agent after the new path is proven."));
    }

    static string TokenAdvice(string token) => token switch
    {
        "ProfileTracer.CreateAsOperation" or "StackifyLib.ProfileTracer" =>
            $"Found '{token}'. Replace with ActivitySource.StartActivity (or Activity.Current) and use W3C trace context instead of Trace.CorrelationManager.ActivityId.",
        "StackifyLib.Metrics" or "Metrics.Count" or "Metrics.Average" or "Metrics.Sum"
            or "Metrics.Time" or "Metrics.SetGauge" or "Metrics.IncrementGauge" or "Metrics.CountAndTime" =>
            $"Found '{token}' (StackifyLib.Metrics). Replace Count/Average/Sum/Time/SetGauge/IncrementGauge with System.Diagnostics.Metrics (Meter, Counter, Histogram, ObservableGauge).",
        "StackifyLib.Logger" =>
            $"Found '{token}'. Replace StackifyLib.Logger.Queue with ILogger or the OpenTelemetry logs API.",
        "RequestTracerMiddleware" =>
            $"Found '{token}'. Remove StackifyMiddleware.RequestTracerMiddleware and use OpenTelemetry.Instrumentation.AspNetCore, zero-code auto-instrumentation, or the Microsoft OpenTelemetry Distro.",
        "ConfigureStackifyLogging" =>
            $"Found '{token}'. Remove app.ConfigureStackifyLogging and configure a Serilog/NLog/log4net OpenTelemetry sink or the Microsoft.Extensions.Logging bridge.",
        "WriteTo.Stackify" =>
            $"Found '{token}'. Replace the Serilog Stackify sink with Serilog.Sinks.OpenTelemetry or a backend-specific sink. Set the endpoint in the environment, not in source.",
        "StackifyTarget" =>
            $"Found '{token}'. Replace the NLog Stackify target with an OpenTelemetry log bridge. Auto-instrumentation log-to-trace correlation is strongest for Microsoft.Extensions.Logging — validate NLog separately.",
        "StackifyAppender" =>
            $"Found '{token}'. Replace the log4net Stackify appender with an OpenTelemetry log bridge and validate correlation.",
        "StackifyHttpModule" =>
            $"Found '{token}'. Remove the IIS HTTP module and use OpenTelemetry .NET zero-code instrumentation for IIS (Register-OpenTelemetryForIIS).",
        _ => $"Found '{token}'. Map it to the matching OpenTelemetry or System.Diagnostics API."
    };

    static IEnumerable<string> EnumerateFiles(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            IEnumerable<string> subs = [];
            IEnumerable<string> files = [];
            try
            {
                subs = Directory.EnumerateDirectories(dir);
                files = Directory.EnumerateFiles(dir);
            }
            catch { continue; }

            foreach (var sub in subs)
            {
                var leaf = Path.GetFileName(sub);
                if (SkipDirs.Contains(leaf, StringComparer.OrdinalIgnoreCase)) continue;
                stack.Push(sub);
            }
            foreach (var f in files) yield return f;
        }
    }

    static string Rel(string root, string file) => Path.GetRelativePath(root, file);

    public static int? LineOf(string text, string token)
    {
        var idx = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var line = 1;
        for (var i = 0; i < idx; i++)
        {
            if (text[i] == '\n') line++;
        }
        return line;
    }

    static string SecretNote(string text) =>
        text.Contains("ApiKey", StringComparison.OrdinalIgnoreCase)
            ? " An ApiKey value is present and was not copied into this report."
            : "";

    static int SeverityRank(string s) => s switch
    {
        "critical" => 0,
        "warning" => 1,
        _ => 2
    };
}
