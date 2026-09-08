# Security and Data Safety

Treat user filesystem access as sensitive.

- Never delete user media automatically.
- Never rename/move files automatically in v1.
- Restrict filesystem operations to configured sources.
- Validate paths and prevent traversal for future modifying operations.
- Never execute arbitrary files.
- Treat embedded metadata as untrusted input.
- Sanitize metadata before rendering.
- Never commit secrets/API keys.
- Do not expose local API publicly by default.

LAN access is opt-in and later: authentication, authorization, secure sessions/tokens, and appropriate rate limits.
