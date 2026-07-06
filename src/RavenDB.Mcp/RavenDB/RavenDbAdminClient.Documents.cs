using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Operations.Counters;
using Raven.Client.Documents.Operations.TimeSeries;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    // RavenDB patch-by-query (mutating) uses an `update { ... }` clause. Reject it so run_query stays read-only.
    private static readonly Regex MutatingQuery = new(@"\bupdate\s*\{", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<GetDocumentResult> GetDocument(
        string databaseName,
        string id,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        ValidateName(id, "Document id", nameof(id));

        // GET /docs?id= returns { "Results": [ { ...document..., "@metadata": {...} } ] }.
        var url = BuildDatabaseUrl(databaseName, "/docs", ("id", id));
        using var response = await http.GetAsync(url, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new GetDocumentResult(databaseName, id, false, ToJson<object?>(null));

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GET {url} failed with {(int)response.StatusCode}: {content}");

        var payload = JsonSerializer.Deserialize<JsonElement>(content);
        if (payload.TryGetProperty("Results", out var results)
            && results.ValueKind == JsonValueKind.Array
            && results.GetArrayLength() > 0
            && results[0].ValueKind == JsonValueKind.Object)
        {
            return new GetDocumentResult(databaseName, id, true, results[0].Clone());
        }

        return new GetDocumentResult(databaseName, id, false, ToJson<object?>(null));
    }

    public async Task<JsonElement> GetDocumentCounters(string databaseName, string id, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        ValidateName(id, "Document id", nameof(id));

        var counters = await store.Operations.ForDatabase(databaseName).SendAsync(
            new GetCountersOperation(id),
            token: cancellationToken);

        return ToJson(counters);
    }

    public async Task<JsonElement> GetDocumentTimeSeries(
        string databaseName,
        string id,
        string name,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        ValidateName(id, "Document id", nameof(id));
        ValidateName(name, "Time series name", nameof(name));

        var series = await store.Operations.ForDatabase(databaseName).SendAsync(
            new GetTimeSeriesOperation(id, name, from, to),
            token: cancellationToken);

        // Project to a clean shape: TimeSeriesRangeResult carries an Includes BlittableJsonReaderObject
        // that System.Text.Json can't serialize, and the agent only needs the entries. (null when absent.)
        if (series is null)
            return ToJson(new { documentId = id, name, found = false });

        return ToJson(new
        {
            documentId = id,
            name,
            from = series.From,
            to = series.To,
            totalResults = series.TotalResults,
            entries = series.Entries.Select(entry => new
            {
                timestamp = entry.Timestamp,
                values = entry.Values,
                tag = entry.Tag,
                isRollup = entry.IsRollup
            })
        });
    }

    public async Task<JsonElement> GetCompareExchange(
        string databaseName,
        string? startsWith,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        var values = await store.Operations.ForDatabase(databaseName).SendAsync(
            new GetCompareExchangeValuesOperation<object>(startsWith ?? string.Empty, 0, Math.Clamp(pageSize ?? 100, 1, 1024)),
            token: cancellationToken);

        return ToJson(values);
    }

    public async Task<RunQueryResult> RunQuery(
        string databaseName,
        string query,
        int? start,
        int? pageSize,
        JsonElement? parameters,
        CancellationToken cancellationToken)
    {
        ValidateName(query, "Query", nameof(query));

        if (MutatingQuery.IsMatch(query))
            throw new ArgumentException("run_query is read-only; UPDATE/patch queries are not allowed.", nameof(query));

        var body = new Dictionary<string, object?>
        {
            ["Query"] = query,
            ["Start"] = Math.Max(start ?? 0, 0),
            ["PageSize"] = Math.Clamp(pageSize ?? 25, 1, 128),
        };
        if (parameters is { ValueKind: JsonValueKind.Object } queryParameters)
            body["QueryParameters"] = queryParameters;

        JsonElement result;
        try
        {
            result = await PostDatabaseJson(databaseName, "/queries", body, cancellationToken);
        }
        catch (RavenRequestException e) when (TryQueryError(e.ResponseText, out var queryError))
        {
            // Malformed RQL — hand the parser/validation message back so the caller can fix it.
            result = queryError;
        }

        return new RunQueryResult(databaseName, query, result);
    }

    // RavenDB reports a bad query as a 500 whose body Type is a query parse/validation exception; a genuine
    // server fault has a different Type and is left to propagate.
    private static bool TryQueryError(string responseText, out JsonElement error)
    {
        error = default;
        try
        {
            var body = JsonSerializer.Deserialize<JsonElement>(responseText);
            if (!body.TryGetProperty("Type", out var t) || t.ValueKind != JsonValueKind.String)
                return false;

            var type = t.GetString()!;
            if (!type.Contains("ParseException", StringComparison.Ordinal)
                && !type.Contains("InvalidQueryException", StringComparison.Ordinal))
                return false;

            var message = body.TryGetProperty("Message", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString()!
                : responseText;
            error = JsonSerializer.SerializeToElement(new
            {
                Error = message,
                Hint = "Don't retry from memory — read rql://cheatsheet for clause order/syntax, or rql://index to find the right feature resource."
            });
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class RavenRequestException(int statusCode, string responseText, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ResponseText { get; } = responseText;
}
