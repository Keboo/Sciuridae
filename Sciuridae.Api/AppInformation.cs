using Azure.Data.Tables;
using Sciuridae.Api.Data;
using Sciuridae.Api.Providers;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace Sciuridae.Api;

public class AppInformation
{
    public AppInformation(TableServiceClient client, ProviderFactory providerFactory)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
    }

    private TableServiceClient Client { get; }
    private ProviderFactory ProviderFactory { get; }

    public async Task<App?> Register(string appName)
    {
        TableClient tableClient = Client.GetTableClient(App.TableName);

        using var cryptoProvider = RandomNumberGenerator.Create();
        byte[] secretKeyByteArray = new byte[32]; //256 bit
        cryptoProvider.GetBytes(secretKeyByteArray);
        var apiKey = Convert.ToBase64String(secretKeyByteArray);

        var app = new App(appName)
        {
            ApiKey = apiKey
        };

        var response = await tableClient.AddEntityAsync(app);
        if (response.IsError)
        {
            return null;
        }
        return app;
    }

    public async Task<bool> AddRelease(Release release)
    {
        TableClient tableClient = Client.GetTableClient(Release.TableName);
        var response = await tableClient.AddEntityAsync(release);
        return !response.IsError;
    }

    public async Task<Uri?> GetFile(string appName, string channel, string fileName, string? version = null)
    {
        Release? release = await GetRelease(appName, channel, version);
        if (release is not null && GetProvider(release) is { } provider)
        {
            return await provider.GetFile(release, fileName);
        }
        return null;
    }

    public async Task<Release?> GetRelease(string appName, string channel, string? version = null)
    {
        Expression<Func<Release, bool>> query = version is null
            ? x => x.AppName == appName
            : x => x.AppName == appName && x.Version == version;

        return await GetLatestRelease(query);
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
