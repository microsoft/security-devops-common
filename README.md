# Microsoft Security DevOps

Common infrastructure, schemata, and plugin marketplace for Microsoft Security DevOps tooling.

## Plugin Marketplace

This repository hosts the **plugin marketplace** for [GitHub Copilot CLI](https://github.com/github/copilot-cli) security plugins provided by ASPM.

### Available Plugins

| Plugin | Description |
|--------|-------------|
| [`aspm-security-skills`](./plugin/aspm-security-skills/) | ASPM runtime-aware security skills — reactive scanning (capped at 5 findings) plus preventive secure code generation grounded in live deployment context. |

See [`marketplace.json`](./marketplace.json) for the machine-readable plugin registry.

### Quick Start

```bash
# 1. Register this marketplace (one-time)
copilot plugin marketplace add microsoft/security-devops-common

# 2. Install a plugin
copilot plugin install aspm-security-skills@aspm-plugins

# 3. Verify
copilot plugin list
```

### Plugin Development

Plugins live under [`plugin/`](./plugin/). Each plugin directory contains a `plugin.json` manifest and a `skills/` folder with one or more skill definitions. See the [aspm-security-skills README](./plugin/aspm-security-skills/README.md) for an example.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
