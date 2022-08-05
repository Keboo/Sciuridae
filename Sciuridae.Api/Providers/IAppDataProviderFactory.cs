namespace Sciuridae.Api.Providers;

public interface IAppDataProviderFactory
{
    IAppDataProvider Create(int version, string data);
}
