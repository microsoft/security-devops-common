---
name: onboard-defender-cli
description: |
  Install and verify the Microsoft Defender for Cloud CLI (`defender`) on the local machine.
  Downloads the standalone binary to `~/.mdc/`, verifies the install script's Authenticode
  signature on Windows, and adds the binary to PATH. Use when the `defender` command is missing
  or out of date, or when a user explicitly asks to install/onboard/set up the Defender CLI.
  Triggers: "install defender cli", "onboard defender cli", "set up defender cli",
  "defender not found", "defender: command not found", "install mdc cli", "download defender cli",
  "InstallCli.ps1", "get defender cli".
---

# Defender CLI — Onboarding & Installation

Install the Microsoft Defender for Cloud CLI (`defender`) on the local machine. The CLI is a standalone binary installed to `~/.mdc/`.

## When to Use

- The `defender` command is not on PATH
- A scan skill (e.g., `run-security-scan`) reports the CLI is missing
- The user explicitly asks to install, onboard, set up, or download the Defender CLI

## Prerequisites

- **PowerShell** (any platform — PowerShell Core or Windows PowerShell)

## Step 1: Check if defender is already available

```powershell
defender --version
```

If the command succeeds, the CLI is already installed. Done — return to the calling skill.

## Step 2: Download the install script

```powershell
$scriptPath = Join-Path ([System.IO.Path]::GetTempPath()) "InstallCli.ps1"
Invoke-RestMethod -Uri "https://cli.dfd.security.stage.azure-test.net/public/v2/latest/InstallCli.ps1" -OutFile $scriptPath
```

## Step 2b: Verify the script signature

The mechanism depends on the OS — `Get-AuthenticodeSignature` is Windows-only.

**Windows** — verify the Authenticode signature:

```powershell
if ($IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop') {
    $sig = Get-AuthenticodeSignature $scriptPath
    if ($sig.Status -ne 'Valid') {
        throw "InstallCli.ps1 signature is $($sig.Status) — aborting. Do NOT run this script."
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
$BaseUrl = "cli.dfd.security.stage.azure-test.net"
& $scriptPath
```

To install a specific version instead of latest:

```powershell
$BaseUrl = "cli.dfd.security.stage.azure-test.net"
& $scriptPath -CliVersion "3.0.12345"
```

The script handles:

- OS detection (Windows, Linux, macOS)
- Architecture detection (x64, ARM64, x86)
- Downloading the binary to `~/.mdc/`
- Setting executable permissions on Linux/macOS
- Adding `~/.mdc/` to PATH (current session + persistent)

## Step 3: Verify

Restart the terminal if PATH was just added, then:

```powershell
defender --version
```

## Step 4: Install the bundled Copilot skills

The `defender` binary ships with the companion Copilot CLI skills embedded inside it. Install them into the local Copilot skills folder (`$HOME/.copilot/skills`) so they are available to the agent:

```powershell
defender agent --install
```

Pass `--dest <path>` to install to a non-default location. The command always overwrites existing files so the installed skills match the CLI version.

After this completes, the `run-security-scan` and `fix-security-issues` skills are available to the Copilot CLI.

## After Installation

Once `defender --version` succeeds and `defender agent --install` has run, return to the calling skill (e.g., [`run-security-scan`](../run-security-scan/SKILL.md)) to continue the original task (scanning, authentication, etc.).

## Error Handling

| Error | Resolution |
|-------|------------|
| `Invoke-RestMethod` download fails | Check network connectivity; verify the download URL is reachable |
| Authenticode signature `NotSigned` / `HashMismatch` | Do NOT run the script. Report failure to the user. Re-download and re-verify |
| `& $scriptPath` fails with `$BaseUrl` validation error | Ensure `$BaseUrl = "cli.dfd.security.stage.azure-test.net"` is set in the same scope before invocation |
| PATH not picked up | Restart the terminal session, or `$env:PATH += ";$HOME\.mdc"` for the current session |
| Linux/macOS — `defender: permission denied` | `chmod +x ~/.mdc/defender` |
