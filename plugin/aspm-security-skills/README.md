# ASPM Runtime-Aware Security — Copilot CLI Plugin

Brings ASPM runtime intelligence into your developer workflow. Skills that scan, generate, and remediate code with awareness of your live deployment context (internet exposure, attack paths, sensitive data).

## Install

```bash
# 1. Register the marketplace (one-time)
copilot plugin marketplace add microsoft/security-devops-common

# 2. Install the plugin
copilot plugin install aspm-security-skills@aspm-plugins
```

Verify:

```bash
copilot plugin list
```

## Skills

| Skill | What it does |
|-------|--------------|
| [`onboard-defender-cli`](./skills/onboard-defender-cli/SKILL.md) | Installs the `defender` CLI binary, then installs the remaining ASPM skills (`run-security-scan`, `fix-security-issues`) by running `defender agent --install`. |

## Try it

In a `copilot` interactive session:

```
> Scan my code — what actually matters?
> Generate a secure admin API endpoint.
> Fix the security issues you just found.
```

## Update / uninstall

```bash
copilot plugin update aspm-security-skills
copilot plugin uninstall aspm-security-skills
copilot plugin marketplace remove aspm-plugins
```
