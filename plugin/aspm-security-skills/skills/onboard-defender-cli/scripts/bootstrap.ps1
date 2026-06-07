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

function Add-PersistentExport {
    # Persist an env var on Linux/macOS by appending an idempotent `export` line to the
    # user's shell rc file. On Windows, [Environment]::SetEnvironmentVariable(...,'User')
    # handles persistence instead, so this is not used there.
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $Value
    )
    $rcName = if ($env:SHELL -and $env:SHELL -match 'zsh') { '.zshrc' } else { '.bashrc' }
    $rc = Join-Path $HOME $rcName
    $line = "export $Name=$Value"
    if (-not (Test-Path $rc) -or -not (Select-String -Path $rc -SimpleMatch $line -Quiet)) {
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
    # Ensure az has at least one cached account so tenants can be enumerated. Use
    # $LASTEXITCODE — `az account show` writes JSON to stdout when logged in, which
    # evaluates as truthy regardless of success.
    az account show 2>$null 1>$null
    if ($LASTEXITCODE -ne 0) {
        az login | Out-Null
    }
    $tenants = az account list `
        --query "[].{tenantId:tenantId, name:tenantDisplayName, defaultDomain:tenantDefaultDomain}" `
        -o json | ConvertFrom-Json | Sort-Object tenantId -Unique
    $tenants = @($tenants)
    if ($tenants.Count -eq 0) {
        throw "No tenants found via 'az account list'. Run 'az login' manually and retry."
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
        brew update
        brew install azure-cli
    } elseif ($IsLinux) {
        # Debian / Ubuntu. RHEL-family users should follow the manual repo steps
        # documented in the skill instead.
        bash -c 'curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash'
    } else {
        throw "Unsupported platform for automatic Azure CLI install. Install 'az' manually and retry."
    }

    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        throw "Azure CLI install ran but 'az' is still not on PATH. Open a new terminal and retry."
    }
    Write-Host "Azure CLI installed."
}

function Invoke-Install {
    $scriptPath = Join-Path ([System.IO.Path]::GetTempPath()) 'InstallCli.ps1'
    Invoke-RestMethod -Uri $InstallScriptUrl -OutFile $scriptPath

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
    } finally {
        $ErrorActionPreference = $prevEap
        Remove-Item $scriptPath -ErrorAction SilentlyContinue
    }
}

function Invoke-Verify {
    if (-not (Get-Command defender -ErrorAction SilentlyContinue)) {
        throw ("'defender' is not on PATH. InstallCli.ps1 updated the persistent PATH; " +
               "open a new terminal so it is picked up, then retry.")
    }
    defender --version
}

function Invoke-InstallSkills {
    # Idempotent: always overwrites so installed Copilot skills match the CLI version.
    defender agent --install
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

    defender auth login --interactive-login
    defender auth status
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

    # FPA-scoped login: the --scope <fpa-app-id>/Defender.InteractiveLogin requests a delegated
    # token whose `aud` is the FPA. --allow-no-subscriptions is required because the FPA app is
    # not bound to any Azure subscription. A generic `az login` will not produce an accepted token.
    az login `
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
