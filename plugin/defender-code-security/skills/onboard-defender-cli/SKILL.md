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
- **Azure CLI (`az`)** — only required for **Path B** (ASPM auth-push). Path B installs it if missing; the install steps below and the legacy Path A do not need it.

## Step 1: Check if defender is already available

```powershell
defender --version
```

If the command succeeds, the CLI is already installed. Done — return to the calling skill.

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
`InstallSkills` (plus `EnsureAzureCli` in Path B). Note that the `Install` step still downloads
Microsoft's `InstallCli.ps1` and verifies **its** Authenticode signature on Windows — that
remote installer is a separate trust boundary from this local script.

## Step 2: Install the CLI

Download Microsoft's `InstallCli.ps1`, verify its Authenticode signature (Windows), and run it
to install the `defender` binary to `~/.mdc/`:

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

## Step 4: Install the bundled Copilot skills

The `defender` binary ships with the companion Copilot CLI skills embedded inside it. Install
them into the local Copilot skills folder (`$HOME/.copilot/skills`) so they are available to the
agent:

```powershell
& $bootstrap -Step InstallSkills
```

This runs `defender agent --install`, which always overwrites existing files so the installed
skills match the CLI version. It is **idempotent** — re-running this skill after a CLI upgrade
refreshes the installed Copilot skills to match.

After this completes, the `run-security-scan` and `fix-security-issues` skills are available to
the Copilot CLI.

## Step 5: Authenticate

The CLI exposes two distinct authentication paths. **Path B (ASPM auth-push) is the default — set it up now during onboarding.** Path A is **deferred**: it is needed only for the local scan commands (`scan image`, `scan fs`, `scan model`, `scan sbom`) and should **not** be set up during onboarding. The `run-security-scan` skill routes the user back here for Path A on demand, the first time they run a local scan.

| Scan type | Auth path | When to set up |
|-----------|-----------|----------------|
| `status result --latest`, `scan ai-scan submit` | **Path B — ASPM auth-push** (interactive `az login` to the DfD FPA). | **Now — default.** |
| `scan image`, `scan fs`, `scan model`, `scan sbom` | **Path A — legacy `defender auth login`** (uses `GDN_MDC_CLI_*`). | On demand — only when a local scan is requested. |

By default, complete **Path B** now and stop. Set up **Path A** only if the user explicitly asks to run a local scan during onboarding.

---

### Path A — Legacy `defender auth login` (image / fs / model / sbom) — on-demand only

> **Do not run Path A during initial onboarding.** It is required only when the user actually
> invokes a local scan (`scan image` / `scan fs` / `scan model` / `scan sbom`); the
> `run-security-scan` skill routes back here at that point. If you are onboarding for the first
> time, complete **Path B** (below) and stop.

**First-time configuration** — before the very first `defender auth login`, two environment variables must be set. These persist across sessions; skip this step if they are already defined.

| Variable | Value |
|----------|-------|
| `GDN_MDC_CLI_CLIENT_ID` | The Azure-based integration resource app's **client ID** (provided by the user) |
| `GDN_MDC_CLI_TENANT_ID` | The **Azure tenant ID** the user logs into (provided by the user) |

> **Where these values come from:** the `GDN_MDC_CLI_CLIENT_ID` and `GDN_MDC_CLI_TENANT_ID` are issued by the team's DfD onboarding admin (the app registration that grants the CLI access). The skill cannot guess them. If the user does not have them, point them at the [Defender for Cloud CLI install doc](https://learn.microsoft.com/en-us/azure/defender-for-cloud/defender-cli-install) or their internal onboarding instructions and stop until the values are available.

Gather the two values from the user, then run the bundled script's `AuthLegacy` phase. It persists them (session + `User` env on Windows, or an idempotent `export` in the shell rc file on Linux/macOS), runs the interactive login, and verifies `defender auth status`:

```powershell
# Re-resolve $bootstrap if authenticating in a fresh session (see "The bundled bootstrap
# script" section):  $bootstrap = Join-Path "<skill-dir>" "scripts/bootstrap.ps1"
& $bootstrap -Step AuthLegacy -ClientId "<client-id>" -TenantId "<tenant-id>"
```

If the values are already set in `GDN_MDC_CLI_CLIENT_ID` / `GDN_MDC_CLI_TENANT_ID`, you may omit the parameters — the phase falls back to those env vars and throws (naming what is missing) if neither source supplies them.

Wait for the browser-based login to complete. The phase prints `defender auth status`. **Auth status must succeed before proceeding.** If it shows no active session, re-run the same command.

---

### Path B — ASPM auth-push for `defender status result --latest` or `scan ai-scan` (interactive `az login` to FPA) — default

> **This is the default authentication path — always set it up during onboarding.** It covers
> `defender status result --latest` and `defender scan ai-scan submit`. No client secret is needed.

#### B-pre. Ensure Azure CLI is installed

Path B authenticates via `az login`, so the `az` CLI must be on PATH. Use the bundled script's
`EnsureAzureCli` phase — it checks for `az` and installs it (winget/MSI on Windows, Homebrew on
macOS, the Microsoft apt script on Debian/Ubuntu) if missing:

```powershell
# Re-resolve $bootstrap here if authenticating in a fresh session (see "The bundled
# bootstrap script" section above):  $bootstrap = Join-Path "<skill-dir>" "scripts/bootstrap.ps1"
& $bootstrap -Step EnsureAzureCli
```

If the phase throws (e.g., on an unsupported Linux distro such as RHEL/Fedora, or macOS without
Homebrew), install `az` manually per the [Azure CLI install docs](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
and retry — Path B cannot proceed without `az`.

> **Linux note:** the Debian/Ubuntu install path runs `curl ... | sudo bash`. In a
> non-interactive/agent shell `sudo` will hang on the password prompt, and the pipe hides its
> exit code. On Linux, run `-Step EnsureAzureCli` only where passwordless `sudo` is configured,
> or install `az` manually beforehand.

#### B0. Select the DfD data tenant

The FPA-scoped login needs the tenant id of the tenant onboarded with DfD. List the tenants the
user can access with the bundled script's `ListTenants` phase — it runs a baseline `az login` if
no account is cached, then emits the candidates as objects (`index`, `tenantId`, `label`, ...):

```powershell
# Re-resolve $bootstrap if authenticating in a fresh session.
& $bootstrap -Step ListTenants
```

> **Surface the choice to the user through the agent UI** (e.g., `vscode_askQuestions`) and have
> them confirm which tenant is onboarded with DfD. If only one tenant is returned, use it.
> Picking the wrong tenant makes the FPA token request fail with `AADSTS500011` ("resource
> principal named ... not found").

#### B1. Authenticate against the FPA and set the tenant env var

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
| `bootstrap.ps1` not found | The script ships with this skill at `scripts/bootstrap.ps1`. Resolve `$bootstrap` relative to this skill's directory (see "The bundled bootstrap script") |
| `-Step Install` — `Invoke-WebRequest` download fails | Check network connectivity; verify `cli.dfd.security.azure.com` is reachable |
| `-Step Install` — Authenticode `NotSigned` / `HashMismatch` / non-Microsoft signer | Do NOT proceed. The bundled script aborts automatically on Windows. Report failure to the user; re-run `-Step Install` to re-download and re-verify |
| `-Step Verify` — `defender` not on PATH | Open a new terminal so the persistent PATH is picked up, then re-run `-Step Verify`. To patch the current session manually: Windows: `$env:PATH += ";$HOME\.mdc"`; Linux/macOS: `$env:PATH += ":$HOME/.mdc"` |
 | `-Step AuthLegacy` — "Missing required value(s)" | Gather `GDN_MDC_CLI_CLIENT_ID` / `GDN_MDC_CLI_TENANT_ID` from the user (issued by the DfD onboarding admin) and pass them as `-ClientId` / `-TenantId` |
 | `-Step AuthAspm` — `az login` fails with `AADSTS500011` | Wrong tenant. Re-run `-Step ListTenants`, have the user confirm the DfD-onboarded tenant, then re-run `-Step AuthAspm -TenantId <confirmed>` |
 | Linux/macOS — `defender: permission denied` | `chmod +x ~/.mdc/defender` |
