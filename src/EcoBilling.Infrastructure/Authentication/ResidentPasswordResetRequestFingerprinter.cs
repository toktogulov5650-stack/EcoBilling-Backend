using System.Security.Cryptography;
using System.Text;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class ResidentPasswordResetRequestFingerprinter
    : IResidentPasswordResetRequestFingerprinter
{
    private readonly byte[] key;

    public ResidentPasswordResetRequestFingerprinter(string base64Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                "The resident password reset fingerprint key must be valid Base64.",
                nameof(base64Key),
                exception);
        }

        if (key.Length < DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes)
        {
            throw new ArgumentException(
                $"The resident password reset fingerprint key must contain at least {DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes} bytes.",
                nameof(base64Key));
        }
    }

    public string Create(ResidentId residentId, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(residentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var canonicalRequest = string.Concat(
            "resident-password-reset:v1|",
            residentId.Value.ToString("D"),
            '|',
            newPassword.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ':',
            newPassword);
        return Convert.ToHexString(
            HMACSHA256.HashData(
                key,
                Encoding.UTF8.GetBytes(canonicalRequest)));
    }
}
