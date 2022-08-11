namespace Sciuridae.Api.Data;

public record class App : BaseTableEntity
{
    public const string TableName = "Apps";

    public App()
    { }

    public App(string appName)
        : base(appName)
    { }

    public override string RowKey
    {
        get => PartitionKey;
        set => PartitionKey = value;
    }

    public string? ApiKey { get; set; }
}
