namespace Sciuridae.Api.Auth;

using Microsoft.AspNetCore.Authentication;
using System;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddHmacAuthentication(this AuthenticationBuilder builder)
    {
        return builder.AddHmacAuthentication((options) => { });
    }

    public static AuthenticationBuilder AddHmacAuthentication(this AuthenticationBuilder builder, Action<HmacAuthenticationOptions> options)
    {
        return builder.AddScheme<HmacAuthenticationOptions, HmacAuthenticationHandler>(HmacAuthenticationOptions.DefaultSchema, options);
    }
}
