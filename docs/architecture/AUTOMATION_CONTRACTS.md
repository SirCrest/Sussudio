# Automation Contracts

The app exposes a JSON automation protocol over a named pipe. The default pipe
name is:

```text
SussudioAutomation
```

The shared command IDs, protocol constants, manifest/catalog, and pipe security
policy live in `Sussudio.Automation.Contracts/`.
`tools/Common/` remains helper-only for shared clients, formatters, diagnostic
sessions, and probes; do not put protocol/catalog/security contract sources
there.
When adding automation commands, treat `Sussudio.Automation.Contracts/` as the
source of truth for command IDs, protocol constants, command metadata, payload
shape, readiness gating, timeouts, path policy, CLI help, and MCP descriptions.
Then keep these consumers in sync:

- `Sussudio/Services/Automation/AutomationCommandDispatcher*.cs`
- `Sussudio.Automation.Contracts/AutomationCommandCatalog.cs`
- `Sussudio.Automation.Contracts/AutomationPipeProtocol.cs`
- `tools/ssctl/`
- `tools/McpServer/`
- `tools/AutomationClient/`
- `tools/send-automation-command.ps1`
- `tests/Sussudio.Tests/`

If `SUSSUDIO_AUTOMATION_TOKEN` is set, automation clients must provide the same
token.

Ownership for each consumer is mapped in `docs/architecture/AGENT_MAP.md`; the
architecture guardrail tests cross-check this checklist against that map.
