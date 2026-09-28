using System.Security.Cryptography;
using System.Text;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class ResidentCreationRequestFingerprinter
    : IResidentCreationRequestFingerprinter
{
    private readonly byte[] key;

    public ResidentCreationRequestFingerprinter(string base64Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                "The resident creation fingerprint key must be valid Base64.",
                nameof(base64Key),
                exception);
        }

        if (key.Length < DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes)
        {
            throw new ArgumentException(
                $"The resident creation fingerprint key must contain at least {DirectorProvisioningRequestFingerprinter.MinimumKeyLengthInBytes} bytes.",
                nameof(base64Key));
        }
    }

    public string Create(
        string fullName,
        string normalizedAccountNumber,
        string password,
        Address address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedAccountNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(address);

        var values = new string?[]
        {
            "resident:v1",
            fullName,
            normalizedAccountNumber,
            password,
            address.Locality,
            address.Street,
            address.House,
            address.Building,
            address.Apartment
        };
        var canonicalRequest = string.Concat(
            values.Select(value =>
                value is null ? "-1:" : $"{value.Length}:{value}"));
        return Convert.ToHexString(
            HMACSHA256.HashData(
                key,
                Encoding.UTF8.GetBytes(canonicalRequest)));
    }
}
