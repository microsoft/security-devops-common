# onboard-defender-cli — tests & evals

Two layers of validation for this skill:

| Layer | Location | Deterministic? | Needs LLM/CLI? |
|-------|----------|----------------|----------------|
| Pester unit tests for `scripts/bootstrap.ps1` | `tests/bootstrap.Tests.ps1` | Yes | No |
| Structural lint for `SKILL.md` (frontmatter + doc/script drift) | `tests/SkillStructure.Tests.ps1` | Yes | No |
| Eval-spec schema validation (`evals/evals.json`) | `tests/Evals.Tests.ps1` | Yes | No |
| Behavioral evals (prompt → agent behavior) | `evals/evals.json` | No | Yes (run via skill-creator) |

The behavioral evals follow the **skill-creator** convention: a single
`evals/evals.json` file holding `prompt` / `expected_output` / `expectations`
(natural-language statements graded by an LLM grader), rather than regex
scenarios. The deterministic Pester suite stays in `tests/`.

## Prerequisites

- **PowerShell 7+** and **Pester 5** for the deterministic suite:

  ```powershell
  Install-Module Pester -MinimumVersion 5.0.0 -Scope CurrentUser -SkipPublisherCheck
  ```

- The **skill-creator** skill (for running the behavioral evals — see below).

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
- **evals/evals.json shape** — valid JSON, `skill_name` matches the directory, unique integer
  `id`s, and every eval has a non-empty `prompt`, `expected_output`, and at least one
  `expectation`.

> `tests/bootstrap.Tests.ps1` dot-sources `scripts/bootstrap.ps1`. The script's `-Step`
> dispatch is guarded by `if ($MyInvocation.InvocationName -ne '.')` so dot-sourcing only
> defines the functions and never runs a phase.

## Run the behavioral evals (live, via skill-creator)

The behavioral evals live in `evals/evals.json` in the skill-creator schema. Each entry is a
single-turn `prompt` plus a set of natural-language `expectations` that an LLM grader checks
against the agent's transcript and outputs. Run them with the **skill-creator** tooling, which
spawns a with-skill and a baseline (no-skill) run, grades each against the `expectations`, and
aggregates a benchmark report.

```text
Use the skill-creator skill to evaluate this skill's evals/evals.json.
```

`evals/evals.json` is the portable spec — `expected_output` describes success for a human, and
`expectations[]` are the verifiable statements the grader scores.

## What each eval protects

- **Triggering** (ids 1–3) — a missing `defender` binary or an explicit install request routes
  to this skill and starts the `Install` → `Verify` → `InstallSkills` flow.
- **Auth path selection** (ids 4–6) — image/fs/model/sbom scans pick legacy **Path A**
  (`AuthLegacy`, `GDN_MDC_CLI_*`); AI-scan / latest-result picks **Path B**
  (`ListTenants` → confirm tenant → `AuthAspm`).
- **Safety** (id 7) — a failed Authenticode check is a hard stop; the agent must never suggest
  bypassing the signature gate.
- **No re-trigger** (id 8) — when `defender` is already installed, the agent scans instead of
  re-onboarding.

## Adding evals

Edit `evals/evals.json` and add an object with a unique integer `id`, a `prompt`, an
`expected_output` (human-readable success description), an optional `files` array, and an
`expectations` array of objectively verifiable statements. Keep expectations *discriminating* —
each should pass only when the skill genuinely does the right thing. `tests/Evals.Tests.ps1`
validates the new entry's shape on the next deterministic run.
