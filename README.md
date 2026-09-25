# ReCam

[![CI](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml)
![Server coverage](https://github.com/jpemendonca/ReCam/raw/badges/coverage.svg)

Turn spare Android phones into security cameras. Self-hosted, open source, no ads, no
subscriptions required.

You need one phone and one computer to start:

- **The ReCam server**, a Docker container on your own machine or VPS. It pairs your phones,
  relays the video and serves the Monitor you open in the browser.
- **The ReCam app**, on the phone that will film. Later you can add more phones, as cameras or as
  Monitors that watch.

> Status: pre-alpha. Nothing works yet. Follow progress in [ROADMAP.md](ROADMAP.md).

## How it will work

1. Run the server with `docker compose up -d`.
2. On your computer, open `https://<server-ip>:8443` and type the first-time code from
   `docker compose logs`. Your browser becomes the Monitor and shows an **Add camera** QR code.
   The certificate is self-signed, so the browser asks you to accept it once.
3. On the phone that will film, open ReCam, tap **Scan QR code**, scan it and name the camera.
4. The live video opens in the browser. Toggle the phone's flashlight from there.
5. Liked it? Add more phones from the browser: **Add camera** for another camera, **Add Monitor**
   to watch from your main phone.

Requirements: Android 9 or newer. Reference devices: Samsung Galaxy A10 and Xiaomi Redmi 6A.

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
