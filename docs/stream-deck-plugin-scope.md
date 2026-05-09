# Stream Deck Plugin Scope

## Transport

The Stream Deck plugin controls Sussudio through the existing named pipe
automation server. Requests use the same JSON line envelope as the built-in
automation clients:

```json
{
  "command": 1,
  "correlationId": "<guid>",
  "authToken": "<token-or-null>",
  "payload": {}
}
```

Use `AutomationPipeProtocol.CreateRequestEnvelope` so plugin requests match
ssctl, MCP, and AutomationClient behavior.

## Authentication

The optional auth token is configured with `SUSSUDIO_AUTOMATION_TOKEN`.
When the app has a token, every plugin request must include the same top-level
`authToken` value. If the token is missing or wrong, the pipe response reports
`ErrorCode: "unauthorized"` and the plugin should surface a connection settings
error instead of retrying the same credentials.

If no token is configured, local automation is available only when the app can
create the explicit per-user pipe security boundary. If that boundary cannot
be established, automation is disabled instead of opening a default security
pipe; configure `SUSSUDIO_AUTOMATION_TOKEN` to allow the token-required
fallback mode.
