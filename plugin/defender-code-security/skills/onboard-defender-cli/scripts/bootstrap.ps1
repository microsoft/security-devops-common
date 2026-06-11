<#
.SYNOPSIS
    Bootstraps the Defender for Cloud CLI (`defender`). Bundled plugin asset for the
    onboard-defender-cli skill. Runs a single phase at a time, selected with -Step.

.DESCRIPTION
    This script is a TRUSTED plugin asset shipped inside the aspm-security-skills plugin.
    The skill runs it directly from disk — it is NOT downloaded or signature-checked at
    runtime, so there is no remote-code-execution surface to validate. Each -Step is an
    idempotent phase the skill invokes in order. Phases are intentionally separate so the
    agent can report progress, handle prompts, and recover between them.

    Note: the Install phase still downloads Microsoft's InstallCli.ps1 from
    cli.dfd.security.azure.com and verifies ITS Authenticode signature on Windows. That
    remote installer is a separate trust boundary from this local script.

.PARAMETER Step
    Which phase to run:
      EnsureAzureCli - install the Azure CLI (`az`) if missing. Needed only for Path B.
      Install        - download Microsoft's InstallCli.ps1, verify its Authenticode
                       signature on Windows, then run it to install the `defender` binary.
      Verify         - confirm `defender --version` resolves on PATH.
      InstallSkills  - run `defender agent --install` to install the bundled Copilot skills.
      AuthLegacy     - Path A. Persist GDN_MDC_CLI_* values and run `defender auth login`.
      ListTenants    - Path B. Enumerate the Azure tenants the user can access (for selection).
      AuthAspm       - Path B. Run the FPA-scoped `az login` and set DEFENDER_DFD_TENANT_ID.

.PARAMETER CliVersion
    Optional. Install a specific `defender` CLI version instead of latest. Install step only.

.PARAMETER ClientId
    AuthLegacy (Path A) only. The integration resource app's client id (user-supplied, issued by
    the DfD onboarding admin). Falls back to $env:GDN_MDC_CLI_CLIENT_ID if omitted.

.PARAMETER TenantId
    AuthLegacy (Path A) and AuthAspm (Path B). The Azure tenant id (user-supplied / user-confirmed).
    AuthLegacy falls back to $env:GDN_MDC_CLI_TENANT_ID if omitted; AuthAspm requires it explicitly.

.EXAMPLE
    ./bootstrap.ps1 -Step EnsureAzureCli

.EXAMPLE
    ./bootstrap.ps1 -Step Install -CliVersion 3.0.12345

.EXAMPLE
    ./bootstrap.ps1 -Step AuthLegacy -ClientId <client-id> -TenantId <tenant-id>

.EXAMPLE
    ./bootstrap.ps1 -Step ListTenants
    ./bootstrap.ps1 -Step AuthAspm -TenantId <confirmed-tenant-id>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('EnsureAzureCli', 'Install', 'Verify', 'InstallSkills',
                 'AuthLegacy', 'ListTenants', 'AuthAspm')]
    [string] $Step,

    # Install step only.
    [string] $CliVersion,

    # AuthLegacy (Path A): user-supplied; falls back to $env:GDN_MDC_CLI_CLIENT_ID.
    [string] $ClientId,

    # AuthLegacy (Path A) and AuthAspm (Path B): user-supplied / user-confirmed tenant id.
    [string] $TenantId
)

$ErrorActionPreference = 'Stop'

$InstallScriptUrl = 'https://cli.dfd.security.azure.com/public/v2/latest/InstallCli.ps1'
$BaseUrlValue     = 'cli.dfd.security.azure.com'

# DfD First-Party Application (FPA) app id — published constant. Update if the FPA is rotated.
$FpaAppId = 'b1a78a13-a596-4366-b37d-406048fa4a23'

function Test-IsWindows {
    # $IsWindows is PowerShell Core only; on Windows PowerShell 5.1 it is $null,
    # in which case PSEdition 'Desktop' identifies Windows.
    return ($IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop')
}

function Invoke-Native {
    # Run a native command and throw on a non-zero exit code. $ErrorActionPreference='Stop'
    # does NOT make native commands throw on Windows PowerShell 5.1 / PS 7.0-7.3, so an
    # `az`/`defender` failure would otherwise look like success. Route all native calls
    # through here so a failed login/install fails the phase loudly.
    param(
        [Parameter(Mandatory)][string] $FilePath,
        [Parameter(ValueFromRemainingArguments)][string[]] $Arguments
    )
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$FilePath $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }
}

function Invoke-AzLogin {
    # Prefer an interactive browser login, but fall back to the device-code flow if the
    # browser attempt fails or does not complete in time. A browser login hangs in a
    # headless/agent shell that has no browser to open, so the browser attempt is bounded
    # by a timeout: if it does not finish, we cancel it and retry with --use-device-code
    # (which prints a URL + one-time code that works headlessly). The same login args
    # (tenant / scope / --allow-no-subscriptions / ...) are passed through to both attempts.
    param(
        [Parameter(ValueFromRemainingArguments)][string[]] $Arguments
    )

    $browserTimeoutSec = 120

    # Run the browser attempt as a background job so a missing browser can't hang the phase
    # forever. The job inherits PATH + the az token cache (~/.azure), so a successful login
    # there persists for the subsequent `az account ...` calls in this process.
    Write-Host "Attempting interactive browser login (falls back to device code after ${browserTimeoutSec}s)..."
    $job = Start-Job -ScriptBlock {
        param($AzArgs)
        $out = & az @AzArgs 2>&1 | Out-String
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $out }
    } -ArgumentList (, (@('login') + $Arguments))

    $result = $null
    if (Wait-Job $job -Timeout $browserTimeoutSec) {
        $result = Receive-Job $job
    } else {
        Write-Host "Browser login did not complete within ${browserTimeoutSec}s — falling back to device code."
        Stop-Job $job -ErrorAction SilentlyContinue
    }
    Remove-Job $job -Force -ErrorAction SilentlyContinue

    if ($result -and $result.ExitCode -eq 0) {
        if ($result.Output) { Write-Host $result.Output.Trim() }
        return
    }
    if ($result -and $result.Output) {
        Write-Host "Browser login failed; falling back to device code:`n$($result.Output.Trim())"
    }

    # Fallback: device-code flow in the foreground so the user sees the URL + code live.
    # Invoke-Native so a cancelled/expired device-code login fails the phase loudly.
    Invoke-Native az login @Arguments --use-device-code
}

function Add-PersistentExport {
    # Persist an env var on Linux/macOS by writing an idempotent `export` line to the
    # user's shell rc file. On Windows, [Environment]::SetEnvironmentVariable(...,'User')
    # handles persistence instead, so this is not used there.
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $Value
    )
    # macOS bash login shells read ~/.bash_profile (not ~/.bashrc); zsh reads ~/.zshrc on
    # both platforms. Pick the file the user's next session will actually source.
    if ($env:SHELL -and $env:SHELL -match 'zsh') {
        $rcName = '.zshrc'
    } elseif ($IsMacOS) {
        $rcName = '.bash_profile'
    } else {
        $rcName = '.bashrc'
    }
    $rc = Join-Path $HOME $rcName

    # Single-quote and escape the value so nothing in it (e.g. $(...) command
    # substitution) is expanded when the shell sources the rc file. Each embedded
    # single quote becomes the standard '\'' bash idiom (close-quote, escaped quote,
    # reopen-quote).
    $escaped = $Value -replace "'", "'\''"
    $line = "export $Name='$escaped'"

    # Key idempotency on the variable NAME, not the whole line: if the value changes on a
    # re-run (e.g. a different tenant), rewrite the existing export in place instead of
    # appending a second conflicting one.
    if (Test-Path $rc) {
        $existing = Get-Content -Path $rc
        $pattern  = "^\s*export\s+$([regex]::Escape($Name))="
        if ($existing -match $pattern) {
            $updated = $existing | ForEach-Object {
                if ($_ -match $pattern) { $line } else { $_ }
            }
            Set-Content -Path $rc -Value $updated
        } else {
            Add-Content -Path $rc -Value $line
        }
    } else {
        Add-Content -Path $rc -Value $line
    }
    Write-Host "Persisted $Name to $rc (run 'source $rc' to load it into your current shell)."
}

function Format-TenantLabel($t) {
    if ($t.name)              { return $t.name }
    elseif ($t.defaultDomain) { return $t.defaultDomain }
    else                      { return $t.tenantId }
}

function Get-AzTenant {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        throw ("'az' is not on PATH. Run this script with '-Step EnsureAzureCli' first " +
               "(or install the Azure CLI manually), then retry.")
    }
    # Ensure az has at least one cached account so tenants can be enumerated. Use
    # $LASTEXITCODE — `az account show` writes JSON to stdout when logged in, which
    # evaluates as truthy regardless of success.
    az account show 2>$null 1>$null
    if ($LASTEXITCODE -ne 0) {
        # Browser login first, with an automatic device-code fallback for headless/agent
        # shells (see Invoke-AzLogin). --allow-no-subscriptions so a user whose only access
        # is the subscription-less DfD data tenant can still sign in.
        Invoke-AzLogin --allow-no-subscriptions
    }
    # `az account tenant list` is provided by the `account` dynamic *preview* extension, which
    # is not installed by default. Without the two settings below, az interactively prompts
    # ("The command requires the extension account. Do you want to install it now? (Y/n):"),
    # which hangs in a non-interactive/agent shell and makes this phase fail. Configure az to
    # install the extension silently (incl. preview) so the command runs unattended. Both are
    # best-effort: suppress output and ignore failures so an older az without these keys still
    # proceeds (it will just prompt, matching prior behaviour).
    az config set extension.use_dynamic_install=yes_without_prompt --only-show-errors 2>$null 1>$null
    az config set extension.dynamic_install_allow_preview=true --only-show-errors 2>$null 1>$null
    # Use `az account tenant list`, not `az account list`: the latter only returns tenants
    # that have a subscription. Path B logs in with --allow-no-subscriptions, so the DfD
    # data tenant may have none and would be invisible here. `az account tenant list`
    # enumerates every tenant the user can access regardless of subscriptions.
    $tenants = az account tenant list `
        --query "[].{tenantId:tenantId, name:displayName, defaultDomain:defaultDomain}" `
        -o json | ConvertFrom-Json | Sort-Object tenantId -Unique
    $tenants = @($tenants)
    if ($tenants.Count -eq 0) {
        throw "No tenants found via 'az account tenant list'. Run 'az login' manually and retry."
    }
    return $tenants
}

function Invoke-EnsureAzureCli {
    if (Get-Command az -ErrorAction SilentlyContinue) {
        Write-Host "Azure CLI already installed."
        return
    }

    Write-Host "Azure CLI not found — installing..."
    if (Test-IsWindows) {
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            winget install --exact --id Microsoft.AzureCLI --silent `
                --accept-package-agreements --accept-source-agreements
        } else {
            $msi = Join-Path ([System.IO.Path]::GetTempPath()) 'AzureCLI.msi'
            Invoke-WebRequest -Uri 'https://aka.ms/installazurecliwindows' -OutFile $msi
            Start-Process msiexec.exe -Wait -ArgumentList "/I `"$msi`" /quiet"
            Remove-Item $msi -ErrorAction SilentlyContinue
        }
    } elseif ($IsMacOS) {
        if (-not (Get-Command brew -ErrorAction SilentlyContinue)) {
            throw ("Homebrew ('brew') is required to install the Azure CLI on macOS but was " +
                   "not found. Install Homebrew (https://brew.sh) or install 'az' manually, then retry.")
        }
        # No `brew update` — it's a slow full tap-sync; `brew install` resolves the formula on its own.
        brew install azure-cli
    } elseif ($IsLinux) {
        # Debian / Ubuntu only. RHEL/Fedora/CentOS users should install 'az' manually per
        # https://learn.microsoft.com/cli/azure/install-azure-cli-linux and re-run.
        bash -c 'curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash'
    } else {
        throw "Unsupported platform for automatic Azure CLI install. Install 'az' manually and retry."
    }

    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        # winget/MSI update the machine PATH, but the current process won't see it until a
        # new shell starts. On Windows, reload PATH from the machine + user scopes and
        # re-check before giving up; only warn (don't throw) if it still isn't visible.
        if (Test-IsWindows) {
            $machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
            $userPath    = [Environment]::GetEnvironmentVariable('Path', 'User')
            $env:PATH = (@($machinePath, $userPath) | Where-Object { $_ }) -join ';'
            if (Get-Command az -ErrorAction SilentlyContinue) {
                Write-Host "Azure CLI installed."
                return
            }
            Write-Warning ("Azure CLI was installed but 'az' is not yet visible in this " +
                           "session. Open a new terminal so the updated PATH is picked up, " +
                           "then continue.")
            return
        }
        throw "Azure CLI install ran but 'az' is still not on PATH. Open a new terminal and retry."
    }
    Write-Host "Azure CLI installed."
}

function Invoke-Install {
    $scriptPath = Join-Path ([System.IO.Path]::GetTempPath()) 'InstallCli.ps1'
    # Invoke-RestMethod -OutFile is PS6+; use Invoke-WebRequest for Windows PowerShell 5.1
    # compatibility (this skill supports Windows PowerShell).
    if ($PSVersionTable.PSEdition -eq 'Desktop') {
        Invoke-WebRequest -Uri $InstallScriptUrl -OutFile $scriptPath -UseBasicParsing
    } else {
        Invoke-WebRequest -Uri $InstallScriptUrl -OutFile $scriptPath
    }

    if (Test-IsWindows) {
        $sig = Get-AuthenticodeSignature $scriptPath
        # 'Valid' only means the signature chains to a trusted root — not that
        # Microsoft signed it. Also assert the signer is Microsoft Corporation.
        if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
            throw ("InstallCli.ps1 is not validly signed by Microsoft " +
                   "(status=$($sig.Status), signer=$($sig.SignerCertificate.Subject)) — aborting. " +
                   "Do NOT run this script.")
        }
        Write-Host "Signature valid — signed by: $($sig.SignerCertificate.Subject)"
    } else {
        Write-Warning ("Skipping Authenticode signature check — not supported on " +
                       "$($PSVersionTable.OS). InstallCli.ps1 will be executed as-is from $scriptPath.")
    }

    # InstallCli.ps1 validates an internal `$BaseUrl` read from the caller's scope but
    # does not declare it as a parameter. Set it locally so the call operator's child
    # scope can resolve it.
    $BaseUrl = $BaseUrlValue

    # Don't let our strict error preference change how Microsoft's installer behaves.
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($CliVersion) {
            & $scriptPath -CliVersion $CliVersion
        } else {
            & $scriptPath
        }
        # InstallCli.ps1 is a native-ish script invocation; with EAP forced to 'Continue'
        # a non-zero exit would otherwise be swallowed and only surface later as a
        # misleading "defender not on PATH" from Verify. Fail fast here instead.
        if ($LASTEXITCODE -ne 0) {
            throw "InstallCli.ps1 exited with code $LASTEXITCODE. Check the installer output above."
        }
    } finally {
        $ErrorActionPreference = $prevEap
        Remove-Item $scriptPath -ErrorAction SilentlyContinue
    }

    # Confirm the binary was actually written — guards against an installer that returns
    # 0 but did not produce the binary.
    $mdcDir = Join-Path $HOME '.mdc'
    if (-not (Test-Path (Join-Path $mdcDir 'defender')) -and
        -not (Test-Path (Join-Path $mdcDir 'defender.exe'))) {
        throw "InstallCli.ps1 completed but the defender binary was not found under $mdcDir. Check the installer output and retry."
    }
}

function Invoke-Verify {
    if (-not (Get-Command defender -ErrorAction SilentlyContinue)) {
        throw ("'defender' is not on PATH. InstallCli.ps1 updated the persistent PATH; " +
               "open a new terminal so it is picked up, then retry.")
    }
    Invoke-Native defender --version
}

function Invoke-InstallSkills {
    # Idempotent: always overwrites so installed Copilot skills match the CLI version.
    Invoke-Native defender agent --install
}

function Invoke-AuthLegacy {
    # User-supplied values; fall back to already-set env vars.
    if (-not $ClientId) { $ClientId = $env:GDN_MDC_CLI_CLIENT_ID }
    if (-not $TenantId) { $TenantId = $env:GDN_MDC_CLI_TENANT_ID }

    $missing = @()
    if (-not $ClientId) { $missing += 'ClientId (GDN_MDC_CLI_CLIENT_ID)' }
    if (-not $TenantId) { $missing += 'TenantId (GDN_MDC_CLI_TENANT_ID)' }
    if ($missing.Count -gt 0) {
        throw ("Missing required value(s): $($missing -join ', '). These are issued by your DfD " +
               "onboarding admin — gather them from the user, pass -ClientId/-TenantId, then retry.")
    }

    # Persist for future sessions + set in the current process.
    $env:GDN_MDC_CLI_CLIENT_ID = $ClientId
    $env:GDN_MDC_CLI_TENANT_ID = $TenantId
    if (Test-IsWindows) {
        [Environment]::SetEnvironmentVariable('GDN_MDC_CLI_CLIENT_ID', $ClientId, 'User')
        [Environment]::SetEnvironmentVariable('GDN_MDC_CLI_TENANT_ID', $TenantId, 'User')
    } else {
        Add-PersistentExport 'GDN_MDC_CLI_CLIENT_ID' $ClientId
        Add-PersistentExport 'GDN_MDC_CLI_TENANT_ID' $TenantId
    }

    # Run login + status through Invoke-Native so a cancelled/expired login (native
    # non-zero exit) fails the phase instead of silently returning success.
    Invoke-Native defender auth login --interactive-login
    Invoke-Native defender auth status
}

function Invoke-ListTenants {
    # Emit the available tenants as objects so an agent can present the choice via its UI,
    # or a human can read the table and pass the chosen tenantId to -Step AuthAspm.
    $tenants = Get-AzTenant
    for ($i = 0; $i -lt $tenants.Count; $i++) {
        $t = $tenants[$i]
        [pscustomobject]@{
            index         = $i
            tenantId      = $t.tenantId
            label         = (Format-TenantLabel $t)
            name          = $t.name
            defaultDomain = $t.defaultDomain
        }
    }
}

function Invoke-AuthAspm {
    if (-not $TenantId) {
        throw ("Missing -TenantId. Run '-Step ListTenants' first, have the user confirm the DfD " +
               "data tenant, then pass it as -TenantId. Picking the wrong tenant causes the FPA " +
               "token request to fail with AADSTS500011.")
    }

    # Validate -TenantId against the tenants the user can actually access. ListTenants and
    # AuthAspm are separate phases, so the list is gone by now — re-enumerate and assert
    # membership. This catches a transposed GUID, or the agent passing the printed `index`
    # instead of the tenantId, up front instead of failing later as AADSTS500011.
    $validIds = (Get-AzTenant).tenantId
    if ($TenantId -notin $validIds) {
        throw ("-TenantId '$TenantId' is not among the tenants you can access: " +
               "$($validIds -join ', '). Re-run '-Step ListTenants' and pass the tenantId " +
               "(not the index) of the DfD-onboarded tenant.")
    }

    # FPA-scoped login: the --scope <fpa-app-id>/Defender.InteractiveLogin requests a delegated
    # token whose `aud` is the FPA. --allow-no-subscriptions is required because the FPA app is
    # not bound to any Azure subscription. A generic `az login` will not produce an accepted token.
    # Invoke-AzLogin tries browser login first and falls back to device code for headless/agent
    # shells. It runs through Invoke-Native, so a failed/cancelled login (e.g. wrong tenant /
    # AADSTS500011) throws and does NOT fall through to persist a bad DEFENDER_DFD_TENANT_ID.
    Invoke-AzLogin `
        --tenant $TenantId `
        --scope "$FpaAppId/Defender.InteractiveLogin" `
        --allow-no-subscriptions

    $env:DEFENDER_DFD_TENANT_ID = $TenantId
    if (Test-IsWindows) {
        [Environment]::SetEnvironmentVariable('DEFENDER_DFD_TENANT_ID', $TenantId, 'User')
    } else {
        Add-PersistentExport 'DEFENDER_DFD_TENANT_ID' $TenantId
    }
    Write-Host "DEFENDER_DFD_TENANT_ID set to $TenantId (session + persistent)."
}

switch ($Step) {
    'EnsureAzureCli' { Invoke-EnsureAzureCli }
    'Install'        { Invoke-Install }
    'Verify'         { Invoke-Verify }
    'InstallSkills'  { Invoke-InstallSkills }
    'AuthLegacy'     { Invoke-AuthLegacy }
    'ListTenants'    { Invoke-ListTenants }
    'AuthAspm'       { Invoke-AuthAspm }
}
