[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[0-9A-Za-z._-]+$")]
    [string]$ReleaseVersion,

    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[0-9]{4}\.[0-9]{2}(\.[0-9]{1,2})?$")]
    [string]$ApplicationVersion,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 9223372036854775807)]
    [long]$BuildNumber,

    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[0-9]{4}-[0-9]{2}-[0-9]{2}$")]
    [string]$ReleaseDate,

    [ValidateSet("HrmOnly", "HrmAndGateway")]
    [string]$DeploymentMode = "HrmAndGateway",

    [string]$ImageNamespace = "vnta",

    [string]$OutputRoot
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

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Convert-ShellScriptsToLf {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptsDirectory
    )

    $encoding = New-Object System.Text.UTF8Encoding($false)
    Get-ChildItem -Path $ScriptsDirectory -Filter "*.sh" -File | ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        $normalizedContent = $content -replace "`r`n", "`n"
        [System.IO.File]::WriteAllText($_.FullName, $normalizedContent, $encoding)
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$deployDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $deployDir)

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot ".artifacts\releases"
}

$releaseFolderName = "ubuntu-docker-$ReleaseVersion"
$releaseDir = Join-Path $OutputRoot $releaseFolderName
$imagesDir = Join-Path $releaseDir "images"
$scriptsOutDir = Join-Path $releaseDir "scripts"

$hrmImage = "$ImageNamespace/hrm-web:$ReleaseVersion"
$admsImage = "$ImageNamespace/adms-gateway:$ReleaseVersion"

if (Test-Path $releaseDir) {
    throw "Release directory already exists; refusing to overwrite: $releaseDir"
}

New-Item -ItemType Directory -Force -Path $imagesDir | Out-Null
New-Item -ItemType Directory -Force -Path $scriptsOutDir | Out-Null

$selectedImages = @()

Invoke-External -Command @(
    "docker", "build",
    "-f", (Join-Path $deployDir "hrm-web.Dockerfile"),
    "-t", $hrmImage,
    "--build-arg", "APPLICATION_VERSION=$ApplicationVersion",
    "--build-arg", "BUILD_NUMBER=$BuildNumber",
    "--build-arg", "RELEASE_DATE=$ReleaseDate",
    $repoRoot
)

Invoke-External -Command @(
    "docker", "save",
    "--output", (Join-Path $imagesDir "hrm-web.tar"),
    $hrmImage
)
$selectedImages += $hrmImage

if ($DeploymentMode -eq "HrmAndGateway") {
    Invoke-External -Command @(
        "docker", "build",
        "-f", (Join-Path $deployDir "adms-gateway.Dockerfile"),
        "-t", $admsImage,
        $repoRoot
    )

    Invoke-External -Command @(
        "docker", "save",
        "--output", (Join-Path $imagesDir "adms-gateway.tar"),
        $admsImage
    )
    $selectedImages += $admsImage
}

Copy-Item (Join-Path $deployDir "docker-compose.production.yml") $releaseDir -Force
Copy-Item (Join-Path $deployDir ".env.production.example") $releaseDir -Force
Copy-Item (Join-Path $deployDir "README.md") $releaseDir -Force
Copy-Item (Join-Path $deployDir "scripts\*.sh") $scriptsOutDir -Force
Convert-ShellScriptsToLf -ScriptsDirectory $scriptsOutDir

$generatedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$manifestLines = @(
    "ReleaseVersion: $ReleaseVersion",
    "ApplicationVersion: $ApplicationVersion",
    "BuildNumber: $BuildNumber",
    "ReleaseDate: $ReleaseDate",
    "DeploymentMode: $DeploymentMode",
    "GeneratedAtUtc: $generatedAtUtc",
    "RepoRoot: $repoRoot",
    "Images:"
)
$manifestLines += $selectedImages | ForEach-Object { "  - $_" }
$manifestLines += @(
    "Contents:",
    "  - docker-compose.production.yml",
    "  - .env.production.example",
    "  - images/*.tar",
    "  - scripts/*.sh"
)
Write-Utf8NoBom -Path (Join-Path $releaseDir "release-manifest.txt") -Content (($manifestLines -join "`n") + "`n")

$checksumLines = foreach ($file in Get-ChildItem -Path $releaseDir -File -Recurse | Sort-Object FullName) {
    $relativePath = $file.FullName.Substring($releaseDir.Length + 1).Replace("\", "/")
    $hash = (Get-FileHash -Algorithm SHA256 -Path $file.FullName).Hash.ToLowerInvariant()
    "$hash  $relativePath"
}

Write-Utf8NoBom -Path (Join-Path $releaseDir "sha256sums.txt") -Content (($checksumLines -join "`n") + "`n")

Write-Host ""
Write-Host "Release package is ready:"
Write-Host "  $releaseDir"
Write-Host ""
Write-Host "Next step:"
Write-Host "  Run publish-ubuntu-release.ps1 to upload, back up the database, and deploy."
