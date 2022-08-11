using Sciuridae.Api.Auth;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Sciuridae.Cli;

public class HmacDelegatingHandler : DelegatingHandler
{
    private string AppName { get; }
    private string ApiKey { get; }

    public HmacDelegatingHandler(string appName, string apiKey)
        : base(new HttpClientHandler())
    {
        AppName = appName ?? throw new ArgumentNullException(nameof(appName));
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }


    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string contentHash = string.Empty;
        string requestUri = request.RequestUri!.AbsoluteUri.ToLowerInvariant();
        string requestHttpMethod = request.Method.Method;
        uint secondsSinceEpoch = (uint)(DateTime.UtcNow - HmacHelper.EpochStart).TotalSeconds;
        string nonce = Guid.NewGuid().ToString("N");
        if (request.Content != null)
        {
            contentHash = await HmacHelper.GetContentHash(await request.Content.ReadAsStreamAsync(cancellationToken));
        }
        string signatureRawData = HmacHelper.BuildSignature(AppName, requestHttpMethod, requestUri, secondsSinceEpoch, nonce, contentHash);

        var secretKeyByteArray = Convert.FromBase64String(ApiKey);

        string requestSignatureBase64String = HmacHelper.Calculate(secretKeyByteArray, signatureRawData);

        request.Headers.Authorization = HmacHelper.BuildHeader(AppName, requestSignatureBase64String, nonce, secondsSinceEpoch);
        return await base.SendAsync(request, cancellationToken);
    }
}
