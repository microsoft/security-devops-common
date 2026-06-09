# Microsoft Security DevOps

Common infrastructure and plugins for Microsoft Security DevOps tooling.

## Plugins

This repository hosts [GitHub Copilot CLI](https://github.com/github/copilot-cli) security plugins provided by ASPM.

### Available Plugins

| Plugin | Description |
|--------|-------------|
| [`defender-code-security`](./plugin/defender-code-security/) | A collection of Copilot CLI skills that enable secure code development by combining preventive guidance during code generation with reactive scanning using the Microsoft Defender CLI. The skills surface prioritized issues, recommend fixes, and expose AI-powered scan results from the repository directly in the developer environment. |

### Quick Start

```bash
# Install the plugin
copilot plugin install microsoft/security-devops-common:plugin/defender-code-security

# Verify
copilot plugin list
```

### Plugin Development

Plugins live under [`plugin/`](./plugin/). Each plugin directory contains a `plugin.json` manifest and a `skills/` folder with one or more skill definitions. See the [defender-code-security README](./plugin/defender-code-security/README.md) for an example.

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
