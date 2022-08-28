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

        string apiKey = GenerateApiKey();
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

    public async Task<App?> CycleKey(string appName)
    {
        TableClient tableClient = Client.GetTableClient(App.TableName);

        App? app = tableClient.GetEntity<App>(appName, appName);
        if (app is null) return null;
        app.ApiKey = GenerateApiKey();

        var response = await tableClient.UpdateEntityAsync<App>(app, app.ETag);
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

    public async Task<bool> DeleteRelease(string appName, string version, string channel)
    {
        TableClient tableClient = Client.GetTableClient(Release.TableName);
        Release? release = await GetRelease(appName, channel, version);
        if (release is null) return false;
        var response = await tableClient.DeleteEntityAsync(release.PartitionKey, release.RowKey);
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
            ? x => x.AppName == appName && x.Channel == channel
            : x => x.AppName == appName && x.Channel == channel && x.Version == version;

        return await GetLatestRelease(query);
    }

    public async Task<App?> GetApp(string appName)
    {
        TableClient tableClient = Client.GetTableClient(App.TableName);
        return await tableClient.GetEntityAsync<App>(appName, appName);
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

    private static string GenerateApiKey()
    {
        RandomNumberGenerator cryptoProvider = RandomNumberGenerator.Create();
        byte[] secretKeyByteArray = new byte[32]; //256 bit
        cryptoProvider.GetBytes(secretKeyByteArray);
        return Convert.ToBase64String(secretKeyByteArray);
    }
}
