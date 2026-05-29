param(
    [string] $DotNetImage = "mcr.microsoft.com/dotnet/sdk:10.0",
    [string] $RavenDbImage = "ravendb/ravendb:7.2-latest",
    [string] $Password = "ravendb-mcp-test",
    [string] $Filter = "FullyQualifiedName~SecuredRavenDbConnectionTests|FullyQualifiedName~McpToolContractTests|FullyQualifiedName~RawHttpDiagnosticsTests|FullyQualifiedName~DocumentStoreFactoryTests|FullyQualifiedName~RavenDbOptionsValidatorTests"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$suffix = [Guid]::NewGuid().ToString("N").Substring(0, 8)
$containerName = "ravendb-mcp-secure-$suffix"
$networkName = "ravendb-mcp-secure-$suffix"
$certRoot = Join-Path ([System.IO.Path]::GetTempPath()) "ravendb-mcp-secure-$suffix"

New-Item -ItemType Directory -Path $certRoot | Out-Null
$testsCompleted = $false

$generateCertificates = @'
set -euo pipefail

openssl genrsa -out /certs/ca.key 2048
openssl req -new -x509 -key /certs/ca.key -out /certs/ca.crt -subj "/C=US/ST=Arizona/L=Nevada/O=RavenDB Test CA/OU=RavenDB test CA/CN=localhost/emailAddress=ravendbca@example.com" -addext "basicConstraints = critical, CA:TRUE" -addext "keyUsage = critical, digitalSignature, keyCertSign"

openssl genrsa -out /certs/localhost.key 2048
openssl req -new -key /certs/localhost.key -out /certs/localhost.csr -subj "/C=US/ST=Arizona/L=Nevada/O=RavenDB Test/OU=RavenDB test/CN=localhost/emailAddress=ravendb@example.com" -addext "subjectAltName = DNS:localhost,DNS:ravendb"
openssl x509 -req -extensions ext -extfile /workspace/cert/test_cert.conf -in /certs/localhost.csr -CA /certs/ca.crt -CAkey /certs/ca.key -CAcreateserial -out /certs/localhost.crt
openssl pkcs12 -passout pass:"$TEST_CERT_PASSWORD" -export -out /certs/server.pfx -inkey /certs/localhost.key -in /certs/localhost.crt -certfile /certs/ca.crt

openssl genrsa -out /certs/operator.key 2048
openssl req -new -key /certs/operator.key -out /certs/operator.csr -subj "/C=US/ST=Arizona/L=Nevada/O=RavenDB MCP/OU=RavenDB MCP/CN=ravendb-mcp-operator/emailAddress=ravendb@example.com" -addext "subjectAltName = DNS:localhost,DNS:ravendb"
openssl x509 -req -extensions ext -extfile /workspace/cert/test_cert.conf -in /certs/operator.csr -CA /certs/ca.crt -CAkey /certs/ca.key -CAcreateserial -out /certs/operator.crt
openssl pkcs12 -passout pass:"$TEST_CERT_PASSWORD" -export -out /certs/operator.pfx -inkey /certs/operator.key -in /certs/operator.crt -certfile /certs/ca.crt

chmod -R a+rX /certs

openssl x509 -in /certs/localhost.crt -noout -fingerprint -sha1 | cut -d= -f2 | tr -d : > /certs/admin-thumbprint.txt
'@

$runTests = @'
set -euo pipefail

cp /certs/ca.crt /usr/local/share/ca-certificates/ravendb-mcp-test-ca.crt
update-ca-certificates

mkdir /work
cp -a /source/. /work/
find /work -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
cd /work

for i in {1..90}; do
    if curl --fail --silent https://ravendb/setup/alive > /dev/null; then
        dotnet test RavenDB.Mcp.slnx --configuration Release --filter "$TEST_FILTER"
        exit $?
    fi

    sleep 1
done

echo "RavenDB did not become ready at https://ravendb/"
exit 1
'@

$generateCertificatesPath = Join-Path $certRoot "generate-certs.sh"
$runTestsPath = Join-Path $certRoot "run-tests.sh"

[System.IO.File]::WriteAllText($generateCertificatesPath, $generateCertificates.Replace("`r`n", "`n"))
[System.IO.File]::WriteAllText($runTestsPath, $runTests.Replace("`r`n", "`n"))

function Assert-NativeCommandSucceeded([string] $step)
{
    if ($LASTEXITCODE -ne 0) {
        throw "$step failed with exit code $LASTEXITCODE."
    }
}

try {
    docker network create $networkName | Out-Null
    Assert-NativeCommandSucceeded "Create Docker network"

    docker run --rm `
        --volume "${repoRoot}:/workspace:ro" `
        --volume "${certRoot}:/certs" `
        --workdir /workspace `
        --env "TEST_CERT_PASSWORD=$Password" `
        $DotNetImage `
        bash /certs/generate-certs.sh
    Assert-NativeCommandSucceeded "Generate certificates"

    $adminThumbprint = (Get-Content (Join-Path $certRoot "admin-thumbprint.txt") -Raw).Trim()

    docker run --detach `
        --name $containerName `
        --network $networkName `
        --network-alias ravendb `
        --volume "${certRoot}:/certs" `
        --env RAVEN_ServerUrl=https://0.0.0.0 `
        --env RAVEN_PublicServerUrl=https://ravendb `
        --env RAVEN_Setup_Mode=None `
        --env RAVEN_Security_Certificate_Path=/certs/server.pfx `
        --env RAVEN_Security_Certificate_Password=$Password `
        --env RAVEN_Security_WellKnownCertificates_Admin=$adminThumbprint `
        --env RAVEN_License_Eula_Accepted=true `
        $RavenDbImage | Out-Null
    Assert-NativeCommandSucceeded "Start RavenDB"

    Start-Sleep -Seconds 2

    $containerRunning = docker inspect --format "{{.State.Running}}" $containerName
    Assert-NativeCommandSucceeded "Inspect RavenDB container"

    if ($containerRunning -ne "true") {
        docker logs $containerName
        throw "RavenDB exited before tests could start."
    }

    docker run --rm `
        --network $networkName `
        --volume "${repoRoot}:/source:ro" `
        --volume "${certRoot}:/certs" `
        --env RAVENDB_SECURE_TEST_URL=https://ravendb/ `
        --env RAVENDB_SECURE_ADMIN_CERTIFICATE_PATH=/certs/server.pfx `
        --env RAVENDB_SECURE_ADMIN_CERTIFICATE_PASSWORD=$Password `
        --env RAVENDB_SECURE_OPERATOR_CERTIFICATE_PATH=/certs/operator.pfx `
        --env RAVENDB_SECURE_OPERATOR_CERTIFICATE_PASSWORD=$Password `
        --env "TEST_FILTER=$Filter" `
        $DotNetImage `
        bash /certs/run-tests.sh
    Assert-NativeCommandSucceeded "Run secured tests"

    $testsCompleted = $true
}
finally {
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    if (-not $testsCompleted) {
        docker logs $containerName 2>$null
    }
    docker rm --force $containerName 2>$null | Out-Null
    docker network rm $networkName 2>$null | Out-Null
    $ErrorActionPreference = $previousErrorActionPreference
    Remove-Item -LiteralPath $certRoot -Recurse -Force
}
