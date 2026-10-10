using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcoBilling.Api.Endpoints;

[JsonConverter(typeof(CreateResidentAddressRequestConverter))]
public sealed record CreateResidentAddressRequest(
    string? Locality,
    string? Street,
    string? House,
    string? Building,
    string? Apartment)
{
    public static implicit operator CreateResidentAddressRequest(string address) =>
        new(address, "-", "-", null, null);

    public override string ToString() =>
        $"{nameof(CreateResidentAddressRequest)} {{ Address = [REDACTED] }}";
}

public sealed class CreateResidentAddressRequestConverter
    : JsonConverter<CreateResidentAddressRequest>
{
    public override CreateResidentAddressRequest? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new(reader.GetString(), "-", "-", null, null);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Address must be a string or an object.");
        }

        var value = JsonSerializer.Deserialize<StructuredAddress>(ref reader, options);
        return value is null ? null : new(
            value.Locality, value.Street, value.House, value.Building, value.Apartment);
    }

    public override void Write(
        Utf8JsonWriter writer,
        CreateResidentAddressRequest value,
        JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, new StructuredAddress(
            value.Locality, value.Street, value.House, value.Building, value.Apartment), options);

    private sealed record StructuredAddress(
        string? Locality,
        string? Street,
        string? House,
        string? Building,
        string? Apartment);
}
