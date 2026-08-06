namespace SocialReelSaver.Infrastructure.Storage;

/// <summary>
/// Resolves local object-storage roots independently of process CWD
/// (Windows Services often start with CWD = System32).
/// </summary>
public static class LocalStoragePath
{
    public static string Resolve(string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath) ? "storage" : configuredPath.Trim();
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, path));
    }
}
