---
name: onboard-defender-cli
description: |
  Install, authenticate, and verify the Defender for Cloud CLI (`defender`) on the local machine.
  Downloads the standalone binary to `~/.mdc/`, verifies the install script's Authenticode
  signature on Windows, adds the binary to PATH, installs the bundled agent skills, and
  authenticates to the ASPM API via an interactive `az login` (Azure CLI). Use when the
  `defender` command is missing or out of date, when the user has never authenticated, or when
  a user explicitly asks to install/onboard/set up/authenticate the Defender for Cloud CLI.
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
- **Azure CLI (`az`)** — required only for **authentication** (ASPM auth-push, Step 5). The auth step installs it if missing; the install/verify/skills steps do not need it.

## Step 1: Check the currently installed version (informational)

```powershell
defender --version
```

This is informational only — **do not stop if it succeeds.** Onboarding always installs the
latest CLI in Step 2, replacing any existing version, so a user on an old build is upgraded in
place. Note the reported version (or that the command was not found) and continue to Step 2.

## The bundled bootstrap script

The deterministic install phases below are driven by a PowerShell script shipped **with this
skill** at `scripts/bootstrap.ps1` (relative to this `SKILL.md`). It is a trusted plugin asset:
the skill runs it directly from disk and does **not** download or signature-check it — there is
no remote-code-execution surface to validate. Resolve its absolute path once, then call it with
a `-Step` argument for each phase:

```powershell
# Absolute path to the bundled script (this skill's own directory + scripts/bootstrap.ps1).
# Substitute the directory this SKILL.md lives in for <skill-dir>.
$bootstrap = Join-Path "<skill-dir>" "scripts/bootstrap.ps1"
```

Each `-Step` is idempotent and safe to re-run. Run them in order: `Install` → `Verify` →
`InstallSkills` (plus `EnsureAzureCli` for authentication). Note that the `Install` step still downloads
Microsoft's `InstallCli.ps1` and verifies **its** Authenticode signature on Windows — that
remote installer is a separate trust boundary from this local script.

## Step 2: Install the CLI

Download Microsoft's `InstallCli.ps1`, verify its Authenticode signature (Windows), and run it
to install the `defender` binary to `~/.mdc/`. This always installs the **latest** version,
overwriting any existing install — so re-running onboarding upgrades an out-of-date CLI in place:

```powershell
& $bootstrap -Step Install
```

To install a specific version instead of latest, pass `-CliVersion`:

```powershell
& $bootstrap -Step Install -CliVersion "3.0.12345"
```

The phase handles OS/architecture detection, downloading the binary to `~/.mdc/`, setting
executable permissions on Linux/macOS, and adding `~/.mdc/` to PATH (current session +
persistent). On Windows it aborts unless `InstallCli.ps1` is validly signed by Microsoft
Corporation; on Linux/macOS it warns that Authenticode validation is unavailable.

> **TODO:** A cross-platform integrity mechanism for `InstallCli.ps1` (e.g., a published SHA-256
> manifest or detached GPG signature) is not yet available. When one is published, update the
> Install phase of `bootstrap.ps1` to verify it on Linux/macOS.

**If the Install phase throws**, do NOT proceed. Report the failure to the user and stop.

## Step 3: Verify

```powershell
& $bootstrap -Step Verify
```

This confirms `defender --version` resolves on PATH. If it throws, open a new terminal (so the
updated PATH is picked up) and re-run `& $bootstrap -Step Verify`.

## Step 4: Install the bundled agent skills

The `defender` binary ships with the companion `run-security-scan` and `fix-security-issues`
skills embedded inside it. Install them into your agent's skills folder so they are available to
the agent:

```powershell
& $bootstrap -Step InstallSkills
```

This runs `defender agent --install`, which **auto-detects the installed agent harnesses** and
writes the skills into each one it finds (e.g. GitHub Copilot, Claude Code). It always overwrites
existing files so the installed skills match the CLI version, and is **idempotent** — re-running
this skill after a CLI upgrade refreshes the installed skills to match.

> Auto-detection covers the common case and adapts as the CLI adds support for new agents — you
> do not need to know which harnesses exist. To target a specific agent or location explicitly,
> run the CLI directly instead of the bootstrap phase: `defender agent --install --agent <name>`
> (run `defender agent --install --help` to list supported agents), or
> `defender agent --install --dest <path>`.

After this completes, the `run-security-scan` and `fix-security-issues` skills are available to
your agent.

## Step 5: Authenticate

Authentication signs the CLI in to the DfD First-Party App (FPA) via an interactive `az login`,
and is required **only** for the ASPM-backed commands — `defender status result --latest` and
`defender scan ai-scan *`. No client secret is needed. Set it up now during onboarding.

> **Local scans need no authentication.** `scan image`, `scan fs`, `scan model`, and `scan sbom`
> run fully against the local target without signing in. Unauthenticated, they simply do not
> publish results to Microsoft Defender for Cloud — the CLI prints a one-line
> `Running without authentication` warning and continues, which is the intended behavior. Do
> **not** set up authentication for local scans.

### Step 5a. Ensure Azure CLI is installed

Authentication runs `az login`, so the `az` CLI must be on PATH. Use the bundled script's
`EnsureAzureCli` phase — it checks for `az` and installs it (winget/MSI on Windows, Homebrew on
macOS, the Microsoft apt script on Debian/Ubuntu) if missing:

```powershell
# Re-resolve $bootstrap here if authenticating in a fresh session (see "The bundled
# bootstrap script" section above):  $bootstrap = Join-Path "<skill-dir>" "scripts/bootstrap.ps1"
& $bootstrap -Step EnsureAzureCli
```

If the phase throws (e.g., on an unsupported Linux distro such as RHEL/Fedora, or macOS without
Homebrew), install `az` manually per the [Azure CLI install docs](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
and retry — authentication cannot proceed without `az`.

> **Linux note:** the Debian/Ubuntu install path runs `curl ... | sudo bash`. In a
> non-interactive/agent shell `sudo` will hang on the password prompt, and the pipe hides its
> exit code. On Linux, run `-Step EnsureAzureCli` only where passwordless `sudo` is configured,
> or install `az` manually beforehand.

### Step 5b. Select the tenant

The FPA-scoped login needs the tenant id of the tenant to use. List the tenants the
user can access with the bundled script's `ListTenants` phase — it runs a baseline `az login` if
no account is cached, then emits the candidates as objects (`index`, `tenantId`, `label`, ...):

> **Logins try the browser first, then fall back to device code.** Both the baseline login here
> and the FPA login in Step 5c attempt an interactive browser `az login`; if that fails or doesn't
> complete within ~2 minutes (e.g. a headless/agent shell with no browser), the script
> automatically retries with `az login --use-device-code`. When the device-code path runs it
> prints a `https://microsoft.com/devicelogin` URL and a one-time code — **surface that URL and
> code to the user verbatim** and wait for them to complete sign-in. Do **not** improvise a
> different login flow or swap in `az account list`; let the phases run.

```powershell
# Re-resolve $bootstrap if authenticating in a fresh session.
& $bootstrap -Step ListTenants
```

> **Surface the choice to the user through the agent UI** (e.g., `vscode_askQuestions`) and have
> them confirm which tenant they would like to use. If only one tenant is returned, use it.
> Picking the wrong tenant makes the FPA token request fail with `AADSTS500011` ("resource
> principal named ... not found").

> **`az account tenant list` needs the `account` extension.** This phase enumerates tenants with
> `az account tenant list`, which lives in the `account` dynamic *preview* extension. The script
> pre-sets `extension.use_dynamic_install=yes_without_prompt` and
> `extension.dynamic_install_allow_preview=true` so az installs it silently. Do **not** improvise
> a workaround if you see an extension-install prompt — re-run `-Step ListTenants` (the settings
> are applied each run), or set those two `az config` values manually and retry. Never skip
> tenant selection.

### Step 5c. Authenticate against the FPA and set the tenant env var

Pass the confirmed `tenantId` to the `AuthAspm` phase. It runs the FPA-scoped `az login`
(`--scope <fpa-app-id>/Defender.InteractiveLogin --allow-no-subscriptions`) and sets
`DEFENDER_DFD_TENANT_ID` (session + persistent):

```powershell
& $bootstrap -Step AuthAspm -TenantId "<confirmed-tenant-id>"
```

- The FPA app id is a published constant baked into `bootstrap.ps1`; update it there if the FPA is rotated.
- `--allow-no-subscriptions` is required because the FPA app is not bound to any Azure subscription; a generic `az login` will not produce a token the router accepts.
- The signed-in user must be granted the FPA roles `AiScan.Upload.Role` and `AiScan.Enabled.Role` by an admin.

> Do **not** set `DEFENDER_ASPM_CLIENT_ID` or `DEFENDER_ASPM_CLIENT_SECRET` in this flow — their presence makes the CLI prefer the client-credentials path and ignore the `az` cache.

## After Installation

Once `defender --version` succeeds, `defender agent --install` has run, and authentication is set up, return to the calling skill (e.g., `run-security-scan`) to continue the original task.

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
| `bootstrap.ps1` not found | The script ships with this skill at `scripts/bootstrap.ps1`. Resolve `$bootstrap` relative to this skill's directory (see "The bundled bootstrap script") |
| `-Step Install` — `Invoke-WebRequest` download fails | Check network connectivity; verify `cli.dfd.security.azure.com` is reachable |
| `-Step Install` — Authenticode `NotSigned` / `HashMismatch` / non-Microsoft signer | Do NOT proceed. The bundled script aborts automatically on Windows. Report failure to the user; re-run `-Step Install` to re-download and re-verify |
| `-Step Verify` — `defender` not on PATH | Open a new terminal so the persistent PATH is picked up, then re-run `-Step Verify`. To patch the current session manually: Windows: `$env:PATH += ";$HOME\.mdc"`; Linux/macOS: `$env:PATH += ":$HOME/.mdc"` |
 | `-Step AuthAspm` — `az login` fails with `AADSTS500011` | Wrong tenant. Re-run `-Step ListTenants`, have the user confirm the tenant, then re-run `-Step AuthAspm -TenantId <confirmed>` |
 | `-Step ListTenants` / `-Step AuthAspm` — az prompts to install the `account` extension (Y/n) or hangs | The `account` extension (for `az account tenant list`) is missing. The script auto-configures silent install; if you still see the prompt (older az / config not applied), run `az config set extension.use_dynamic_install=yes_without_prompt` and `az config set extension.dynamic_install_allow_preview=true`, then re-run the step. Do not bypass tenant selection. |
 | `-Step ListTenants` / `-Step AuthAspm` — `az login` hangs or fails with an interactive/browser prompt | A headless shell has no browser. The script tries browser login first and auto-falls back to `az login --use-device-code` after ~2 minutes; surface the printed `microsoft.com/devicelogin` URL + code to the user and wait for sign-in. Do not switch to a different login flow or use `az account list` to work around it. |
 | Linux/macOS — `defender: permission denied` | `chmod +x ~/.mdc/defender` |
