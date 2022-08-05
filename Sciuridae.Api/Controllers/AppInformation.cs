using Azure.Data.Tables;
using Sciuridae.Api.Data;
using Sciuridae.Api.Providers;
using System.Linq.Expressions;

namespace Sciuridae.Api.Controllers;

public class AppInformation
{
    public AppInformation(TableServiceClient client, ProviderFactory providerFactory)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
    }

    private TableServiceClient Client { get; }
    private ProviderFactory ProviderFactory { get; }

    public async Task AddRelease(string appName, string channel, string version)
    {
        TableClient tableClient = Client.GetTableClient(Release.TableName);
        var release = new Release(appName, version, channel, version)
        {
            Provider = "github",
            ProviderVersion = 1,
            ProviderData = "{ \"RepositoryUrl\": \"https://github.com/Keboo/SimplyBudget\" }"
        };

        await tableClient.AddEntityAsync(release);
    }

    public async Task<Uri?> GetFile(string appName, string channel, string fileName, string? version = null)
    {
        Expression<Func<Release, bool>> query = version is null
            ? x => x.AppName == appName 
            : x => x.AppName == appName && x.Version == version;

        Release? release = await GetLatestRelease(query);
        if (release is not null && GetProvider(release) is { } provider)
        {
            return await provider.GetFile(release, fileName);
        }
        return null;
    }

    private async Task<Release?> GetLatestRelease(Expression<Func<Release, bool>> query)
    {
        TableClient tableClient = Client.GetTableClient(Release.TableName);
        var items = tableClient.QueryAsync(query);
        //Get latest version for the channel and return that manifest
        Release? latest = null;
        await foreach (var item in items)
        {
            if (latest is null)
            {
                latest = item;
            }
            else
            {
                if (VersionComparer.Instance.Compare(latest, item) <= 0)
                {
                    latest = item;
                }
            }
        }
        
        return latest;
    }

    private IAppDataProvider? GetProvider(Release release)
    {
        if (release?.Provider is null || release.ProviderData is null)
        {
            return null;
        }
        return ProviderFactory.CreateProvider(release.Provider, release.ProviderVersion, release.ProviderData);
    }
}
