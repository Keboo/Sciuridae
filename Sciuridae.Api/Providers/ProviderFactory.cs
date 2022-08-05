using Sciuridae.Api.Providers.GitHub;

namespace Sciuridae.Api.Providers;

public class ProviderFactory
{
    public IAppDataProvider CreateProvider(string provider, int version, string data)
        => GetProvider(provider).Create(version, data);

    private IAppDataProviderFactory GetProvider(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "github" => new GitHubProviderFactory(),
            _ => throw new NotSupportedException($"Unknown provider '{provider}'"),
        };
    }
}
