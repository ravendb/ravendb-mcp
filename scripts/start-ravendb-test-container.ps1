param(
    [int] $Port = 8070,
    [string] $Name = "ravendb-mcp-test"
)

docker run --rm --detach `
    --name $Name `
    --publish "${Port}:8080" `
    --env RAVEN_ServerUrl=http://0.0.0.0:8080 `
    --env RAVEN_Setup_Mode=None `
    --env RAVEN_Security_UnsecuredAccessAllowed=PublicNetwork `
    --env RAVEN_License_Eula_Accepted=true `
    ravendb/ravendb:7.2-latest

for ($i = 0; $i -lt 60; $i++) {
    try {
        Invoke-WebRequest "http://127.0.0.1:$Port/setup/alive" -UseBasicParsing | Out-Null
        "RavenDB is ready at http://127.0.0.1:$Port/"
        exit 0
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

throw "RavenDB did not become ready at http://127.0.0.1:$Port/"
