using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Storage;

/// <summary>
/// Applies <c>Storage:LocalDirectory</c> over <c>ObjectStorage:LocalRootPath</c>
/// and normalizes to an absolute path.
/// </summary>
public sealed class ObjectStorageOptionsPostConfigure : IPostConfigureOptions<ObjectStorageOptions>
{
    private readonly IConfiguration _configuration;

    public ObjectStorageOptionsPostConfigure(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void PostConfigure(string? name, ObjectStorageOptions options)
    {
        var alias = _configuration.GetSection(StorageOptions.SectionName)["LocalDirectory"];
        if (!string.IsNullOrWhiteSpace(alias))
        {
            options.LocalRootPath = alias.Trim();
        }

        options.LocalRootPath = LocalStoragePath.Resolve(options.LocalRootPath);
    }
}
