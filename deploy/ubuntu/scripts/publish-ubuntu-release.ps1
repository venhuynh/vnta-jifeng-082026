[CmdletBinding()]
param(
    [ValidateSet("HrmOnly", "HrmAndGateway")]
    [string]$DeploymentMode = "HrmAndGateway",

    [ValidatePattern("^[0-9A-Za-z._-]+$")]
    [string]$ReleaseVersion,

    [ValidatePattern("^[0-9]{4}\.[0-9]{2}(\.[0-9]{1,2})?$")]
    [string]$ApplicationVersion,

    [ValidatePattern("^[A-Za-z0-9.-]+$")]
    [string]$ServerHost = "192.168.1.218",

    [ValidatePattern("^[A-Za-z0-9._-]+$")]
    [string]$SshUser = "vns",

    [ValidateRange(1, 65535)]
    [int]$SshPort = 22,

    [ValidatePattern("^/[A-Za-z0-9._/-]+$")]
    [string]$DeployRoot = "/opt/vnta",

    [ValidatePattern("^[A-Za-z0-9._-]+$")]
    [string]$ImageNamespace = "vnta",

    [switch]$SkipDatabaseBackup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Command
    )

    Write-Host ">> $($Command -join ' ')"
    & $Command[0] $Command[1..($Command.Length - 1)]
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $($Command -join ' ')"
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$deployDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $deployDir)
$packageScript = Join-Path $scriptDir "package-release.ps1"

if ([string]::IsNullOrWhiteSpace($ReleaseVersion)) {
    $ReleaseVersion = (Get-Date).ToString("yyyy.MM.dd-HHmmss")
}

if ([string]::IsNullOrWhiteSpace($ApplicationVersion)) {
    $ApplicationVersion = (Get-Date).ToString("yyyy.MM")
}

$releaseDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
$remoteTarget = "$SshUser@$ServerHost"
$buildCounterPath = "$DeployRoot/shared/release-build-counter"
$buildCounterLockPath = "$DeployRoot/shared/release-build-counter.lock"

Push-Location $repoRoot
try {
    $worktreeChanges = & git status --porcelain
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to check Git status."
    }
    if ($worktreeChanges) {
        throw "Worktree has changes. Commit or stash them before creating a release."
    }

    Invoke-External -Command @( "docker", "info" )

    # Reserve the next persistent build number before creating the image. The server-side flock
    # prevents two release sessions from receiving the same number.
    $reserveBuildNumberCommand = @"
mkdir -p '$DeployRoot/shared' && ( flock -x 9; current=`$(cat '$buildCounterPath' 2>/dev/null || printf '0'); case "`$current" in (*[!0-9]*|'') exit 2;; esac; next=`$((current + 1)); printf '%s\n' "`$next" > '$buildCounterPath.tmp'; mv '$buildCounterPath.tmp' '$buildCounterPath'; printf '%s' "`$next" ) 9>'$buildCounterLockPath'
"@
    $reservedBuildNumber = & ssh -p $SshPort $remoteTarget $reserveBuildNumberCommand
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to reserve the next build number on the deployment server."
    }

    [long]$BuildNumber = 0
    $reservedBuildNumberText = ($reservedBuildNumber -join "").Trim()
    if (-not [long]::TryParse($reservedBuildNumberText, [ref]$BuildNumber) -or $BuildNumber -lt 1) {
        throw "Deployment server returned an invalid build number: '$reservedBuildNumberText'."
    }

    & $packageScript `
        -ReleaseVersion $ReleaseVersion `
        -ApplicationVersion $ApplicationVersion `
        -BuildNumber $BuildNumber `
        -ReleaseDate $releaseDate `
        -DeploymentMode $DeploymentMode `
        -ImageNamespace $ImageNamespace

    $releaseName = "ubuntu-docker-$ReleaseVersion"
    $releaseDirectory = Join-Path $repoRoot ".artifacts\releases\$releaseName"
    $remoteReleaseDirectory = "$DeployRoot/releases/$releaseName"
    $remoteEnvFile = "$DeployRoot/shared/env/.env.production"
    $hrmImage = "$ImageNamespace/hrm-web:$ReleaseVersion"
    $admsImage = "$ImageNamespace/adms-gateway:$ReleaseVersion"

    Invoke-External -Command @(
        "ssh", "-p", $SshPort, $remoteTarget,
        "test ! -e '$remoteReleaseDirectory'"
    )

    Invoke-External -Command @(
        "scp", "-P", $SshPort, "-r", $releaseDirectory,
        "${remoteTarget}:$DeployRoot/releases/"
    )

    $imageUpdateArguments = "'$remoteEnvFile' '$hrmImage'"
    if ($DeploymentMode -eq "HrmAndGateway") {
        $imageUpdateArguments += " '$admsImage'"
    }

    $remoteCommands = @(
        "set -euo pipefail",
        "cd '$remoteReleaseDirectory'",
        "chmod +x scripts/*.sh",
        "./scripts/set-release-images.sh $imageUpdateArguments"
    )

    if (-not $SkipDatabaseBackup) {
        $remoteCommands += "./scripts/backup-db.sh '$remoteEnvFile'"
    }

    $remoteCommands += @(
        "./scripts/deploy-release.sh '$remoteEnvFile' '$DeploymentMode'",
        "./scripts/verify-no-source.sh '$DeployRoot'"
    )

    Invoke-External -Command @(
        "ssh", "-p", $SshPort, $remoteTarget,
        ($remoteCommands -join " && ")
    )

    Write-Host ""
    Write-Host "Deployment completed: $releaseName"
    Write-Host "Application version: $ApplicationVersion"
    Write-Host "Build number: $BuildNumber"
    Write-Host "Release date (UTC): $releaseDate"
    Write-Host "Mode: $DeploymentMode"
    Write-Host "HRM: https://$ServerHost:8443"
}
finally {
    Pop-Location
}
