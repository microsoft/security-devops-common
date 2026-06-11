<#
.SYNOPSIS
    Runs the behavioral evals for the onboard-defender-cli skill against a Copilot agent.

.DESCRIPTION
    Loads evals/skill-evals.psd1, sends each case's prompt to an agent that has the skill loaded,
    and checks the resulting transcript against the case's ExpectMatch / ForbidMatch regexes.

    These evals are NON-DETERMINISTIC (they exercise an LLM) and require the GitHub Copilot CLI
    (`copilot`) to be installed and authenticated, OR a custom -Invoker. They are intentionally
    separate from the deterministic Pester suite so CI can run the Pester tests without an LLM.

    The agent is invoked through -Invoker, a scriptblock that takes the prompt and returns the
    transcript string. The default invoker shells out to:
        copilot -p "<prompt>" --allow-all-tools
    Adjust -Invoker (or the default below) if your CLI build uses different flags.

.PARAMETER SpecPath
    Path to the eval spec. Defaults to evals/skill-evals.psd1 next to this script.

.PARAMETER Id
    Optional. Run only the case(s) whose Id matches this wildcard.

.PARAMETER Category
    Optional. Run only cases in this category (Trigger | NoTrigger | Flow | Safety).

.PARAMETER Invoker
    Optional scriptblock: param([string]$Prompt) -> [string] transcript. Override the default
    Copilot CLI call (useful for a different CLI, or for mocking in tests).

.PARAMETER ListOnly
    Print the selected cases and exit without invoking the agent.

.EXAMPLE
    ./evals/Invoke-SkillEvals.ps1 -ListOnly

.EXAMPLE
    ./evals/Invoke-SkillEvals.ps1 -Category Trigger

.EXAMPLE
    ./evals/Invoke-SkillEvals.ps1 -Invoker { param($p) my-agent --print $p | Out-String }
#>
[CmdletBinding()]
param(
    [string] $SpecPath = (Join-Path $PSScriptRoot 'skill-evals.psd1'),
    [string] $Id,
    [ValidateSet('Trigger', 'NoTrigger', 'Flow', 'Safety')]
    [string] $Category,
    [scriptblock] $Invoker,
    [switch] $ListOnly
)

$ErrorActionPreference = 'Stop'

function Get-DefaultInvoker {
    if (-not (Get-Command copilot -ErrorAction SilentlyContinue)) {
        throw ("The GitHub Copilot CLI ('copilot') was not found on PATH. Install/authenticate it " +
               "(see the copilot-sdk skill) or pass a custom -Invoker scriptblock.")
    }
    return {
        param([string] $Prompt)
        # Print (non-interactive) mode. --allow-all-tools lets the agent run the skill's steps.
        # Adjust these flags if your Copilot CLI build differs.
        & copilot -p $Prompt --allow-all-tools 2>&1 | Out-String
    }
}

# --- Load + filter cases -----------------------------------------------------------------
$spec = Import-PowerShellDataFile -Path $SpecPath
$cases = @($spec.Cases)
if ($Id)       { $cases = @($cases | Where-Object { $_.Id -like $Id }) }
if ($Category) { $cases = @($cases | Where-Object { $_.Category -eq $Category }) }

if ($cases.Count -eq 0) {
    Write-Warning "No eval cases matched the given filters."
    return
}

if ($ListOnly) {
    # Emit objects to the pipeline so callers (and tests) can consume them.
    $cases | ForEach-Object {
        [pscustomobject]@{ Id = $_.Id; Category = $_.Category; Prompt = $_.Prompt }
    }
    return
}

if (-not $Invoker) { $Invoker = Get-DefaultInvoker }

# --- Run + assert ------------------------------------------------------------------------
$results = foreach ($case in $cases) {
    Write-Host "`n=== [$($case.Category)] $($case.Id) ===" -ForegroundColor Cyan
    Write-Host "Prompt: $($case.Prompt)"

    $transcript = ''
    $failures = [System.Collections.Generic.List[string]]::new()
    try {
        $transcript = & $Invoker $case.Prompt
    } catch {
        $failures.Add("invoker threw: $($_.Exception.Message)")
    }

    foreach ($rx in @($case.ExpectMatch)) {
        if ($transcript -notmatch $rx) { $failures.Add("expected (missing): /$rx/") }
    }
    foreach ($rx in @($case.ForbidMatch)) {
        if ($transcript -match $rx) { $failures.Add("forbidden (present): /$rx/") }
    }

    $passed = $failures.Count -eq 0
    if ($passed) {
        Write-Host "PASS" -ForegroundColor Green
    } else {
        Write-Host "FAIL" -ForegroundColor Red
        $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    }

    [pscustomobject]@{
        Id        = $case.Id
        Category  = $case.Category
        Passed    = $passed
        Failures  = $failures.ToArray()
        Rationale = $case.Rationale
    }
}

# --- Summary -----------------------------------------------------------------------------
$passCount = @($results | Where-Object Passed).Count
$total = @($results).Count
Write-Host "`n================ Eval summary ================" -ForegroundColor Cyan
Write-Host "Passed $passCount / $total"

# Emit the result objects to the pipeline so callers/tests can assert on them.
$results

# Signal failure to CI via a non-zero exit code (not Write-Error, which would throw under
# $ErrorActionPreference='Stop' before the results are returned).
if ($passCount -lt $total) { exit 1 }
