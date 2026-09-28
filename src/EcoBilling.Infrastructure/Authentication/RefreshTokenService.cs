using System.Security.Cryptography;
using System.Text;
using EcoBilling.Modules.Identity.Application.Abstractions;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenByteLength = 32;

    public GeneratedRefreshToken Generate()
    {
        var token = ToBase64Url(RandomNumberGenerator.GetBytes(TokenByteLength));
        return new GeneratedRefreshToken(token, Hash(token));
    }

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string ToBase64Url(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
