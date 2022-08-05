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

    public string PartitionKey
    {
        get => AppName;
        set => AppName = value;
    }
    public abstract string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string AppName { get; private set; } = null!;
}
