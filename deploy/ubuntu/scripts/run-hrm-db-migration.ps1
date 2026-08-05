[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [string]$EnvironmentName = "Production"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$expectedDatabaseName = "jifeng_hrm"
$connectionStringBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()
$connectionStringBuilder.PSBase.ConnectionString = $ConnectionString
$databaseName = if ($connectionStringBuilder.ContainsKey("Database")) {
    [string]$connectionStringBuilder["Database"]
}
elseif ($connectionStringBuilder.ContainsKey("Initial Catalog")) {
    [string]$connectionStringBuilder["Initial Catalog"]
}
else {
    ""
}

if (-not [string]::Equals($databaseName, $expectedDatabaseName, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing migration: the target database must be '$expectedDatabaseName'."
}

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Command
    )

    Write-Host ">> $($Command -join ' ')"
    & $Command[0] $Command[1..($Command.Length - 1)]
    if ($LASTEXITCODE -ne 0) {
        throw "Lệnh thất bại với mã thoát ${LASTEXITCODE}: $($Command -join ' ')"
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$deployDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $deployDir)

$projectPath = Join-Path $repoRoot "src\Vnta.HRM2026\Vnta.Hrm.Infrastructure\Vnta.Hrm.Infrastructure.csproj"
$startupPath = Join-Path $repoRoot "src\Vnta.HRM2026\Vnta.Hrm.Web\Vnta.Hrm.Web.csproj"
$toolManifest = Join-Path $repoRoot "dotnet-tools.json"

$previousEnvironment = [Environment]::GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Process")
$previousPostgres = [Environment]::GetEnvironmentVariable("ConnectionStrings__Postgres", "Process")
$previousDefault = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Process")

Push-Location $repoRoot
try {
    [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", $EnvironmentName, "Process")
    [Environment]::SetEnvironmentVariable("ConnectionStrings__Postgres", $ConnectionString, "Process")
    [Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $ConnectionString, "Process")

    Invoke-External -Command @(
        "dotnet", "tool", "restore",
        "--tool-manifest", $toolManifest
    )

    Invoke-External -Command @(
        "dotnet", "ef", "database", "update",
        "--project", $projectPath,
        "--startup-project", $startupPath,
        "--context", "ApplicationDbContext",
        "--configuration", "Release"
    )

    # Do not release a build whose EF model has drifted past the latest migration.
    # This is the same condition that would otherwise surface as
    # PendingModelChangesWarning when the application starts.
    Invoke-External -Command @(
        "dotnet", "ef", "migrations", "has-pending-model-changes",
        "--project", $projectPath,
        "--startup-project", $startupPath,
        "--context", "ApplicationDbContext",
        "--configuration", "Release"
    )
}
finally {
    [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", $previousEnvironment, "Process")
    [Environment]::SetEnvironmentVariable("ConnectionStrings__Postgres", $previousPostgres, "Process")
    [Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $previousDefault, "Process")
    Pop-Location
}
