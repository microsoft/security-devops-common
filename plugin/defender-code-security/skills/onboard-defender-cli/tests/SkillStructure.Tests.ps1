<#
.SYNOPSIS
    Structural lint for the onboard-defender-cli SKILL.md and its bundled bootstrap.ps1.

.DESCRIPTION
    Deterministic, dependency-free checks that catch documentation drift and broken references
    without invoking an LLM:
      * SKILL.md has valid YAML frontmatter with name + description.
      * The frontmatter `name` matches the skill's directory name.
      * The bundled script referenced by SKILL.md exists on disk.
      * Every `-Step X` mentioned in SKILL.md is a real value in the script's ValidateSet,
        and every ValidateSet value is documented in SKILL.md (no drift in either direction).

    Run:  Invoke-Pester -Path ./tests/SkillStructure.Tests.ps1
#>

# Pester 5 runs BeforeDiscovery and BeforeAll in separate scopes and does NOT re-source the
# file for the run phase, so top-level functions are unavailable in BeforeAll. The path/step
# extraction is therefore inlined in both blocks (kept identical on purpose).
BeforeDiscovery {
    $dir               = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $bootstrap         = Join-Path $dir 'scripts' 'bootstrap.ps1'
    $skillMd           = Join-Path $dir 'SKILL.md'
    $script:ValidSteps = ((Get-Command $bootstrap).Parameters['Step'].Attributes |
        Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }).ValidValues
    $script:DocSteps   = @([regex]::Matches((Get-Content -Path $skillMd -Raw), '-Step\s+(\w+)') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
}

BeforeAll {
    $script:SkillDir     = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $script:SkillMd      = Join-Path $script:SkillDir 'SKILL.md'
    $script:Bootstrap    = Join-Path $script:SkillDir 'scripts' 'bootstrap.ps1'
    $script:MdText       = Get-Content -Path $script:SkillMd -Raw
    $script:ValidSteps   = ((Get-Command $script:Bootstrap).Parameters['Step'].Attributes |
        Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }).ValidValues
    $script:DocSteps     = @([regex]::Matches($script:MdText, '-Step\s+(\w+)') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    $script:SkillDirName = Split-Path $script:SkillDir -Leaf

    if ($script:MdText -match '(?s)^---\s*\r?\n(.*?)\r?\n---\s*\r?\n') {
        $script:Frontmatter = $Matches[1]
    } else {
        $script:Frontmatter = $null
    }
}

Describe 'SKILL.md frontmatter' {
    It 'starts with a YAML frontmatter block' {
        $script:Frontmatter | Should -Not -BeNullOrEmpty -Because 'SKILL.md must open with a --- fenced YAML block'
    }

    It 'declares a name' {
        $script:Frontmatter | Should -Match '(?m)^name:\s*\S'
    }

    It 'declares a description' {
        $script:Frontmatter | Should -Match '(?m)^description:'
    }

    It 'name matches the skill directory name' {
        $name = ([regex]::Match($script:Frontmatter, '(?m)^name:\s*(.+?)\s*$')).Groups[1].Value
        $name | Should -Be $script:SkillDirName
    }
}

Describe 'Bundled script reference' {
    It 'SKILL.md references scripts/bootstrap.ps1' {
        $script:MdText | Should -Match 'scripts/bootstrap\.ps1'
    }

    It 'the referenced bootstrap.ps1 exists on disk' {
        Test-Path $script:Bootstrap | Should -BeTrue
    }
}

Describe 'Step documentation drift' {
    It 'documents at least one -Step invocation' {
        $script:DocSteps.Count | Should -BeGreaterThan 0
    }

    It "every -Step '<_>' documented in SKILL.md is a real ValidateSet value" -ForEach $script:DocSteps {
        $script:ValidSteps | Should -Contain $_ -Because "SKILL.md references '-Step $_' but the script has no such phase"
    }

    It "every script phase '<_>' is documented in SKILL.md" -ForEach $script:ValidSteps {
        $script:DocSteps | Should -Contain $_ -Because "bootstrap.ps1 exposes '-Step $_' but SKILL.md never documents it"
    }
}
