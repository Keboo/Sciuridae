using Azure.Data.Tables;

namespace Sciuridae.Api.Data;

public static class TableServiceClientExtensions
{
    public static async Task<App?> GetApp(this TableServiceClient client, string appName)
    {
        var tableClient = client.GetTableClient(App.TableName);
        return await tableClient.GetEntityAsync<App>(appName, appName);
    }
}
