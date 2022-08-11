namespace Sciuridae.Api.Auth;

using Microsoft.AspNetCore.Authentication;
using System;

public class HmacAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultSchema = "HMAC";

    public string Schema => DefaultSchema;

    //TODO: Configurable
    public TimeSpan AllowedDateDrift { get; set; } = TimeSpan.FromMinutes(5);
}
