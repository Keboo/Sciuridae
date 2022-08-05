using Azure.Data.Tables;

namespace Sciuridae.Api.Data;

public record class App : BaseTableEntity
{
    public const string TableName = "Apps";

    public App(string appName)
        : base(appName)
    { }

    public override string RowKey
    {
        get => ((ITableEntity)this).PartitionKey;
        set => ((ITableEntity)this).PartitionKey = value;
    }
}
