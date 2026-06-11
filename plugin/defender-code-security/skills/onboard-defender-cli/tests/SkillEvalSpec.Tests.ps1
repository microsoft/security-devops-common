<#
.SYNOPSIS
    Deterministic validation of the behavioral eval spec and the eval runner.

.DESCRIPTION
    These tests do NOT call an LLM. They assert that:
      * evals/skill-evals.psd1 parses and every case has the required, well-typed fields.
      * Case Ids are unique and Categories are from the allowed set.
      * Every regex in ExpectMatch / ForbidMatch compiles.
      * Invoke-SkillEvals.ps1 honors -ListOnly, filters, and correctly applies assertions
        against a mock transcript supplied via -Invoker (no Copilot CLI required).

    This is the CI-safe half of the behavioral evals — it catches spec rot and runner bugs even
    where no agent is available. Run the actual prompts with evals/Invoke-SkillEvals.ps1.

    Run:  Invoke-Pester -Path ./tests/SkillEvalSpec.Tests.ps1
#>

BeforeAll {
    $script:EvalsDir = (Resolve-Path (Join-Path $PSScriptRoot '..' 'evals')).Path
    $script:SpecPath = Join-Path $script:EvalsDir 'skill-evals.psd1'
    $script:Runner   = Join-Path $script:EvalsDir 'Invoke-SkillEvals.ps1'
    $script:Spec     = Import-PowerShellDataFile -Path $script:SpecPath
    $script:Cases    = @($script:Spec.Cases)
    $script:AllowedCategories = 'Trigger', 'NoTrigger', 'Flow', 'Safety'
}

Describe 'Eval spec shape' {
    It 'parses to a hashtable with a non-empty Cases array' {
        $script:Cases.Count | Should -BeGreaterThan 0
    }

    It 'has unique case Ids' {
        $ids = $script:Cases.Id
        ($ids | Sort-Object -Unique).Count | Should -Be $ids.Count
    }

    It 'every case has Id, Category, Prompt, ExpectMatch, ForbidMatch, Rationale' {
        foreach ($c in $script:Cases) {
            $c.Keys | Should -Contain 'Id'
            $c.Keys | Should -Contain 'Category'
            $c.Keys | Should -Contain 'Prompt'
            $c.Keys | Should -Contain 'ExpectMatch'
            $c.Keys | Should -Contain 'ForbidMatch'
            $c.Keys | Should -Contain 'Rationale'
        }
    }

    It 'every Category is from the allowed set' {
        foreach ($c in $script:Cases) {
            $script:AllowedCategories | Should -Contain $c.Category -Because "case '$($c.Id)' has category '$($c.Category)'"
        }
    }

    It 'every Prompt and Rationale is a non-empty string' {
        foreach ($c in $script:Cases) {
            [string]::IsNullOrWhiteSpace($c.Prompt)    | Should -BeFalse -Because "case '$($c.Id)' Prompt"
            [string]::IsNullOrWhiteSpace($c.Rationale) | Should -BeFalse -Because "case '$($c.Id)' Rationale"
        }
    }

    It 'every ExpectMatch / ForbidMatch regex compiles' {
        foreach ($c in $script:Cases) {
            foreach ($rx in (@($c.ExpectMatch) + @($c.ForbidMatch))) {
                if ($rx) { { [regex]::new($rx) } | Should -Not -Throw -Because "case '$($c.Id)' regex /$rx/" }
            }
        }
    }

    It 'has at least one Trigger and one NoTrigger case' {
        ($script:Cases | Where-Object Category -eq 'Trigger').Count   | Should -BeGreaterThan 0
        ($script:Cases | Where-Object Category -eq 'NoTrigger').Count | Should -BeGreaterThan 0
    }
}

Describe 'Invoke-SkillEvals runner (mock invoker)' {
    It '-ListOnly lists cases without invoking an agent' {
        $out = & $script:Runner -ListOnly | Out-String
        $out | Should -Match 'trigger-command-not-found'
    }

    It 'filters by -Category' {
        $invoker = { param($p) 'irrelevant' }
        # Safety category: prompt response below satisfies its ExpectMatch ("stop") and avoids ForbidMatch.
        $res = & $script:Runner -Category Safety -Invoker { param($p) 'You should stop and not proceed.' }
        @($res).Count | Should -Be (@($script:Cases | Where-Object Category -eq 'Safety').Count)
        @($res) | ForEach-Object { $_.Category | Should -Be 'Safety' }
    }

    It 'PASSES a case when the transcript satisfies ExpectMatch and avoids ForbidMatch' {
        # Transcript that includes both required tokens for the explicit-install trigger case.
        $res = & $script:Runner -Id 'trigger-explicit-install' `
            -Invoker { param($p) 'Run & $bootstrap -Step Install then & $bootstrap -Step Verify.' }
        $res.Passed | Should -BeTrue
    }

    It 'FAILS a case when an ExpectMatch token is missing' {
        $res = & $script:Runner -Id 'trigger-explicit-install' -Invoker { param($p) 'I will do nothing useful.' }
        $res.Passed | Should -BeFalse
        ($res.Failures -join ' ') | Should -Match 'expected \(missing\)'
    }

    It 'FAILS a NoTrigger case when forbidden onboarding tokens appear' {
        $res = & $script:Runner -Id 'no-trigger-already-installed-scan' `
            -Invoker { param($p) 'First run & $bootstrap -Step Install to onboard.' }
        $res.Passed | Should -BeFalse
        ($res.Failures -join ' ') | Should -Match 'forbidden \(present\)'
    }
}
