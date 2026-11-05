using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace StableRail.Infrastructure.Configuration;

public static class ConfigLocator
{
    public static void UseStableRailConfiguration(WebApplicationBuilder builder)
    {
        var configDirectory = FindConfigDirectory(builder.Environment.ContentRootPath);
        var existing = builder.Configuration.Sources.ToList();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(Path.Combine(configDirectory, "appsettings.json"), optional: false, reloadOnChange: false);
        builder.Configuration.AddJsonFile(
            Path.Combine(configDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"),
            optional: true,
            reloadOnChange: false);
        foreach (var source in existing)
            builder.Configuration.Sources.Add(source);
    }

    public static string FindConfigDirectory(string start)
    {
        var current = new DirectoryInfo(start);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "config", "appsettings.json");
            if (File.Exists(candidate))
                return Path.GetDirectoryName(candidate)!;
            current = current.Parent;
        }

        throw new InvalidOperationException("config/appsettings.json was not found from the content root.");
    }
}
