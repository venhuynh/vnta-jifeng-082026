[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$baselinePath = Join-Path $PSScriptRoot 'raw-mutation-baseline.json'
$infrastructureRoot = Join-Path $RepositoryRoot 'src\Vnta.HRM2026\Vnta.Hrm.Infrastructure'
$rawMutationPattern = 'ExecuteSql(?:Raw|Interpolated)?Async|new\s+NpgsqlCommand|\.ExecuteUpdateAsync\(|\.ExecuteDeleteAsync\('
$normalizedRepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path.TrimEnd('\', '/')
$repositoryRootPrefix = "$normalizedRepositoryRoot$([IO.Path]::DirectorySeparatorChar)"

if (-not (Test-Path -LiteralPath $baselinePath)) {
    throw "Audit raw-mutation baseline is missing: $baselinePath"
}

if (-not (Test-Path -LiteralPath $infrastructureRoot)) {
    throw "Infrastructure source root is missing: $infrastructureRoot"
}

$baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
$expected = @{}
foreach ($entry in $baseline.entries) {
    if ([string]::IsNullOrWhiteSpace($entry.path) -or $entry.occurrences -lt 0) {
        throw 'Audit raw-mutation baseline contains an invalid entry.'
    }

    if ($expected.ContainsKey($entry.path)) {
        throw "Audit raw-mutation baseline contains a duplicate path: $($entry.path)"
    }

    $expected[$entry.path] = [int]$entry.occurrences
}

$actual = @{}
Get-ChildItem -LiteralPath $infrastructureRoot -Recurse -Filter '*.cs' | ForEach-Object {
    $occurrences = [regex]::Matches(
        (Get-Content -LiteralPath $_.FullName -Raw),
        $rawMutationPattern,
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase).Count

    if ($occurrences -gt 0) {
        if (-not $_.FullName.StartsWith($repositoryRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Source file is unexpectedly outside the repository root: $($_.FullName)"
        }

        $relativePath = $_.FullName.Substring($repositoryRootPrefix.Length).Replace('\', '/')
        $actual[$relativePath] = $occurrences
    }
}

$paths = @($expected.Keys + $actual.Keys | Sort-Object -Unique)
$violations = foreach ($path in $paths) {
    $expectedCount = if ($expected.ContainsKey($path)) { $expected[$path] } else { 0 }
    $actualCount = if ($actual.ContainsKey($path)) { $actual[$path] } else { 0 }

    if ($expectedCount -ne $actualCount) {
        [PSCustomObject]@{
            Path = $path
            Expected = $expectedCount
            Actual = $actualCount
        }
    }
}

if (@($violations).Count -gt 0) {
    Write-Error 'Raw SQL/bulk mutation inventory changed. Review the audit path, then update tools/audit/raw-mutation-baseline.json and doc/sprints/KienTruc/sprint-024-audit-trail/write-path-inventory.md in the same pull request.'
    $violations | Format-Table -AutoSize | Out-String | Write-Error
    exit 1
}

$total = ($actual.Values | Measure-Object -Sum).Sum
Write-Host "Audit raw-mutation baseline verified: $total occurrences across $($actual.Count) Infrastructure files."
