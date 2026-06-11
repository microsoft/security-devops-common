# onboard-defender-cli — tests & evals

Two layers of validation for this skill:

| Layer | Location | Deterministic? | Needs LLM/CLI? |
|-------|----------|----------------|----------------|
| Pester unit tests for `scripts/bootstrap.ps1` | `tests/bootstrap.Tests.ps1` | Yes | No |
| Structural lint for `SKILL.md` (frontmatter + doc/script drift) | `tests/SkillStructure.Tests.ps1` | Yes | No |
| Eval-spec + runner validation (mock invoker) | `tests/SkillEvalSpec.Tests.ps1` | Yes | No |
| Behavioral evals (prompt → agent behavior) | `evals/skill-evals.psd1` + `evals/Invoke-SkillEvals.ps1` | No | Yes (Copilot CLI) |

## Prerequisites

- **PowerShell 7+** and **Pester 5** for the deterministic suite:

  ```powershell
  Install-Module Pester -MinimumVersion 5.0.0 -Scope CurrentUser -SkipPublisherCheck
  ```

- **GitHub Copilot CLI** (`copilot`) — only for the live behavioral evals.

## Run the deterministic suite (CI-safe, no LLM)

```powershell
Invoke-Pester -Path ./tests -Output Detailed
```

This covers:

- **bootstrap.ps1 logic** — `-Step` validation, the Authenticode signature gate, env-var
  persistence/idempotency, tenant validation (the index-vs-id guard), and native error
  propagation. All network/native/interactive calls are mocked, so it is hermetic.
- **SKILL.md drift** — frontmatter shape, the `name`/directory match, the bundled-script
  reference, and that every `-Step` documented in `SKILL.md` maps 1:1 to the script's
  `ValidateSet` (and vice-versa).
- **Eval spec + runner** — the eval cases parse and are well-formed; the runner's
  ExpectMatch/ForbidMatch assertion logic works against a mock transcript.

> `tests/bootstrap.Tests.ps1` dot-sources `scripts/bootstrap.ps1`. The script's `-Step`
> dispatch is guarded by `if ($MyInvocation.InvocationName -ne '.')` so dot-sourcing only
> defines the functions and never runs a phase.

## Run the behavioral evals (live, needs Copilot CLI)

The evals send each prompt in `evals/skill-evals.psd1` to an agent that has this skill loaded
and check the transcript against per-case regexes.

```powershell
# List the cases without calling an agent:
./evals/Invoke-SkillEvals.ps1 -ListOnly

# Run a subset:
./evals/Invoke-SkillEvals.ps1 -Category Trigger
./evals/Invoke-SkillEvals.ps1 -Id 'flow-*'

# Run all (requires `copilot` on PATH and authenticated):
./evals/Invoke-SkillEvals.ps1
```

The default invoker calls `copilot -p "<prompt>" --allow-all-tools`. If your CLI build uses
different flags, or you want to target another agent, pass a custom invoker:

```powershell
./evals/Invoke-SkillEvals.ps1 -Invoker { param($p) my-agent --print $p | Out-String }
```

The runner exits non-zero if any case fails, so it can gate CI when an agent is available.

## What each eval case protects

- **Trigger** — a missing `defender` binary or an explicit install request routes to this skill
  and starts the `Install` → `Verify` → `InstallSkills` flow.
- **Flow** — image/fs/model/sbom scans pick legacy **Path A** (`AuthLegacy`, `GDN_MDC_CLI_*`);
  AI-scan / latest-result picks **Path B** (`ListTenants` → confirm tenant → `AuthAspm`).
- **Safety** — a failed Authenticode check is a hard stop; the agent must never suggest
  bypassing the signature gate.
- **NoTrigger** — when `defender` is already installed, the agent scans instead of re-onboarding.

## Adding cases

Edit `evals/skill-evals.psd1` and add a hashtable with `Id`, `Category`
(`Trigger`/`NoTrigger`/`Flow`/`Safety`), `Prompt`, `ExpectMatch`, `ForbidMatch`, and
`Rationale`. `tests/SkillEvalSpec.Tests.ps1` will validate the new case's shape and regexes on
the next deterministic run.
