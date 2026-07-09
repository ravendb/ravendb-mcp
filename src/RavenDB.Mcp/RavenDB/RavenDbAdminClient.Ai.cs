using System.Text.Json;
using Raven.Client.Documents.Operations.AI.Agents;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    // AI agent configs can carry inline model credentials; redact defensively (this endpoint bypasses the record path).
    public async Task<JsonElement> GetAiAgents(string databaseName, string? name, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        var operation = string.IsNullOrWhiteSpace(name)
            ? new GetAiAgentsOperation()
            : new GetAiAgentsOperation(name);
        return RedactSecrets(await TryReadJson(() => ForDatabase(databaseName).SendAsync(operation, cancellationToken), cancellationToken));
    }
}
