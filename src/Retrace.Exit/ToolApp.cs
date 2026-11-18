namespace Retrace.Exit;

public static class ToolApp
{
    public const string Version = "0.1.0";

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Any(a => a is "-h" or "--help" or "-?" or "help"))
        {
            stdout.WriteLine(HelpText);
            return 0;
        }

        if (args.Any(a => a is "-v" or "--version"))
        {
            stdout.WriteLine($"Retrace.Exit {Version}");
            return 0;
        }

        string? pathArg = args.FirstOrDefault(a => !a.StartsWith('-') && !IsOptionValue(args, a));
        var root = Path.GetFullPath(pathArg ?? Directory.GetCurrentDirectory());
        var format = GetOption(args, "--format") ?? "markdown";
        var backend = GetOption(args, "--backend") ?? "azure-monitor";
        var outPath = GetOption(args, "--out");
        var snippetDir = GetOption(args, "--emit-snippets");

        if (!IsSupportedFormat(format))
        {
            stderr.WriteLine($"Unknown format '{format}'. Use markdown or html.");
            return 2;
        }

        if (!IsSupportedBackend(backend))
        {
            stderr.WriteLine($"Unknown backend '{backend}'. Use azure-monitor, grafana, otlp, or signoz.");
            return 2;
        }

        if (!Directory.Exists(root))
        {
            stderr.WriteLine($"Path not found: {root}");
            return 1;
        }

        var findings = Scanner.Scan(root);
        var report = ReportWriter.Render(findings, format, backend);

        if (string.IsNullOrWhiteSpace(outPath))
        {
            stdout.WriteLine(report);
        }
        else
        {
            var fullOut = Path.GetFullPath(outPath);
            var dir = Path.GetDirectoryName(fullOut);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullOut, report);
            stdout.WriteLine($"Wrote {fullOut}");
        }

        if (!string.IsNullOrWhiteSpace(snippetDir))
        {
            var fullSnippets = Path.GetFullPath(snippetDir);
            Directory.CreateDirectory(fullSnippets);
            SnippetGenerator.Write(fullSnippets, backend, findings);
            stdout.WriteLine($"Wrote snippets to {fullSnippets}");
        }

        return 0;
    }

    public static readonly string HelpText = """
Retrace.Exit 0.1.0
Scan a .NET solution for Stackify Retrace usage and draft an OpenTelemetry migration plan.

Usage:
  retrace-exit [path] [options]

Arguments:
  path                     Directory to scan (default: current directory)

Options:
  --format markdown|html   Report format (default: markdown)
  --out <file>             Write the report to a file instead of stdout
  --backend <name>         azure-monitor (default), grafana, otlp, or signoz
  --emit-snippets <dir>    Write starter OpenTelemetry snippets to a directory
  -h, --help               Show this help
  -v, --version            Show the version

The tool does not modify your source, does not send data over the network, and
never prints API keys. Official EOL notice:
https://docs.stackify.com/docs/stackify-end-of-life-customer-letter
""";

    static bool IsSupportedFormat(string format) =>
        format.Equals("markdown", StringComparison.OrdinalIgnoreCase) ||
        format.Equals("md", StringComparison.OrdinalIgnoreCase) ||
        format.Equals("html", StringComparison.OrdinalIgnoreCase);

    static bool IsSupportedBackend(string backend) =>
        backend.Equals("azure-monitor", StringComparison.OrdinalIgnoreCase) ||
        backend.Equals("grafana", StringComparison.OrdinalIgnoreCase) ||
        backend.Equals("otlp", StringComparison.OrdinalIgnoreCase) ||
        backend.Equals("signoz", StringComparison.OrdinalIgnoreCase);

    static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }

    static bool IsOptionValue(string[] args, string value)
    {
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == value && args[i - 1].StartsWith('-'))
                return true;
        }
        return false;
    }
}
