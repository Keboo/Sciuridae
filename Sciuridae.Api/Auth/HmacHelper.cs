using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Sciuridae.Api.Auth;

public static class HmacHelper
{
    private const string HmacScheme = "hmacauth";
    public static DateTime EpochStart { get; } = new(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);

    public static AuthenticationHeaderValue BuildHeader(string appName, string base64Signature, string nonce, uint secondsSinceEpoch)
        => new(HmacScheme, $"{appName}:{base64Signature}:{nonce}:{secondsSinceEpoch}");

    public static (string AppName, string Signature, string Nonce, string SecondsSinceEpoch)? ParseHeader(string headerContent)
    {
        var splitHeader = headerContent.Split(' ', ':');
        if (splitHeader.Length != 5)
            return null;
        var scheme = splitHeader[0];
        if (!string.Equals(scheme, HmacScheme))
            return null;

        return (splitHeader[1], splitHeader[2], splitHeader[3], splitHeader[4]);
    }

    public static string BuildSignature(string appName, string httpMethod, string requestUri, uint secondsSinceEpoch, string nonce, string contentHash) 
        => $"{appName}{httpMethod.ToUpperInvariant()}{requestUri.ToLowerInvariant()}{secondsSinceEpoch}{nonce}{contentHash}";
    
    public static async Task<string> GetContentHash(Stream content)
    {
        using MD5 md5 = MD5.Create();
        byte[] requestContentHash = await md5.ComputeHashAsync(content);
        return Convert.ToBase64String(requestContentHash);
    }

    public static string Calculate(byte[] secret, string signatureData)
    {
        using HMAC hmac = new HMACSHA256();
        hmac.Key = secret ?? throw new ArgumentNullException(nameof(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData)));
    }
}
