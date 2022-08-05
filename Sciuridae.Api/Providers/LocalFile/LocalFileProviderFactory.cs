namespace Sciuridae.Api.Providers.File;

public class LocalFileProviderFactory : IAppDataProviderFactory
{
    public IAppDataProvider Create(int version, string data)
        => version switch
        {
            1 => new v1.LocalFileProvider(data),
            _ => throw new NotSupportedException($"Local file provider version {version} is not supported"),
        };
}
