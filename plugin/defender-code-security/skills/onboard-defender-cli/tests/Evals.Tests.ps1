#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
    Deterministic validation of evals/evals.json against the skill-creator
    eval schema (references/schemas.md → evals.json). No LLM required: this only
    checks that the eval spec is well-formed so the behavioral harness (run via
    skill-creator's subagent benchmark) has a valid input.

    Schema (per skill-creator references/schemas.md):
      {
        "skill_name": "onboard-defender-cli",
        "evals": [
          { "id": 1, "prompt": "...", "expected_output": "...",
            "files": ["..."], "expectations": ["...", "..."] }
        ]
      }
#>

BeforeDiscovery {
    $skillRoot = Split-Path -Parent $PSScriptRoot
    $script:EvalsPath = Join-Path $skillRoot 'evals' 'evals.json'
    $DiscoveryEvals = @()
    if (Test-Path $script:EvalsPath) {
        $spec = Get-Content $script:EvalsPath -Raw | ConvertFrom-Json
        $DiscoveryEvals = @($spec.evals)
    }
}

BeforeAll {
    $script:SkillRoot   = Split-Path -Parent $PSScriptRoot
    $script:SkillName   = Split-Path -Leaf $script:SkillRoot
    $script:EvalsPath   = Join-Path $script:SkillRoot 'evals' 'evals.json'
    $script:Raw         = Get-Content $script:EvalsPath -Raw
    $script:Spec        = $script:Raw | ConvertFrom-Json
    $script:Evals       = @($script:Spec.evals)
}

Describe 'evals/evals.json — spec shape' {
    It 'exists at evals/evals.json' {
        Test-Path $script:EvalsPath | Should -BeTrue
    }

    It 'is valid JSON' {
        { $script:Raw | ConvertFrom-Json } | Should -Not -Throw
    }

    It 'skill_name matches the skill directory name' {
        $script:Spec.skill_name | Should -Be $script:SkillName
    }

    It 'has a non-empty evals array' {
        $script:Evals.Count | Should -BeGreaterThan 0
    }

    It 'has unique, integer eval ids' {
        $ids = @($script:Evals | ForEach-Object { $_.id })
        foreach ($id in $ids) {
            ($id -is [int] -or $id -is [long]) | Should -BeTrue -Because "id '$id' must be an integer"
        }
        ($ids | Select-Object -Unique).Count | Should -Be $ids.Count
    }
}

Describe 'eval <_.id>' -ForEach $DiscoveryEvals {
    BeforeAll { $eval = $_ }

    It 'has a non-empty prompt' {
        [string]::IsNullOrWhiteSpace($eval.prompt) | Should -BeFalse
    }

    It 'has a non-empty expected_output' {
        [string]::IsNullOrWhiteSpace($eval.expected_output) | Should -BeFalse
    }

    It 'has a files field that is a list of path strings (may be empty)' {
        $eval.PSObject.Properties.Name | Should -Contain 'files'
        foreach ($f in @($eval.files)) {
            $f | Should -BeOfType [string]
        }
    }

    It 'has at least one expectation' {
        @($eval.expectations).Count | Should -BeGreaterThan 0
    }

    It 'has only non-empty expectation strings' {
        foreach ($x in @($eval.expectations)) {
            [string]::IsNullOrWhiteSpace($x) | Should -BeFalse
        }
    }
}
