using Azure;
using Azure.Data.Tables;

namespace Sciuridae.Api.Data;

public abstract record class BaseTableEntity : ITableEntity
{
    //Needed for queries
    public BaseTableEntity()
    { }
    
    public BaseTableEntity(string appName)
    {
        AppName = appName;
    }

    string ITableEntity.PartitionKey
    {
        get => AppName;
        set => AppName = value;
    }
    public abstract string RowKey { get; set; }
    DateTimeOffset? ITableEntity.Timestamp { get; set; }
    ETag ITableEntity.ETag { get; set; }
    public string AppName { get; private set; } = null!;
}
