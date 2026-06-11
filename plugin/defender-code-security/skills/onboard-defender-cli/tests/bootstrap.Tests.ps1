<#
.SYNOPSIS
    Pester 5 unit tests for the onboard-defender-cli bundled bootstrap.ps1.

.DESCRIPTION
    Validates the deterministic logic of bootstrap.ps1 in isolation: parameter validation,
    the Authenticode signature gate, env-var persistence, tenant validation, and the native
    error-propagation helper. All network / native / interactive operations are mocked, so the
    suite is hermetic and safe to run anywhere (Windows, Linux, macOS, CI).

    The script is dot-sourced so its functions are available without running the -Step dispatch
    (the dispatch is guarded by `$MyInvocation.InvocationName -ne '.'`). -Step is supplied only to
    satisfy the mandatory parameter; no phase actually runs on dot-source.

    Run:  Invoke-Pester -Path ./tests/bootstrap.Tests.ps1
#>

BeforeAll {
    $script:BootstrapPath = Join-Path $PSScriptRoot '..' 'scripts' 'bootstrap.ps1'
    $script:BootstrapPath = (Resolve-Path $script:BootstrapPath).Path

    # Dot-source to import the functions. -Step is mandatory; the value is irrelevant because the
    # dispatch is skipped when dot-sourced.
    . $script:BootstrapPath -Step Verify

    # Snapshot env vars the suite mutates so we can restore them afterwards.
    $script:SavedEnv = @{}
    foreach ($name in 'GDN_MDC_CLI_CLIENT_ID', 'GDN_MDC_CLI_TENANT_ID', 'DEFENDER_DFD_TENANT_ID', 'SHELL') {
        $script:SavedEnv[$name] = [Environment]::GetEnvironmentVariable($name)
    }
}

AfterAll {
    foreach ($name in $script:SavedEnv.Keys) {
        Set-Item "env:$name" -Value $script:SavedEnv[$name] -ErrorAction SilentlyContinue
        if ($null -eq $script:SavedEnv[$name]) {
            Remove-Item "env:$name" -ErrorAction SilentlyContinue
        }
    }
}

Describe 'Parameter validation (-Step ValidateSet)' {
    It 'rejects an unknown -Step value' {
        { & $script:BootstrapPath -Step 'NotAStep' } | Should -Throw
    }

    It 'accepts every documented step name' {
        $expected = 'EnsureAzureCli', 'Install', 'Verify', 'InstallSkills', 'AuthLegacy', 'ListTenants', 'AuthAspm'
        $attr = (Get-Command $script:BootstrapPath).Parameters['Step'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
        $attr.ValidValues | Should -Be $expected
    }
}

Describe 'Format-TenantLabel' {
    It 'prefers the display name' {
        Format-TenantLabel ([pscustomobject]@{ name = 'Contoso'; defaultDomain = 'contoso.onmicrosoft.com'; tenantId = 'tid' }) |
            Should -Be 'Contoso'
    }

    It 'falls back to defaultDomain when name is empty' {
        Format-TenantLabel ([pscustomobject]@{ name = ''; defaultDomain = 'contoso.onmicrosoft.com'; tenantId = 'tid' }) |
            Should -Be 'contoso.onmicrosoft.com'
    }

    It 'falls back to tenantId when name and domain are empty' {
        Format-TenantLabel ([pscustomobject]@{ name = $null; defaultDomain = $null; tenantId = 'tid-123' }) |
            Should -Be 'tid-123'
    }
}

Describe 'Invoke-Native' {
    BeforeDiscovery {
        # -Skip is evaluated during discovery, so detect pwsh here (not in BeforeAll).
        $script:HasPwsh = [bool](Get-Command pwsh -ErrorAction SilentlyContinue)
    }

    It 'returns without throwing on exit code 0' -Skip:(-not $script:HasPwsh) {
        { Invoke-Native -FilePath 'pwsh' -Arguments @('-NoProfile', '-NonInteractive', '-Command', 'exit 0') } |
            Should -Not -Throw
    }

    It 'throws on a non-zero exit code' -Skip:(-not $script:HasPwsh) {
        { Invoke-Native -FilePath 'pwsh' -Arguments @('-NoProfile', '-NonInteractive', '-Command', 'exit 7') } |
            Should -Throw '*exit code 7*'
    }
}

Describe 'Add-PersistentExport' {
    BeforeEach {
        # PowerShell uses dynamic scoping: overriding $HOME here makes the dot-sourced function
        # read it. $HOME is read-only, so -Force is required to shadow it for the test.
        $script:FakeHome = Join-Path $TestDrive ([guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $script:FakeHome -Force | Out-Null
        Set-Variable -Name HOME -Value $script:FakeHome -Force
        $env:SHELL = $null
    }

    It 'appends an export line when the variable is not present' {
        Add-PersistentExport 'MY_VAR' 'hello'
        $rc = Get-Content (Join-Path $script:FakeHome '.bashrc')
        $rc | Should -Contain "export MY_VAR='hello'"
    }

    It 'rewrites the existing export in place rather than appending a duplicate' {
        Add-PersistentExport 'MY_VAR' 'first'
        Add-PersistentExport 'MY_VAR' 'second'
        $rc = @(Get-Content (Join-Path $script:FakeHome '.bashrc'))
        ($rc | Where-Object { $_ -match '^export MY_VAR=' }).Count | Should -Be 1
        $rc | Should -Contain "export MY_VAR='second'"
        $rc | Should -Not -Contain "export MY_VAR='first'"
    }

    It "escapes embedded single quotes using the '\'' bash idiom" {
        Add-PersistentExport 'MY_VAR' "a'b"
        $rc = Get-Content (Join-Path $script:FakeHome '.bashrc')
        $rc | Should -Contain "export MY_VAR='a'\''b'"
    }

    It 'writes to .zshrc when SHELL is zsh' {
        $env:SHELL = '/usr/bin/zsh'
        Add-PersistentExport 'MY_VAR' 'z'
        Test-Path (Join-Path $script:FakeHome '.zshrc')  | Should -BeTrue
        Test-Path (Join-Path $script:FakeHome '.bashrc') | Should -BeFalse
    }
}

Describe 'Invoke-Install — Authenticode signature gate (Windows)' {
    BeforeEach {
        # Avoid any real download / installer execution.
        Mock Invoke-WebRequest {}
        Mock Test-IsWindows { $true }
    }

    It 'aborts when InstallCli.ps1 is not validly signed' {
        Mock Get-AuthenticodeSignature {
            [pscustomobject]@{ Status = 'NotSigned'; SignerCertificate = [pscustomobject]@{ Subject = 'CN=Unknown' } }
        }
        { Invoke-Install } | Should -Throw '*not validly signed by Microsoft*'
    }

    It 'aborts when the signer is not Microsoft Corporation even if the chain is Valid' {
        Mock Get-AuthenticodeSignature {
            [pscustomobject]@{ Status = 'Valid'; SignerCertificate = [pscustomobject]@{ Subject = 'CN=Evil, O=Evil Corp' } }
        }
        { Invoke-Install } | Should -Throw '*not validly signed by Microsoft*'
    }
}

Describe 'Invoke-Verify' {
    It "throws a PATH-hint error when 'defender' is not resolvable" {
        Mock Get-Command { $null } -ParameterFilter { $Name -eq 'defender' }
        { Invoke-Verify } | Should -Throw "*not on PATH*"
    }
}

Describe 'Get-AzTenant' {
    It "throws a clear error when 'az' is not on PATH" {
        Mock Get-Command { $null } -ParameterFilter { $Name -eq 'az' }
        { Get-AzTenant } | Should -Throw "*'az' is not on PATH*"
    }
}

Describe 'Invoke-AuthLegacy (Path A)' {
    BeforeEach {
        Remove-Item env:GDN_MDC_CLI_CLIENT_ID -ErrorAction SilentlyContinue
        Remove-Item env:GDN_MDC_CLI_TENANT_ID -ErrorAction SilentlyContinue
        # The function reads the script-level $ClientId/$TenantId parameters via dynamic scope,
        # not its own args. Clear them so each test controls them explicitly.
        $ClientId = $null
        $TenantId = $null
        # Treat as non-Windows so persistence routes through the mockable Add-PersistentExport
        # instead of writing to the real Windows User environment.
        Mock Test-IsWindows { $false }
        Mock Add-PersistentExport {}
        Mock Invoke-Native {}
    }

    It 'throws naming both missing values when neither params nor env vars are set' {
        { Invoke-AuthLegacy } | Should -Throw '*Missing required value*'
    }

    It 'names only the missing value when one is supplied' {
        $ClientId = 'cid'
        { Invoke-AuthLegacy } | Should -Throw '*TenantId*'
    }

    It 'persists supplied values and runs login + status' {
        $ClientId = 'cid-1'
        $TenantId = 'tid-1'
        Invoke-AuthLegacy
        $env:GDN_MDC_CLI_CLIENT_ID | Should -Be 'cid-1'
        $env:GDN_MDC_CLI_TENANT_ID | Should -Be 'tid-1'
        Should -Invoke Add-PersistentExport -Times 2
        # defender auth login + defender auth status
        Should -Invoke Invoke-Native -Times 2
    }

    It 'falls back to env vars when params are omitted' {
        $env:GDN_MDC_CLI_CLIENT_ID = 'env-cid'
        $env:GDN_MDC_CLI_TENANT_ID = 'env-tid'
        { Invoke-AuthLegacy } | Should -Not -Throw
        Should -Invoke Invoke-Native -Times 2
    }
}

Describe 'Invoke-AuthAspm (Path B)' {
    BeforeEach {
        Remove-Item env:DEFENDER_DFD_TENANT_ID -ErrorAction SilentlyContinue
        # The function reads the script-level $TenantId parameter via dynamic scope.
        $TenantId = $null
        Mock Test-IsWindows { $false }
        Mock Add-PersistentExport {}
        Mock Invoke-AzLogin {}
        Mock Get-AzTenant {
            @(
                [pscustomobject]@{ tenantId = '11111111-1111-1111-1111-111111111111' },
                [pscustomobject]@{ tenantId = '22222222-2222-2222-2222-222222222222' }
            )
        }
    }

    It 'throws when -TenantId is missing' {
        { Invoke-AuthAspm } | Should -Throw '*Missing -TenantId*'
    }

    It 'throws when -TenantId is not among the accessible tenants (index-instead-of-id guard)' {
        $TenantId = '0'
        { Invoke-AuthAspm } | Should -Throw '*not among the tenants*'
    }

    It 'logs in and persists DEFENDER_DFD_TENANT_ID for a valid tenant' {
        $TenantId = '11111111-1111-1111-1111-111111111111'
        Invoke-AuthAspm
        $env:DEFENDER_DFD_TENANT_ID | Should -Be '11111111-1111-1111-1111-111111111111'
        Should -Invoke Invoke-AzLogin -Times 1
        Should -Invoke Add-PersistentExport -Times 1
    }

    It 'does NOT persist the tenant when login fails' {
        $TenantId = '11111111-1111-1111-1111-111111111111'
        Mock Invoke-AzLogin { throw 'AADSTS500011' }
        { Invoke-AuthAspm } | Should -Throw '*AADSTS500011*'
        $env:DEFENDER_DFD_TENANT_ID | Should -BeNullOrEmpty
        Should -Invoke Add-PersistentExport -Times 0
    }
}
