namespace Sciuridae.Api.Controllers;

public record class UpdateAppRequest(
    string AppName,
    string Version,
    string? Tag = null,
    string? Channel = null);

public record class UpdateGithubAppRequest(
    string AppName,
    string Version,
    string RepositoryUrl,
    string? Tag = null,
    string? Channel = null)
    : UpdateAppRequest(AppName, Version, Tag, Channel);
