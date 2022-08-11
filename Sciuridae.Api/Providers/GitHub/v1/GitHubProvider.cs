using Sciuridae.Api.Data;

namespace Sciuridae.Api.Providers.GitHub.v1;

public class GitHubProvider : IAppDataProvider
{
    private record class GitHubProviderData(string RepositoryUrl);

    private GitHubProviderData Data { get; }

    public GitHubProvider(string data)
    {
        Data = System.Text.Json.JsonSerializer.Deserialize<GitHubProviderData>(data)
            ?? throw new InvalidOperationException($"Invalid data provided to {nameof(GitHubProvider)}");
    }

    public Task<Uri?> GetFile(Release release, string fileName)
    {
        Uri fileUri = new(GetRepoUri(), $"releases/download/{release.Tag}/{fileName}");
        return Task.FromResult<Uri?>(fileUri);
    }

    private Uri GetRepoUri()
        => new (Data.RepositoryUrl, UriKind.Absolute);
}