namespace Sciuridae.Api.Providers.GitHub;

public class GitHubProviderFactory : IAppDataProviderFactory
{
    public IAppDataProvider Create(int version, string data)
        => version switch
        {
            1 => new v1.GitHubProvider(data),
            _ => throw new NotSupportedException($"GitHub provider version {version} is not supported"),
        };
}
