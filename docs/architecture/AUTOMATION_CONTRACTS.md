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

The app reads `SUSSUDIO_AUTOMATION_TOKEN` at startup. When a nonblank server token
is configured, every command requires the same token, including
`GetAutomationManifest`. Clients should send it in the request's top-level
`authToken` field; comparison is exact and case-sensitive.

ssctl accepts the global `--token` or `-t` option **before** the command. MCP
accepts `--token` when its server process starts; credentials are configured once
for the server, not exposed as arguments on individual MCP tools:

```powershell
ssctl --token "<token>" manifest
ssctl -t "<token>" state
McpServer --token "<token>"
```

Both clients pass an explicitly supplied token unchanged. When that value is
omitted or null, the shared request builder reads the client's
`SUSSUDIO_AUTOMATION_TOKEN` environment variable. A non-null empty or whitespace
value remains explicit and does not trigger that environment fallback. These
options do not modify the process environment. AutomationClient and
`tools/send-automation-command.ps1` retain their existing explicit-token route.

For compatibility, the server accepts `payload.authToken` on **all commands**
when the top-level `authToken` is null, empty, or whitespace. A wrong nonblank
top-level token takes precedence and cannot be rescued by a matching payload
token. Missing or incorrect effective credentials produce `unauthorized`.
New clients should use the top-level field.

The manifest's `Authentication` object describes these locations, precedence,
and the server-token configuration rule. It is static contract metadata: it
contains no credential and does not report whether a particular running server
has configured a token. Retrieve the manifest with valid credentials when the
server requires authentication.

Ownership for each consumer is mapped in `docs/architecture/AGENT_MAP.md`; the
architecture guardrail tests cross-check this checklist against that map.
