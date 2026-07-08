using Microsoft.Extensions.Hosting;

namespace Hop.Api.Configuration;

public static class EnvFileLoader
{
    public static void LoadForEnvironment(string? environmentName = null)
    {
        var configuredEnvFile = Environment.GetEnvironmentVariable("HOP_API_ENV_FILE");
        if (!string.IsNullOrWhiteSpace(configuredEnvFile) && File.Exists(configuredEnvFile))
        {
            Load([configuredEnvFile]);
            return;
        }

        const string serverEnvFile = "/etc/hop/hop-api.env";
        if (File.Exists(serverEnvFile))
        {
            Load([serverEnvFile]);
            return;
        }

        var normalizedEnvironment = environmentName
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Production;

        var fileNames = new List<string> { ".env" };
        if (!string.IsNullOrWhiteSpace(normalizedEnvironment))
        {
            fileNames.Add($".env.{normalizedEnvironment}");
            fileNames.Add($".env.{normalizedEnvironment.ToLowerInvariant()}");
        }

        LoadFromParentDirectories(fileNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static void LoadFromParentDirectories(params string[] fileNames)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        var paths = new List<string>();

        while (directory is not null)
        {
            foreach (var fileName in fileNames)
            {
                var path = Path.Combine(directory.FullName, fileName);
                if (File.Exists(path))
                {
                    paths.Add(path);
                }
            }

            if (paths.Count > 0)
            {
                Load(paths);
                return;
            }

            directory = directory.Parent;
        }
    }

    private static void Load(IEnumerable<string> paths)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            foreach (var entry in DotNetEnv.Env.NoEnvVars().Load(path))
            {
                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                values[entry.Key] = entry.Value;
            }
        }

        foreach (var (key, value) in values)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
