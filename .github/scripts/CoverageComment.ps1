#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Builds the coverage comment posted to a pull request.

.DESCRIPTION
    Reads the Cobertura report produced for the PR head and, when it is available, the one
    produced for the PR base, and writes coverage-comment.md.

    The base report is best-effort by design: the workflow step that produces it is allowed
    to fail, because a base commit that no longer builds should cost the PR its delta, not
    its whole coverage report. With no base to compare against, the comment reports head
    coverage alone rather than reporting nothing.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Marker = '<!-- coverage-report -->'
$HeadReports = 'head-coverage'
$BaseReports = 'base/XmlFormatter.Tests/TestResults'

# Percentages are formatted invariantly: a runner with a comma decimal separator would
# otherwise emit "84,2%" into the comment.
$Invariant = [cultureinfo]::InvariantCulture

function Get-CoverageRates
{
    <#
        Returns a hashtable with Line and Branch percentages, or $null if no report matched.

        Sums the counts across every matching report rather than reading one file's rate. A
        run can leave more than one report behind - a rerun, or a second test project later -
        and picking one of them silently reports a number for part of the suite.
    #>
    param([string] $Root)

    if (-not (Test-Path -Path $Root)) { return $null }

    # @() so a single match is still an array - StrictMode has no .Count on a scalar.
    $files = @(Get-ChildItem -Path $Root -Filter 'coverage.cobertura.xml' -Recurse -File |
               Sort-Object -Property FullName)
    if ($files.Count -eq 0) { return $null }

    $linesCovered = $linesValid = $branchesCovered = $branchesValid = 0

    foreach ($file in $files)
    {
        try
        {
            $report = ([xml](Get-Content -Path $file.FullName -Raw)).DocumentElement
        }
        catch
        {
            Write-Host "Skipping unreadable report $($file.FullName): $($_.Exception.Message)"
            continue
        }

        $linesCovered    += [int] ($report.GetAttribute('lines-covered')    -as [int])
        $linesValid      += [int] ($report.GetAttribute('lines-valid')      -as [int])
        $branchesCovered += [int] ($report.GetAttribute('branches-covered') -as [int])
        $branchesValid   += [int] ($report.GetAttribute('branches-valid')   -as [int])
    }

    if ($linesValid -eq 0) { return $null }

    return @{
        Line = $linesCovered / $linesValid * 100
        # A project with no branches at all is 100% branch-covered, not 0%.
        Branch = if ($branchesValid -gt 0) { $branchesCovered / $branchesValid * 100 } else { 100.0 }
    }
}

function Format-Percent
{
    param([double] $Value)
    return $Value.ToString('F1', $Invariant)
}

function Format-Delta
{
    param([double] $Head, [double] $Base)

    $delta = $Head - $Base
    # Coverage percentages are noisy in the last decimal; below that a "change" is rounding.
    if ([Math]::Abs($delta) -lt 0.05) { return 'no change' }
    return "$($delta.ToString('+0.0;-0.0', $Invariant)) pp"
}

function New-Row
{
    param([string] $Label, [double] $Head, [object] $Base)

    if ($null -eq $Base) { return "| $Label | $(Format-Percent $Head)% | - | - |" }
    return "| $Label | $(Format-Percent $Head)% | $(Format-Percent $Base)% | $(Format-Delta $Head $Base) |"
}

$head = Get-CoverageRates -Root $HeadReports
if ($null -eq $head)
{
    Write-Host 'No head coverage report found; not writing a comment.'
    exit 0
}

$base = Get-CoverageRates -Root $BaseReports

$lines = @(
    $Marker
    '### Coverage'
    ''
    '| | This PR | Base | Change |'
    '|:---|---:|---:|---:|'
    New-Row -Label 'Line'   -Head $head.Line   -Base $(if ($base) { $base.Line }   else { $null })
    New-Row -Label 'Branch' -Head $head.Branch -Base $(if ($base) { $base.Branch } else { $null })
    ''
)

if ($null -eq $base)
{
    $lines += "_No base measurement available, so no comparison - this is the PR's own coverage._"
    $lines += ''
}

$server = if ($env:GITHUB_SERVER_URL) { $env:GITHUB_SERVER_URL } else { 'https://github.com' }
$runUrl = "$server/$($env:GITHUB_REPOSITORY)/actions/runs/$($env:GITHUB_RUN_ID)"
$lines += "[Full HTML report]($runUrl) is attached to the run as ``coverage-report``."

# LF and no BOM: the file is posted verbatim as a comment body.
$content = ($lines -join "`n") + "`n"
[System.IO.File]::WriteAllText(
    (Join-Path (Get-Location) 'coverage-comment.md'),
    $content,
    (New-Object System.Text.UTF8Encoding $false))

Write-Host "line $(Format-Percent $head.Line)%, branch $(Format-Percent $head.Branch)%"
