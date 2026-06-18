# Defender code security — Agent Plugin

Brings Defender Code Security intelligence into your developer workflow. Skills that scan, generate, and remediate code.

## Install

### GitHub Copilot CLI

```bash
copilot plugin install microsoft/security-devops-common:plugin/defender-code-security
```

Verify:

```bash
copilot plugin list
```

### Claude

Claude can't install a plugin straight from a repo (no marketplace yet), so install the
skill folder directly. Ask Claude to:

1. Download the [`skills/onboard-defender-cli`](./skills/onboard-defender-cli) folder from this
   repo (`microsoft/security-devops-common`, path `plugin/defender-code-security/skills/onboard-defender-cli`).
2. Place it under your Claude skills directory (e.g. `~/.claude/skills/onboard-defender-cli`),
   keeping the folder structure (`SKILL.md` plus the `scripts/` directory) intact.
3. Run the `onboard-defender-cli` skill, which installs the Defender CLI and the remaining
   Defender Code Security skills (`run-security-scan`, `fix-security-issues`) via
   `defender agent --install`.

> Once we publish to a common marketplace, this will collapse to a single install command.

## Skills

| Skill | What it does |
|-------|--------------|
| [`onboard-defender-cli`](./skills/onboard-defender-cli/SKILL.md) | Installs the Defender CLI (`defender`) binary, then installs the remaining Defender Code Security skills (`run-security-scan`, `fix-security-issues`) by running `defender agent --install`. |

## Try it

In a `copilot` interactive session:

```
> Scan my code — what actually matters?
> Generate a secure admin API endpoint.
> Fix the security issues you just found.
```

## Update / uninstall

### GitHub Copilot CLI

```bash
copilot plugin update defender-code-security
copilot plugin uninstall defender-code-security
```

### Claude

Re-download the skill folder to update, or delete it from your Claude skills directory
(e.g. `~/.claude/skills/onboard-defender-cli`) to uninstall.
