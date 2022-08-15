using Sciuridae.Api.Controllers;
using Sciuridae.Cli;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http.Json;
using System.Text.Json;

Option<string> appNameOption = new(new[] { "--app-name", "-a" })
{
    IsRequired = true
};
Option<string> versionOption = new(new[] { "--app-version", "-v" })
{
    IsRequired = true
};
Option<string> apiKeyOption = new(new[] { "--api-key" })
{
    IsRequired = true
};
Option<string> channelOption = new(new[] { "--channel", "-c" }, () => "production");
Option<string> gitTagOption = new(new[] { "--git-tag", "--tag" });
Option<string> repositoryOption = new(new[] { "--repository", "-r" })
{
    IsRequired = true
};
Option<Uri> serverUrlOption = new("--server-url", () => new Uri("https://sciuridae.azurewebsites.net/"))
{
    IsRequired = true
};
Option<DirectoryInfo> outputDirectoryOption = new(new[] { "--output-dir", "-o" }, () => new("./Releases"));

Command downloadCommand = new("download")
{
    appNameOption,
    channelOption,
    outputDirectoryOption,
    serverUrlOption
};
downloadCommand.SetHandler(async (InvocationContext ctx) =>
{
    string? appName = ctx.ParseResult.GetValueForOption(appNameOption);
    string? channel = ctx.ParseResult.GetValueForOption(channelOption);
    Uri? serverUrl = ctx.ParseResult.GetValueForOption(serverUrlOption);
    DirectoryInfo? outputDirectory = ctx.ParseResult.GetValueForOption(outputDirectoryOption);

    if (serverUrl is null)
    {
        ctx.Console.Error.Write($"No server URL provided{Environment.NewLine}");
        ctx.ExitCode = 1;
        return;
    }

    HttpClient client = new();
    string latestFiles = await client.GetStringAsync(new Uri(serverUrl, $"App/download/{appName}"));
    if (JsonSerializer.Deserialize(latestFiles, StringArrayContext.Default.StringArray) is { } fileUrls)
    {
        await Parallel.ForEachAsync(fileUrls, async (fileUrl, token) =>
        {
            using Stream urlStream = await client.GetStreamAsync(fileUrl);
            string outputFile = Path.GetFileName(fileUrl);
            if (outputDirectory is not null)
            {
                outputDirectory.Create();
                outputFile = Path.Combine(outputDirectory.FullName, outputFile);
            }
            using Stream fileStream = File.OpenWrite(outputFile);
            await urlStream.CopyToAsync(fileStream);
        });
    }
    else
    {
        ctx.Console.WriteLine("No files found to download");
    }
});

Command latestVersionCommand = new("latest-version")
{
    appNameOption,
    channelOption,
    apiKeyOption,
    serverUrlOption
};
latestVersionCommand.SetHandler(async (InvocationContext ctx) =>
{
    string? appName = ctx.ParseResult.GetValueForOption(appNameOption);
    string? channel = ctx.ParseResult.GetValueForOption(channelOption);
    Uri? serverUrl = ctx.ParseResult.GetValueForOption(serverUrlOption);
    string? apiKey = ctx.ParseResult.GetValueForOption(apiKeyOption);


    if (serverUrl is null)
    {
        ctx.Console.Error.Write($"No server URL provided{Environment.NewLine}");
        ctx.ExitCode = 1;
        return;
    }
    HttpClient client = new();

    string endpoint = $"{serverUrl.AbsoluteUri}App/version/{appName}";
    if (!string.IsNullOrWhiteSpace(channel))
    {
        endpoint += $"?channel={channel}";
    }
    string version = await client.GetStringAsync(new Uri(endpoint, UriKind.Absolute));
    ctx.Console.WriteLine(version);
});


Command githubCommand = new("github")
{
    appNameOption,
    versionOption,
    channelOption,
    gitTagOption,
    repositoryOption,
    apiKeyOption,
    serverUrlOption
};
githubCommand.SetHandler(async (InvocationContext ctx) =>
{
    string? appName = ctx.ParseResult.GetValueForOption(appNameOption);
    string? version = ctx.ParseResult.GetValueForOption(versionOption);
    string? channel = ctx.ParseResult.GetValueForOption(channelOption);
    string? gitTag = ctx.ParseResult.GetValueForOption(gitTagOption);
    Uri? serverUrl = ctx.ParseResult.GetValueForOption(serverUrlOption);
    string? apiKey = ctx.ParseResult.GetValueForOption(apiKeyOption);
    string? repositoryUrl = ctx.ParseResult.GetValueForOption(repositoryOption);

    string endpoint = $"{serverUrl!.AbsoluteUri}App/release/github/v1";

    HttpClient client = new(new HmacDelegatingHandler(appName!, apiKey!));
    UpdateGithubAppRequest request = new(appName!, version!, repositoryUrl!, gitTag, channel);
    var response = await client.PostAsJsonAsync(new Uri(endpoint, UriKind.Absolute), request);
    if (!response.IsSuccessStatusCode)
    {
        ctx.Console.Error.Write(await response.Content.ReadAsStringAsync());
        ctx.Console.Error.Write(Environment.NewLine);
        ctx.ExitCode = 1;
    }
    else
    {
        ctx.Console.WriteLine($"Added release for {appName} @ {version}");
    }
});

Command updateCommand = new("add-release")
{
    githubCommand
};


Command generateActions = new("generate-action")
{

};

RootCommand rootCommand = new()
{
    downloadCommand,
    latestVersionCommand,
    updateCommand
};



await rootCommand.InvokeAsync(args);
