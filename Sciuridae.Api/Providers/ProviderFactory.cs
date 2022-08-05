using Sciuridae.Api.Providers.File;
using Sciuridae.Api.Providers.GitHub;

namespace Sciuridae.Api.Providers;

public class ProviderFactory
{
    public IAppDataProvider CreateProvider(string provider, int version, string data)
        => GetProvider(provider).Create(version, data);

    private IAppDataProviderFactory GetProvider(string provider) 
        => provider.ToLowerInvariant() switch
        {
            "github" => new GitHubProviderFactory(),
            "localfile" => new LocalFileProviderFactory(),
            _ => throw new NotSupportedException($"Unknown provider '{provider}'"),
        };
}
