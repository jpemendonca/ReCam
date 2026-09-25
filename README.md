# Recam

Turn spare Android phones into security cameras. Self-hosted, open source, no ads, no
subscriptions required.

Recam has three parts:

- **Camera phone**: any Android phone that captures and streams video.
- **Server**: a Docker container on your own machine or VPS that pairs devices and relays video.
- **Viewer phone**: your everyday phone, where you watch the cameras live.

One app does both phone roles, in two tabs: **Camera** and **Watch**.

> Status: pre-alpha. Nothing works yet. Follow progress in [ROADMAP.md](ROADMAP.md).

## How it will work

1. Run the server with `docker compose up -d`.
2. On your phone, open the **Watch** tab and scan the QR code the server shows.
3. Tap **Add camera**. On the camera phone, open the **Camera** tab and scan that QR code.
4. Watch it live from your phone, and toggle the camera phone's flashlight remotely.

Requirements: Android 9 or newer. Tested on a Samsung Galaxy A10 and a Xiaomi Redmi 7A.

## Development

Prerequisites: .NET 10 SDK, Flutter, Docker.

```bash
git config core.hooksPath .githooks
bash scripts/gate.sh
```

The gate runs formatting, analysis, build and tests for `server/` and `app/`. Server tests start
a MediaMTX container, so Docker must be running.

Architecture, protocol and decisions: [SPECS.md](SPECS.md). Code rules: [CODESTYLE.md](CODESTYLE.md).
Agent workflow: [AGENTS.md](AGENTS.md).

## License

[AGPL-3.0](LICENSE). Contributions require signing a Contributor License Agreement.
