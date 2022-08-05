using Sciuridae.Api.Data;

namespace Sciuridae.Api.Providers.File.v1;

public class LocalFileProvider : IAppDataProvider
{
    private record class LocalFileProviderData(string DirectoryPath);

    private LocalFileProviderData Data { get; }

    public LocalFileProvider(string data)
    {
        Data = System.Text.Json.JsonSerializer.Deserialize<LocalFileProviderData>(data)
            ?? throw new InvalidOperationException($"Invalid data provided to {nameof(LocalFileProvider)}");
    }

    public Task<Uri?> GetFile(Release release, string fileName)
    {
        string filePath = Path.Combine(Data.DirectoryPath, fileName);
        return Task.FromResult<Uri?>(new Uri("file://" + filePath));
    }
}
