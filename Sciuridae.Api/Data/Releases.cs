namespace Sciuridae.Api.Data;

public record class Release : BaseTableEntity
{
    public const string DefaultChannel = "production";
    public const string TableName = "Releases";
    
    public Release()
    { }
    
    public Release(string appName, string tag, string? channel, string version, string setupFile)
        : base(appName)
    {
        Tag = tag;
        Channel = channel ?? DefaultChannel;
        Version = version;
        SetupFile = setupFile;
    }

    public override string RowKey
    {
        get => Version;
        set => Version = value;
    }

    public string Tag { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string SetupFile { get; set; } = null!;
    public string? Provider { get; set; }
    public int ProviderVersion { get; set; }
    public string? ProviderData { get; set; }
}
