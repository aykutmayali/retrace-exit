using Serilog;
using StackifyLib;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Stackify()
    .CreateLogger();

var app = builder.Build();
app.UseMiddleware<StackifyMiddleware.RequestTracerMiddleware>();
app.ConfigureStackifyLogging(app.Configuration);

app.MapGet("/login", () =>
{
    var tracer = ProfileTracer.CreateAsOperation("UserLogin");
    tracer.Exec(() => Metrics.Count("Users", "Login Success"));
    return "ok";
});

app.Run();
