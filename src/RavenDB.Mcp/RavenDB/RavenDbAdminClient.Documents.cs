using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ModelContextProtocol;
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
            throw new McpException($"GET {url} failed with {(int)response.StatusCode}: {content}");

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
        bool includeMetadata,
        CancellationToken cancellationToken)
    {
        ValidateName(query, "Query", nameof(query));

        if (MutatingQuery.IsMatch(query))
            throw new McpException("run_query is read-only; UPDATE/patch queries are not allowed.");

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
        catch (RavenRequestException e) when (TryQueryError(e.StatusCode, e.ResponseText, out var queryError))
        {
            // Malformed RQL — hand the parser/validation message back so the caller can fix it.
            return new RunQueryResult(databaseName, query, queryError);
        }

        return new RunQueryResult(databaseName, query,
            includeMetadata ? result : SlimMetadata(result));
    }

    private static readonly string[] KeptMetadata = ["@id", "@collection"];

    // Reduce each row/include's @metadata to identity only (@id, @collection); drops change-vector, flags,
    // timestamps, CLR type, scores. Keeps results referenceable while cutting the bulk of a result set.
    internal static JsonElement SlimMetadata(JsonElement result)
    {
        var root = JsonNode.Parse(result.GetRawText());

        static void Slim(JsonNode? doc)
        {
            if (doc is not JsonObject o || o["@metadata"] is not JsonObject meta)
                return;
            var kept = new JsonObject();
            foreach (var key in KeptMetadata)
                if (meta[key] is { } value)
                    kept[key] = value.DeepClone();
            if (kept.Count > 0)
                o["@metadata"] = kept;
            else
                o.Remove("@metadata");
        }

        if (root?["Results"] is JsonArray rows)
            foreach (var row in rows)
                Slim(row);
        if (root?["Includes"] is JsonObject includes)
            foreach (var include in includes)
                Slim(include.Value);

        return JsonSerializer.SerializeToElement(root);
    }

    // RavenDB reports a bad query as an error whose body Type is a query parse/validation exception (or a
    // missing target index); a genuine server fault has a different Type and is left to propagate.
    internal static bool TryQueryError(int statusCode, string responseText, out JsonElement error)
    {
        // A bodyless 404 from the queries endpoint means the named index doesn't exist.
        if (statusCode == 404 && string.IsNullOrWhiteSpace(responseText))
        {
            error = JsonSerializer.SerializeToElement(new
            {
                Error = "Query target not found (HTTP 404) — the index named in `from index '…'` does not exist.",
                Hint = "Check the index name with get_database_record (index names) or get_index, or query the collection dynamically: from <Collection>."
            });
            return true;
        }

        error = default;
        try
        {
            var body = JsonSerializer.Deserialize<JsonElement>(responseText);
            if (!body.TryGetProperty("Type", out var t) || t.ValueKind != JsonValueKind.String)
                return false;

            var type = t.GetString()!;
            if (!type.Contains("ParseException", StringComparison.Ordinal)
                && !type.Contains("InvalidQueryException", StringComparison.Ordinal)
                && !type.Contains("IndexDoesNotExistException", StringComparison.Ordinal))
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
