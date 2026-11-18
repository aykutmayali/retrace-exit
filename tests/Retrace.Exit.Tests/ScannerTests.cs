using Xunit;

namespace Retrace.Exit.Tests;

public class ScannerTests
{
    [Fact]
    public void AspNetCoreSample_DetectsPackagesConfigJsonMiddlewareAndSink()
    {
        var result = Scanner.Scan(RepoPaths.AspNetCoreShop);

        Assert.Contains(result.Findings, f => f.Kind == "nuget" && f.Detail.Contains("StackifyMiddleware"));
        Assert.Contains(result.Findings, f => f.Kind == "nuget" && f.Detail.Contains("Serilog.Sinks.Stackify"));
        Assert.Contains(result.Findings, f => f.Kind == "nuget" && f.Detail.Contains("StackifyLib.AspNetCore"));
        Assert.Contains(result.Findings, f => f.Kind == "nuget" && f.Detail.Contains("StackifyLib") && !f.Detail.Contains("StackifyLib.AspNetCore"));
        Assert.Contains(result.Findings, f => f.Kind == "stackify-json" && f.Path.Replace('\\', '/').Contains("Stackify.json"));
        Assert.Contains(result.Findings, f => f.Kind == "stackify-json" && f.Detail.Contains("AppName=Sample.AspNetCoreShop"));
        Assert.Contains(result.Findings, f => f.Kind == "appsettings");
        Assert.Contains(result.Findings, f => f.Kind == "code" && f.Detail.Contains("WriteTo.Stackify"));
        Assert.Contains(result.Findings, f => f.Kind == "code" && f.Detail.Contains("RequestTracerMiddleware"));
        Assert.Contains(result.Findings, f => f.Kind == "code" && f.Detail.Contains("ConfigureStackifyLogging"));
        Assert.Contains(result.Findings, f => f.Kind == "profiler-guid");
        Assert.True(result.Findings.Count >= 8);
    }

    [Fact]
    public void NetFrameworkSample_DetectsPackagesConfigWebConfigAndProfileTracer()
    {
        var result = Scanner.Scan(RepoPaths.NetFrameworkIis);

        Assert.Contains(result.Findings, f => f.Kind == "nuget" && f.Path.Replace('\\', '/').Contains("packages.config") && f.Detail.Contains("StackifyLib"));
        Assert.Contains(result.Findings, f => f.Kind == "nuget" && f.Detail.Contains("StackifyLib.log4net"));
        Assert.Contains(result.Findings, f => f.Kind == "xml-config" && f.Detail.Contains("Stackify.ApiKey"));
        Assert.Contains(result.Findings, f => f.Kind == "xml-config" && f.Detail.Contains("Stackify.AppName"));
        Assert.Contains(result.Findings, f => f.Kind == "xml-config" && f.Detail.Contains("StackifyAppender"));
        Assert.Contains(result.Findings, f => f.Kind == "code" && f.Detail.Contains("ProfileTracer.CreateAsOperation"));
        Assert.Contains(result.Findings, f => f.Kind == "code" && (f.Detail.Contains("StackifyLib.Metrics") || f.Detail.Contains("Metrics.Count") || f.Detail.Contains("Metrics.Time")));
    }

    [Fact]
    public void Reports_DoNotLeakApiKeys()
    {
        var leaked = "SECRET-TEST-KEY-DO-NOT-LEAK";
        var core = Scanner.Scan(RepoPaths.AspNetCoreShop);
        var fx = Scanner.Scan(RepoPaths.NetFrameworkIis);

        var md = ReportWriter.Render(fx, "markdown", "azure-monitor")
                 + ReportWriter.Render(core, "markdown", "azure-monitor")
                 + ReportWriter.Render(fx, "html", "azure-monitor");

        Assert.DoesNotContain(leaked, md);
        Assert.DoesNotContain("DUMMY-NOT-A-REAL-KEY", md);
        Assert.Contains("was not copied into this report", ReportWriter.Render(fx, "markdown", "azure-monitor"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyDirectory_HasNoFindings()
    {
        var dir = Directory.CreateTempSubdirectory("retrace-exit-empty-");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "readme.txt"), "nothing to see");
            var result = Scanner.Scan(dir.FullName);
            Assert.Empty(result.Findings);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ReferencesPackage_IsExact_NotAPrefixMatch()
    {
        var onlyAspNetCore = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <PackageReference Include="StackifyLib.AspNetCore" Version="2.1.5" />
              </ItemGroup>
            </Project>
            """;

        Assert.True(Scanner.ReferencesPackage(onlyAspNetCore, "StackifyLib.AspNetCore"));
        Assert.False(Scanner.ReferencesPackage(onlyAspNetCore, "StackifyLib"));

        var packagesConfig = """
            <?xml version="1.0"?>
            <packages>
              <package id="StackifyLib" version="2.2.18" targetFramework="net462" />
            </packages>
            """;
        Assert.True(Scanner.ReferencesPackage(packagesConfig, "StackifyLib"));
        Assert.False(Scanner.ReferencesPackage(packagesConfig, "StackifyLib.log4net"));
    }

    [Fact]
    public void MarkdownAndHtmlReports_IncludeSnippetsAndEolLink()
    {
        var result = Scanner.Scan(RepoPaths.AspNetCoreShop);
        var md = ReportWriter.Render(result, "markdown", "azure-monitor");
        var html = ReportWriter.Render(result, "html", "grafana");

        Assert.Contains("31 March 2027", md);
        Assert.Contains("https://docs.stackify.com/docs/stackify-end-of-life-customer-letter", md);
        Assert.Contains("UseMicrosoftOpenTelemetry", md);
        Assert.Contains("Register-OpenTelemetryForIIS", md);
        Assert.Contains("StackifyMiddleware", md);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("31 March 2027", html);
        Assert.Contains("AddOtlpExporter", html);
        Assert.Contains("Register-OpenTelemetryForIIS", html);
        Assert.DoesNotContain("UseMicrosoftOpenTelemetry", html);
    }

    [Fact]
    public void ToolApp_HelpAndVersion_AndWritesReportFile()
    {
        var help = new StringWriter();
        var err = new StringWriter();
        Assert.Equal(0, ToolApp.Run(["--help"], help, err));
        Assert.Contains("retrace-exit [path]", help.ToString());

        var ver = new StringWriter();
        Assert.Equal(0, ToolApp.Run(["--version"], ver, err));
        Assert.Contains("0.1.0", ver.ToString());

        var missing = new StringWriter();
        var missingErr = new StringWriter();
        Assert.Equal(1, ToolApp.Run(["/tmp/retrace-exit-does-not-exist-xyz"], missing, missingErr));
        Assert.Contains("Path not found", missingErr.ToString());

        var outDir = Directory.CreateTempSubdirectory("retrace-exit-out-");
        try
        {
            var reportPath = Path.Combine(outDir.FullName, "report.md");
            var snippetDir = Path.Combine(outDir.FullName, "snippets");
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var code = ToolApp.Run(
                [RepoPaths.NetFrameworkIis, "--format", "markdown", "--out", reportPath, "--emit-snippets", snippetDir, "--backend", "azure-monitor"],
                stdout,
                stderr);
            Assert.Equal(0, code);
            Assert.True(File.Exists(reportPath));
            var text = File.ReadAllText(reportPath);
            Assert.Contains("ProfileTracer.CreateAsOperation", text);
            Assert.DoesNotContain("SECRET-TEST-KEY-DO-NOT-LEAK", text);
            Assert.True(File.Exists(Path.Combine(snippetDir, "aspnetcore-Program.cs.txt")));
            Assert.True(File.Exists(Path.Combine(snippetDir, "iis-netframework-notes.md")));
        }
        finally
        {
            outDir.Delete(recursive: true);
        }
    }
}
