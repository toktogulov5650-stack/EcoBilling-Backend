using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EcoBilling.Modules.Identity.Application.Abstractions;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class DirectorPasswordResetRequestFingerprinter
    : IDirectorPasswordResetRequestFingerprinter
{
    private readonly byte[] key;

    public DirectorPasswordResetRequestFingerprinter(string base64Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                "The director password reset fingerprint key must be valid Base64.",
                nameof(base64Key),
                exception);
        }

        if (key.Length < DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes)
        {
            throw new ArgumentException(
                $"The director password reset fingerprint key must contain at least {DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes} bytes.",
                nameof(base64Key));
        }
    }

    public string Create(string normalizedEmail, string newPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var canonicalRequest = string.Concat(
            "director-password-reset:v1|",
            normalizedEmail,
            '|',
            newPassword.Length.ToString(CultureInfo.InvariantCulture),
            ':',
            newPassword);
        return Convert.ToHexString(
            HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(canonicalRequest)));
    }
}
