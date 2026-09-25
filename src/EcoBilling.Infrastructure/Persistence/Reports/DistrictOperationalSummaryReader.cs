using System.Data;
using EcoBilling.Modules.Reports.Contracts;
using EcoBilling.Modules.Reports.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class DistrictOperationalSummaryReader(
    EcoBillingDbContext dbContext)
    : IDistrictOperationalSummaryReader
{
    public async Task<DistrictOperationalSummary> ReadAsync(
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State is not ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT
                    (SELECT count(*) FROM residents.residents),
                    (SELECT count(*) FROM controllers.controllers),
                    (SELECT count(*) FROM accounts.accounts),
                    (SELECT count(*) FROM accounts.addresses),
                    (SELECT count(*) FROM meters.meters),
                    (SELECT count(*) FROM readings.meter_readings),
                    (SELECT count(*) FROM tariffs.tariffs),
                    (SELECT count(*) FROM tariffs.tariff_versions),
                    (SELECT count(*) FROM billing.charges),
                    (SELECT count(*) FROM payments.payments)
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "The operational summary query returned no row.");
            }

            return new DistrictOperationalSummary(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetInt64(3),
                reader.GetInt64(4),
                reader.GetInt64(5),
                reader.GetInt64(6),
                reader.GetInt64(7),
                reader.GetInt64(8),
                reader.GetInt64(9));
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
