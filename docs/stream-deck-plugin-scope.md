# Stream Deck Plugin Scope

> **Status: future roadmap.** No Stream Deck plugin is included in the current
> signed portable prerelease ZIP. This document preserves the intended protocol
> contract for a future implementation.

## Transport

The Stream Deck plugin controls Sussudio through the existing named pipe
automation server. Requests use the same JSON line envelope as the built-in
automation clients:

```json
{
  "command": 1,
  "correlationId": "<guid>",
  "authToken": "<token-or-null>",
  "manifestRevision": 2,
  "payload": {}
}
```

Use `AutomationPipeProtocol.CreateRequestEnvelope` and
`AutomationPipeProtocol.CommandManifestRevision` so plugin requests match ssctl,
MCP, and AutomationClient behavior. The app rejects mismatched manifest
revisions before dispatch to keep stale numeric command IDs from silently
misrouting commands.

## Authentication

The app reads its optional auth token from `SUSSUDIO_AUTOMATION_TOKEN` at
startup. When a nonblank token is configured, every request requires matching
credentials, including `GetAutomationManifest`. New plugin requests should
send the exact, case-sensitive token in the top-level `authToken` field.

The server also accepts legacy `payload.authToken` on every command, but only
when the top-level token is null, empty, or whitespace. A wrong nonblank
top-level token wins over a matching payload token and is rejected. Missing or
incorrect effective credentials produce `ErrorCode: "unauthorized"`; the plugin
should surface a connection settings error instead of retrying the same
credentials.

With `AutomationPipeProtocol.CreateRequestEnvelope`, an explicit non-null token
is sent unchanged. An omitted or null token falls back to the client process's
`SUSSUDIO_AUTOMATION_TOKEN`; an explicit empty or whitespace value does not.
For command-line checks, use `ssctl --token "<token>" manifest` or
`ssctl -t "<token>" state`, keeping the global option before the command. Start
the MCP server with `McpServer --token "<token>"` to configure its token once
for all tools. These options do not modify the process environment.

The manifest's `Authentication` object documents the credential paths and
precedence without including a token or reporting a running server's configured
state. Fetching it still requires authentication when the app has a token.

If no token is configured, local automation is available only when the app can
create the explicit per-user pipe security boundary. If that boundary cannot
be established, automation is disabled instead of opening a default security
pipe; configure `SUSSUDIO_AUTOMATION_TOKEN` to allow the token-required
fallback mode.
