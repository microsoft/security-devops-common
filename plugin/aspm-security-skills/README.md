# Defender code security — Copilot CLI Plugin

Brings Defender Code Security intelligence into your developer workflow. Skills that scan, generate, and remediate code.

## Install

```bash
copilot plugin install microsoft/security-devops-common:plugin/defender-code-security
```

Verify:

```bash
copilot plugin list
```

## Skills

| Skill | What it does |
|-------|--------------|
| [`onboard-defender-cli`](./skills/onboard-defender-cli/SKILL.md) | Installs the Defender for Cloud CLI (`defender`) binary, then installs the remaining Defender Code Security skills (`run-security-scan`, `fix-security-issues`) by running `defender agent --install`. |

## Try it

In a `copilot` interactive session:

```
> Scan my code — what actually matters?
> Generate a secure admin API endpoint.
> Fix the security issues you just found.
```

## Update / uninstall

```bash
copilot plugin update defender-code-security
copilot plugin uninstall defender-code-security
```
