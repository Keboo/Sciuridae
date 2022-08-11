namespace Sciuridae.Api.Auth;

using Azure.Data.Tables;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sciuridae.Api.Data;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

public sealed class HmacAuthenticationHandler : AuthenticationHandler<HmacAuthenticationOptions>
{
    private const string AuthorizationHeader = "Authorization";

    public HmacAuthenticationHandler(IOptionsMonitor<HmacAuthenticationOptions> options,
                                     ILoggerFactory logger,
                                     UrlEncoder encoder,
                                     ISystemClock clock,
                                     TableServiceClient client)
        : base(options, logger, encoder, clock)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
    }

    private TableServiceClient Client { get; }

    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerContent = Request.Headers[AuthorizationHeader].SingleOrDefault();
        if (headerContent is null)
            return AuthenticateResult.NoResult();

        var parsed = HmacHelper.ParseHeader(headerContent);
        if (parsed is null)
            return AuthenticateResult.NoResult();

        var (appName, signature, nonce, timestamp) = parsed.Value;
        //TODO: Handle nonce

        // Verify that request data is within acceptable time
        if (!uint.TryParse(timestamp, NumberStyles.Number, CultureInfo.InvariantCulture, out uint seconds))
            return AuthenticateResult.Fail("Unable to parse timestamp");

        DateTimeOffset requestDate = HmacHelper.EpochStart + TimeSpan.FromSeconds(seconds);

        if (requestDate > Clock.UtcNow.Add(Options.AllowedDateDrift) || requestDate < Clock.UtcNow.Subtract(Options.AllowedDateDrift))
            return AuthenticateResult.Fail("Date is drifted more than allowed");

        // Lookup and verify secret
        App? app = await Client.GetApp(appName);

        if (app is null)
        {
            return AuthenticateResult.Fail("Invalid app name");
        }

        var secret = Convert.FromBase64String(app.ApiKey ?? "");

        if (secret is null)
        {
            return AuthenticateResult.Fail($"No secret for {appName}");
        }
        else if (secret.Length != 32)
        {
            throw new InvalidOperationException("Incorrect secret size");
        }

        // Check signature
        //TODO: Limit content length
        Request.EnableBuffering();
        string contentHash = await HmacHelper.GetContentHash(Request.Body);
        Request.Body.Position = 0;
        string requestUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";

        string signatureRawData = HmacHelper.BuildSignature(appName, Request.Method, requestUri, seconds, nonce, contentHash);
        string serverSignature = HmacHelper.Calculate(secret, signatureRawData);

        if (serverSignature.Equals(signature))
        {
            return AuthenticateResult.Success(new AuthenticationTicket(
                new GenericPrincipal(new GenericIdentity(appName), null),
                new AuthenticationProperties()
                {
                    IsPersistent = false,
                    AllowRefresh = false
                },
                Options.Schema));
        }
        else
        {
            return AuthenticateResult.Fail("Invalid signature");
        }
    }
}
