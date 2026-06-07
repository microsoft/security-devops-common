---
name: onboard-defender-cli
description: |
  Install, authenticate, and verify the Defender for Cloud CLI (`defender`) on the local machine.
  Downloads the standalone binary to `~/.mdc/`, verifies the install script's Authenticode
  signature on Windows, adds the binary to PATH, installs the bundled Copilot skills, and walks
  the user through the two authentication paths (legacy `defender auth login` and ASPM auth-push
  via `az login`). Use when the `defender` command is missing or out of date, when the user
  has never authenticated, or when a user explicitly asks to install/onboard/set up/authenticate
  the Defender for Cloud CLI.
  Triggers: "install defender cli", "onboard defender cli", "set up defender cli",
  "defender not found", "defender: command not found", "install aspm cli", "download defender cli",
  "InstallCli.ps1", "get defender cli", "defender auth", "defender login", "authenticate defender",
  "set up defender auth".
---

# Defender for Cloud CLI — Onboarding & Installation

Install the Defender for Cloud CLI (`defender`) on the local machine. The CLI is a standalone binary installed to `~/.mdc/`.

For official documentation, see:

- [Defender for Cloud CLI overview](https://learn.microsoft.com/en-us/azure/defender-for-cloud/defender-cli-overview)
- [Install the Defender for Cloud CLI](https://learn.microsoft.com/en-us/azure/defender-for-cloud/defender-cli-install)

## When to Use

- The `defender` command is not on PATH
- A scan skill (e.g., `run-security-scan`) reports the CLI is missing
- The user explicitly asks to install, onboard, set up, or download the Defender for Cloud CLI

## Prerequisites

- **PowerShell** (any platform — PowerShell Core or Windows PowerShell)
- **Azure CLI (`az`)** — Step 1b below checks for it and installs it if missing.

## Step 1: Check if defender is already available

```powershell
defender --version
```

If the command succeeds, the CLI is already installed. Done — return to the calling skill.

## Step 1b: Ensure Azure CLI is installed

The `defender scan ai-scan` commands authenticate via `az login`, so the `az` CLI must be on PATH.

```powershell
az --version
```

If the command succeeds, skip to Step 2. Otherwise install it for the current platform:

**Windows** — install via `winget` (preferred) or fall back to the official MSI:

```powershell
if (Get-Command winget -ErrorAction SilentlyContinue) {
    winget install --exact --id Microsoft.AzureCLI --silent --accept-package-agreements --accept-source-agreements
} else {
    $msi = Join-Path ([System.IO.Path]::GetTempPath()) "AzureCLI.msi"
    Invoke-WebRequest -Uri "https://aka.ms/installazurecliwindows" -OutFile $msi
    Start-Process msiexec.exe -Wait -ArgumentList "/I `"$msi`" /quiet"
    Remove-Item $msi
}
```

**macOS** — install via Homebrew:

```powershell
brew update; brew install azure-cli
```

**Linux** — use the official Microsoft install script:

```powershell
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash    # Debian / Ubuntu
# RHEL / Fedora / CentOS:
# sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
# curl -sL https://packages.microsoft.com/config/rhel/9/prod.repo | sudo tee /etc/yum.repos.d/azure-cli.repo
# sudo dnf install -y azure-cli
```

After installation, restart the terminal (or refresh PATH) and re-run `az --version` to confirm. If it still fails, report the install failure to the user and stop — the ai-scan flow cannot proceed without `az`.

## Step 2: Download the install script

```powershell
$scriptPath = Join-Path ([System.IO.Path]::GetTempPath()) "InstallCli.ps1"
Invoke-RestMethod -Uri "https://cli.dfd.security.azure.com/public/latest/InstallCli.ps1" -OutFile $scriptPath
```

## Step 2b: Verify the script signature

The mechanism depends on the OS — `Get-AuthenticodeSignature` is Windows-only.

**Windows** — verify the Authenticode signature:

```powershell
if ($IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop') {
    $sig = Get-AuthenticodeSignature $scriptPath
    # 'Valid' only means the signature chains to a trusted root — not that
    # Microsoft signed it. Also assert the signer is Microsoft Corporation.
    if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        throw "InstallCli.ps1 is not validly signed by Microsoft (status=$($sig.Status), signer=$($sig.SignerCertificate.Subject)) — aborting. Do NOT run this script."
    }
    Write-Host "Signature valid — signed by: $($sig.SignerCertificate.Subject)"
}
```

**Linux / macOS** — Authenticode validation is not available. Skip the check and warn the user:

```powershell
if (-not ($IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop')) {
    Write-Warning "Skipping Authenticode signature check — not supported on $($PSVersionTable.OS). The script will be executed as-is from $scriptPath."
}
```

> **TODO:** A cross-platform integrity mechanism (e.g., a published SHA-256 manifest or a detached GPG signature alongside `InstallCli.ps1`) is not yet available. When one is published, replace the Linux/macOS branch with a real verification step.

**If Windows verification fails**, do NOT run the script. Report the failure to the user and stop.

## Step 2c: Run the verified script

The script uses an internal `$BaseUrl` variable that is validated but not declared as a parameter.
Set it in the calling scope so the validation passes:

```powershell
# Set BaseUrl to satisfy the script's internal validation check
$BaseUrl = "cli.dfd.security.azure.com"
& $scriptPath
```

To install a specific version instead of latest:

```powershell
$BaseUrl = "cli.dfd.security.azure.com"
& $scriptPath -CliVersion "3.0.12345"
```

The script handles:

- OS detection (Windows, Linux, macOS)
- Architecture detection (x64, ARM64, x86)
- Downloading the binary to `~/.mdc/`
- Setting executable permissions on Linux/macOS
- Adding `~/.mdc/` to PATH (current session + persistent)

## Step 3: Verify

`InstallCli.ps1` updates the persistent PATH but does not always refresh the current session. Refresh it in-process, then verify:

```powershell
# Prepend ~/.mdc to PATH for the current session if not already present
$mdcDir = Join-Path $HOME ".mdc"
if (-not ($env:PATH -split [IO.Path]::PathSeparator | Where-Object { $_ -eq $mdcDir })) {
    $env:PATH = $mdcDir + [IO.Path]::PathSeparator + $env:PATH
}

defender --version
```

If `defender` still cannot be resolved, restart the terminal and re-run `defender --version`.

## Step 4: Install the bundled Copilot skills

The `defender` binary ships with the companion Copilot CLI skills embedded inside it. Install them into the local Copilot skills folder (`$HOME/.copilot/skills`) so they are available to the agent:

```powershell
defender agent --install
```

Pass `--dest <path>` to install to a non-default location. The command always overwrites existing files so the installed skills match the CLI version. This is **idempotent** — re-running this skill after a CLI upgrade refreshes the installed Copilot skills to match.

After this completes, the `run-security-scan` and `fix-security-issues` skills are available to the Copilot CLI.

## Step 5: Authenticate

The CLI exposes two distinct authentication paths. Set up the one(s) matching the scans the user plans to run. If unsure, set up both.

| Scan type | Auth path |
|-----------|-----------|
| `scan image`, `scan fs`, `scan model`, `scan sbom` | **Path A — legacy `defender auth login`** (uses `GDN_MDC_CLI_*`). |
| `status result --latest`, `scan ai-scan submit` | **Path B — ASPM auth-push** (interactive `az login` to the DfD FPA). |

---

### Path A — Legacy `defender auth login` (image / fs / model / sbom)

**First-time configuration** — before the very first `defender auth login`, two environment variables must be set. These persist across sessions; skip this step if they are already defined.

| Variable | Value |
|----------|-------|
| `GDN_MDC_CLI_CLIENT_ID` | The Azure-based integration resource app's **client ID** (provided by the user) |
| `GDN_MDC_CLI_TENANT_ID` | The **Azure tenant ID** the user logs into (provided by the user) |

> **Where these values come from:** the `GDN_MDC_CLI_CLIENT_ID` and `GDN_MDC_CLI_TENANT_ID` are issued by the team's DfD onboarding admin (the app registration that grants the CLI access). The skill cannot guess them. If the user does not have them, point them at the [Defender for Cloud CLI install doc](https://learn.microsoft.com/en-us/azure/defender-for-cloud/defender-cli-install) or their internal onboarding instructions and stop until the values are available.

Check whether both are set (the agent should reason on `$missing`, not the printed value):

```powershell
$missing = @()
if (-not $env:GDN_MDC_CLI_CLIENT_ID) { $missing += 'GDN_MDC_CLI_CLIENT_ID' }
if (-not $env:GDN_MDC_CLI_TENANT_ID) { $missing += 'GDN_MDC_CLI_TENANT_ID' }
if ($missing) {
    Write-Host "Missing required env vars: $($missing -join ', ')"
} else {
    Write-Host "GDN_MDC_CLI_CLIENT_ID and GDN_MDC_CLI_TENANT_ID are set."
}
```

If either is empty, ask the user for the values, then set them persistently:

```powershell
# Persistent (current user) — Windows
[Environment]::SetEnvironmentVariable("GDN_MDC_CLI_CLIENT_ID", "<client-id>", "User")
[Environment]::SetEnvironmentVariable("GDN_MDC_CLI_TENANT_ID", "<tenant-id>", "User")

# Also set in current session so login works immediately
$env:GDN_MDC_CLI_CLIENT_ID = "<client-id>"
$env:GDN_MDC_CLI_TENANT_ID = "<tenant-id>"
```

On Linux/macOS, append `export GDN_MDC_CLI_CLIENT_ID=<client-id>` and `export GDN_MDC_CLI_TENANT_ID=<tenant-id>` to `~/.bashrc` or `~/.zshrc`, then `source` it. **Make the append idempotent** so re-running the skill does not duplicate lines:

```bash
for kv in "GDN_MDC_CLI_CLIENT_ID=<client-id>" "GDN_MDC_CLI_TENANT_ID=<tenant-id>"; do
    grep -qxF "export $kv" ~/.bashrc || echo "export $kv" >> ~/.bashrc
done
```

Then run interactive login and verify auth status:

```powershell
defender auth login --interactive-login
```

Wait for the browser-based login to complete. Then verify:

```powershell
defender auth status
```

**Auth status must succeed before proceeding.** If `auth status` shows no active session, re-run `auth login --interactive-login`.

---

### Path B — ASPM auth-push for `defender status result --latest` or `scan ai-scan` (interactive `az login` to FPA)

Use this only when the user is running `defender status result --latest` or `defender scan ai-scan submit`. No client secret is needed.

#### B0. Discover the DfD data tenant id

The FPA-scoped `az login` in B1 requires a tenant id (`<DFD_DATA_TENANT_ID>`). Discover it from the `az` cache.

> **Agents driving this skill:** when there are multiple tenants, surface the choice through the agent UI (e.g., `vscode_askQuestions`) instead of relying on the `Read-Host` fallback below — `Read-Host` blocks on stdin and cannot be answered programmatically. The `Read-Host` branch is the human-shell fallback only.

```powershell
# Ensure az has at least one account cached. If not, run a baseline `az login`
# (no scope, no tenant) first so the tenant list can be enumerated. Use
# $LASTEXITCODE — `az account show` writes JSON to stdout when logged in,
# which evaluates as truthy regardless of success.
az account show 2>$null 1>$null
if ($LASTEXITCODE -ne 0) {
    az login | Out-Null
}

# Build a deduplicated list of tenants the user has access to. tenantDisplayName
# can be empty for some tenants — fall back to defaultDomain, then tenantId.
$tenants = az account list `
    --query "[].{tenantId:tenantId, name:tenantDisplayName, defaultDomain:tenantDefaultDomain}" `
    -o json `
    | ConvertFrom-Json `
    | Sort-Object tenantId -Unique

if (-not $tenants -or $tenants.Count -eq 0) {
    throw "No tenants found via 'az account list'. Run 'az login' manually and retry."
}

function Format-TenantLabel($t) {
    if ($t.name)            { return $t.name }
    elseif ($t.defaultDomain) { return $t.defaultDomain }
    else                    { return $t.tenantId }
}

if ($tenants.Count -eq 1) {
    $tenantId = $tenants[0].tenantId
    Write-Host "Using the only available tenant: $(Format-TenantLabel $tenants[0]) ($tenantId)"
} else {
    Write-Host "Multiple tenants are available:"
    for ($i = 0; $i -lt $tenants.Count; $i++) {
        Write-Host ("  [{0}] {1}  ({2})" -f $i, (Format-TenantLabel $tenants[$i]), $tenants[$i].tenantId)
    }
    $choice = Read-Host "Select the DfD data tenant index"
    # Cast once: '0' is a valid index, and a string comparison like '10' -ge '3'
    # would otherwise let out-of-range values through.
    $idx = $choice -as [int]
    if ($null -eq $idx -or $idx -lt 0 -or $idx -ge $tenants.Count) {
        throw "Invalid selection: $choice"
    }
    $tenantId = $tenants[$idx].tenantId
    Write-Host "Using tenant: $(Format-TenantLabel $tenants[$idx]) ($tenantId)"
}
```

`$tenantId` now holds the value to use as `<DFD_DATA_TENANT_ID>` in B1 and B2. The user must confirm the selected tenant is the one onboarded with DfD — picking a wrong tenant will cause the FPA token request to fail with `AADSTS500011` ("resource principal named ... not found").

#### B1. Run `az login` against the FPA

```powershell
# DfD First-Party Application (FPA) app id — published constant.
# Update this single value if the FPA is rotated.
$fpaAppId = "b1a78a13-a596-4366-b37d-406048fa4a23"

az login `
  --tenant $tenantId `
  --scope "$fpaAppId/Defender.InteractiveLogin" `
  --allow-no-subscriptions
```

- `$tenantId` comes from B0.
- `$fpaAppId` is the DfD First-Party Application id; the `--scope <fpa-app-id>/Defender.InteractiveLogin` requests a delegated token whose `aud` is the FPA. A generic `az login` will not produce a token the router accepts.
- `--allow-no-subscriptions` is required because the FPA app is not bound to any Azure subscription.
- The signed-in user must be granted the FPA roles `AiScan.Upload.Role` and `AiScan.Enabled.Role` by an admin.

#### B2. Set the required environment variable

```powershell
$env:DEFENDER_DFD_TENANT_ID = $tenantId   # same value used in B1
# Persist for future sessions (Windows, current user):
[Environment]::SetEnvironmentVariable("DEFENDER_DFD_TENANT_ID", $tenantId, "User")
```

On Linux/macOS, persist the variable from the **PowerShell** side so the resolved `$tenantId` from B0 is substituted in (a literal `$tenantId` would expand to nothing in a separate bash shell and persist an empty value). Use a guarded append so the line is not duplicated on re-runs:

```powershell
$rc  = Join-Path $HOME ".bashrc"   # use .zshrc if the user's shell is zsh
$line = "export DEFENDER_DFD_TENANT_ID=$tenantId"
if (-not (Test-Path $rc) -or -not (Select-String -Path $rc -SimpleMatch $line -Quiet)) {
    Add-Content -Path $rc -Value $line
}
```

Then `source ~/.bashrc` (or `~/.zshrc`) in the user's shell to load it into the current session.

> Do **not** set `DEFENDER_ASPM_CLIENT_ID` or `DEFENDER_ASPM_CLIENT_SECRET` in this flow — their presence makes the CLI prefer the client-credentials path and ignore the `az` cache.

## After Installation

Once `defender --version` succeeds, `defender agent --install` has run, and the required auth path is set up, return to the calling skill (e.g., `run-security-scan`) to continue the original task.

### Suggested next prompts

Surface these to the user as ready-to-use chat prompts (not shell commands) once onboarding is complete:

- "Scan this repo for security issues" - presents ranked findings.
- "Scan this repo with the AI-powered scanner" — presents AI-powered findings.
- "Show me the latest AI scan results for this repo" — presents the latest AI scan results.
- "Scan the `<image>` container image for vulnerabilities" — presents image scan findings.
- "Generate an SBOM for `<image>`" — presents SBOM findings.
- "Scan this AI model directory" — presents AI model scan findings.

After any of these, the user can reply `fix` (or `fix #N #M`) to hand the top findings to the `fix-security-issues` skill.

## Error Handling

| Error | Resolution |
|-------|------------|
| `Invoke-RestMethod` download fails | Check network connectivity; verify the download URL is reachable |
| Authenticode signature `NotSigned` / `HashMismatch` | Do NOT run the script. Report failure to the user. Re-download and re-verify |
| `& $scriptPath` fails with `$BaseUrl` validation error | Ensure `$BaseUrl = "cli.dfd.security.azure.com"` is set in the same scope before invocation |
 | PATH not picked up | Restart the terminal session, or update PATH for the current session: Windows: `$env:PATH += ";$HOME\.mdc"`; Linux/macOS: `$env:PATH += ":$HOME/.mdc"` |
 | Linux/macOS — `defender: permission denied` | `chmod +x ~/.mdc/defender` |
