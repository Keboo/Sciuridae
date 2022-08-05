using Sciuridae.Api.Data;

namespace Sciuridae.Api.Providers;

public interface IAppDataProvider
{
    Task<Uri?> GetFile(Release release, string fileName);
}
