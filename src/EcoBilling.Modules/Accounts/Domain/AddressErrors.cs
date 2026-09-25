using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Accounts.Domain;

public static class AddressErrors
{
    public static Error InvalidLocality { get; } = new(
        "address.invalid_locality",
        "The address locality is invalid.",
        ErrorType.Validation);

    public static Error InvalidStreet { get; } = new(
        "address.invalid_street",
        "The address street is invalid.",
        ErrorType.Validation);

    public static Error InvalidHouse { get; } = new(
        "address.invalid_house",
        "The address house is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "address.not_found",
        "The address was not found.",
        ErrorType.NotFound);
}
