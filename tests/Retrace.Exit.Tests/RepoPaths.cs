namespace Retrace.Exit.Tests;

internal static class RepoPaths
{
    public static string Root
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Retrace.Exit.slnx"))
                    || File.Exists(Path.Combine(dir.FullName, "Retrace.Exit.sln")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Could not locate Retrace.Exit.sln from " + AppContext.BaseDirectory);
        }
    }

    public static string Samples => Path.Combine(Root, "samples");
    public static string AspNetCoreShop => Path.Combine(Samples, "aspnetcore-shop");
    public static string NetFrameworkIis => Path.Combine(Samples, "netframework-iis");
}
