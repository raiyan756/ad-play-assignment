using System.Data;
using MySqlConnector;

// Q5: Parameterized MySQL query option, implemented with MySqlConnector.
// Assumes Orders(Id, CustomerId, OrderDate, Amount, Status) and
// Customers(Id, Name, Country, City). DateTo is an exclusive upper bound.
public sealed record SearchFilter(
    string? CustomerName = null, DateTime? DateFrom = null, DateTime? DateTo = null,
    decimal? MinAmount = null, decimal? MaxAmount = null, string? Status = null,
    string? Country = null, string? City = null,
    string SortBy = "OrderDate", string SortDirection = "desc",
    int Page = 1, int PageSize = 20);

public sealed record OrderResult(long Id, string CustomerName, DateTime OrderDate,
    decimal Amount, string Status);

public static class DynamicSearch
{
    public static async Task<(List<OrderResult> Items, long TotalCount)> SearchAsync(
        MySqlConnection connection, SearchFilter f, CancellationToken ct)
    {
        if (f.Page < 1 || f.PageSize is < 1 or > 100)
            throw new ArgumentException("Page must be positive; PageSize must be 1 to 100.");
        long offset = ((long)f.Page - 1) * f.PageSize;
        if (offset > 100_000)
            throw new ArgumentException("Use cursor pagination for deeper pages.");
        if (f.DateFrom > f.DateTo || f.MinAmount > f.MaxAmount || f.MinAmount < 0 || f.MaxAmount < 0)
            throw new ArgumentException("Invalid date or amount range.");

        var sortColumn = f.SortBy switch
        {
            "Id" => "o.Id", "OrderDate" => "o.OrderDate",
            "Amount" => "o.Amount", "CustomerName" => "c.Name",
            _ => throw new ArgumentException("Invalid SortBy.")
        };
        var direction = f.SortDirection?.ToUpperInvariant();
        if (direction is not ("ASC" or "DESC"))
            throw new ArgumentException("Invalid SortDirection.");

        var predicates = new List<string>();
        var parameters = new Dictionary<string, object>();
        void Add(string condition, string name, object? value)
        {
            if (value is null) return;
            predicates.Add(condition);
            parameters.Add(name, value);
        }
        if (!string.IsNullOrWhiteSpace(f.CustomerName))
        {
            // Literal prefix search: escape the escape character and LIKE wildcards.
            var prefix = f.CustomerName.Replace("=", "==").Replace("%", "=%")
                .Replace("_", "=_") + "%";
            Add("c.Name LIKE @name ESCAPE '='", "@name", prefix);
        }
        Add("o.OrderDate >= @from", "@from", f.DateFrom);
        Add("o.OrderDate < @to", "@to", f.DateTo);
        Add("o.Amount >= @min", "@min", f.MinAmount);
        Add("o.Amount <= @max", "@max", f.MaxAmount);
        Add("o.Status = @status", "@status", f.Status);
        Add("c.Country = @country", "@country", f.Country);
        Add("c.City = @city", "@city", f.City);

        var where = predicates.Count == 0 ? "1=1" : string.Join(" AND ", predicates);
        var from = $"FROM Orders o JOIN Customers c ON c.Id=o.CustomerId WHERE {where}";
        var order = $"{sortColumn} {direction}" + (sortColumn == "o.Id" ? "" : ", o.Id ASC");

        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        // Count and page use the same consistent InnoDB snapshot.
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        MySqlCommand Command(string sql)
        {
            var cmd = new MySqlCommand(sql, connection, tx) { CommandTimeout = 10 };
            foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
            return cmd;
        }

        await using var count = Command("SELECT COUNT(*) " + from);
        var total = Convert.ToInt64(await count.ExecuteScalarAsync(ct));
        await using var page = Command($"""
            SELECT o.Id, c.Name, o.OrderDate, o.Amount, o.Status
            {from} ORDER BY {order} LIMIT @take OFFSET @skip
            """);
        page.Parameters.AddWithValue("@take", f.PageSize);
        page.Parameters.AddWithValue("@skip", offset);
        var items = new List<OrderResult>();
        await using (var reader = await page.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                items.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetDateTime(2),
                    reader.GetDecimal(3), reader.GetString(4)));
        }
        await tx.CommitAsync(ct);
        return (items, total);
    }
}
