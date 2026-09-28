using System.Security.Cryptography;
using System.Text;
using EcoBilling.Modules.Controllers.Features.Abstractions;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class ControllerCreationRequestFingerprinter
    : IControllerCreationRequestFingerprinter
{
    private readonly byte[] key;

    public ControllerCreationRequestFingerprinter(string base64Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                "The controller creation fingerprint key must be valid Base64.",
                nameof(base64Key),
                exception);
        }

        if (key.Length < DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes)
        {
            throw new ArgumentException(
                $"The controller creation fingerprint key must contain at least {DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes} bytes.",
                nameof(base64Key));
        }
    }

    public string Create(
        string fullName,
        string normalizedEmail,
        string initialCredential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(initialCredential);

        var canonicalRequest = string.Concat(
            "controller:v1:",
            fullName.Length,
            ":",
            fullName,
            normalizedEmail.Length,
            ":",
            normalizedEmail,
            initialCredential.Length,
            ":",
            initialCredential);
        return Convert.ToHexString(
            HMACSHA256.HashData(
                key,
                Encoding.UTF8.GetBytes(canonicalRequest)));
    }
}
