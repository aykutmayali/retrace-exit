# Sample scan fixtures

These projects exist so `retrace-exit` can be demonstrated and tested. They are **not** meant to be restored or built: they reference Stackify packages on purpose, and they contain a clearly fake API key (`SECRET-TEST-KEY-DO-NOT-LEAK`) that the tool must never print.

- `aspnetcore-shop` — ASP.NET Core + `StackifyLib` + `StackifyMiddleware` + `Serilog.Sinks.Stackify` + `Stackify.json` + a Dockerfile profiler GUID.
- `netframework-iis` — a .NET Framework 4.6.2-style csproj, `packages.config`, `Web.config` appSettings / log4net `StackifyAppender`, and `ProfileTracer.CreateAsOperation` usage.

Scan them from the repo root:

```bash
retrace-exit ./samples --format markdown --out retrace-exit-report.md
```
