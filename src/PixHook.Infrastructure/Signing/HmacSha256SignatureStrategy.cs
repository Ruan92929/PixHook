using System.Security.Cryptography;
using System.Text;
using PixHook.Application.Abstractions;

namespace PixHook.Infrastructure.Signing;

/// <summary>HMAC-SHA256 genérico em hexadecimal minúsculo no header <c>X-Signature</c>.</summary>
public sealed class HmacSha256SignatureStrategy : ISignatureStrategy
{
    public const string StrategyKey = "hmac-sha256";
    public string Key => StrategyKey;
    public string HeaderName => "X-Signature";

    public string Sign(string payload, string secret) => HmacHex(payload, secret);

    internal static string HmacHex(string payload, string secret)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
